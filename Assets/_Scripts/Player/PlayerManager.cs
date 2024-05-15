using System;
using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using Assets._Scripts.Utils;
using Assets._Scripts.Multiplayer;

namespace Assets._Scripts.Player
{
    [RequireComponent(typeof(ServerAuthoriser), typeof(ClientPredictor), typeof(PlayerInput))]
    public class PlayerManager : NetworkBehaviour // Singleton<Player>
    {
        #region Components

        private Transform _playerTransform;
        private ClientPredictor _clientPredictor;
        private ServerAuthoriser _serverAuthoriser;

        internal PlayerMovement playerMovement;

        #endregion

        #region InputVariables

        private Vector3 _lookInput;
        private bool _isThrottling;
        
        #endregion

        #region WeaponVariables

        [Header("Weapon")][SerializeField] private Bullet _bulletPrefab;
        [SerializeField] private float _ammo;
        [SerializeField] private float _fireRate;
        [SerializeField] private float _ammoDepletionRate;
        [SerializeField] private float _ammoRefillRate;
        [SerializeField, Tooltip("Amount of time to wait before firing is re-enabled")] private float _waitForAmmoRefill;

        private float _currentAmmo;
        private bool _isFiring;
        private bool _isFiringEnabled;

        #endregion

        #region ReconciliationVariables

        [Header("Reconciliation")] [SerializeField] private float _rotationReconciliationThreshold = 5f;
        [SerializeField] private float _positionReconciliationThreshold = 0.5f;

        #endregion

        #region NetworkGeneral

        private NetworkTimer _networkTimer;
        public ref NetworkTimer NetworkTimer => ref _networkTimer;

        private const float k_serverTickRate = 60f;
        private const int k_bufferSize = 1024;
        public int BufferSize => k_bufferSize;

        #endregion

        private void Awake()
        {
            _playerTransform = transform;
            _clientPredictor = GetComponent<ClientPredictor>();
            _serverAuthoriser = GetComponent<ServerAuthoriser>();
            playerMovement = GetComponent<PlayerMovement>();

            _currentAmmo = _ammo;
            _isFiringEnabled = true;

            _networkTimer = new NetworkTimer(k_serverTickRate);
        }

        private void Update()
        {
            _networkTimer.Update(Time.deltaTime);
            if (!IsOwner) return;

            if (_isFiring && _currentAmmo > 0 && _isFiringEnabled)
            {
                _currentAmmo = Mathf.MoveTowards(_currentAmmo, 0, _ammoDepletionRate * Time.deltaTime);
                if (_currentAmmo == 0)
                {
                    _isFiringEnabled = false;
                    WaitForRefillAmmo();
                }
            }
            else
            {
                _currentAmmo = Mathf.MoveTowards(_currentAmmo, _ammo, _ammoRefillRate * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;
            while (_networkTimer.ShouldTick())
            {
                MovementInputPayload inputPayload = new MovementInputPayload
                {
                    tick = _networkTimer.CurrentTick,
                    look = _lookInput,
                    throttle = _isThrottling
                };
                _clientPredictor.HandleClientTick(inputPayload);
                _serverAuthoriser.SendToServerRpc(inputPayload);
                _serverAuthoriser.HandleServerTick();
            }
        }

        internal void HandleServerReconciliation()
        {
            if (!_clientPredictor.ShouldReconcile()) return;

            var bufferIndex = _clientPredictor.GetBufferIndexOfLastState(k_bufferSize);

            if (bufferIndex - 1 < 0) return; // Not enough data to reconcile

            // Host RPCs execute immediately
            var rewindState = IsHost ? _serverAuthoriser.GetStateFromServer(bufferIndex - 1) : _clientPredictor.GetLastServerState();

            float rotationError = _clientPredictor.GetRotationErrorForBufferIndex(bufferIndex, rewindState.rotation);
            float positionError = _clientPredictor.GetPositionErrorForBufferIndex(bufferIndex, rewindState.position);

            if (rotationError > _rotationReconciliationThreshold || positionError > _positionReconciliationThreshold)
            {
                ReconcileState(rewindState);
            }

            _clientPredictor.SetLastProcessedState();
        }

        void ReconcileState(MovementStatePayload rewindState)
        {
            playerMovement.ReconcileRewindState(ref rewindState);

            MovementStatePayload lastServerState = _clientPredictor.GetLastServerState();
            if (!rewindState.Equals(lastServerState)) return;

            _clientPredictor.AddToClientStateBuffer(rewindState, rewindState.tick);

            // replay all the inputs from rewind state to current state
            int tickToReplay = lastServerState.tick;

            while (tickToReplay < _networkTimer.CurrentTick)
            {
                int bufferIndex = tickToReplay % k_bufferSize;
                MovementInputPayload inputAtBufferIndex = _clientPredictor.GetInputAtBufferIndex(bufferIndex);
                MovementStatePayload statePayload = playerMovement.ProcessInput(ref inputAtBufferIndex);
                _clientPredictor.AddToClientStateBuffer(statePayload, bufferIndex);
                tickToReplay++;
            }
        }

        internal void SendStateToClient(MovementStatePayload statePayload)
        {
            _clientPredictor.SendToClientRpc(statePayload);
        }

        private async void Fire()
        {
            if (!_isFiring || !_isFiringEnabled) return;
            Instantiate(_bulletPrefab, _playerTransform.position, _playerTransform.rotation);
            await Task.Delay((int)(1000 / _fireRate));
            Fire();
        }

        private async void WaitForRefillAmmo()
        {
            await Task.Delay((int)(_waitForAmmoRefill * 1000));
            _isFiringEnabled = true;
        }

        #region SetInputs

        internal void SetLookInput(Vector3 lookInput)
        {
            _lookInput = lookInput;
        }

        internal void SetThrottleInput(bool throttleInput)
        {
            _isThrottling = throttleInput;
        }

        internal void SetFireInput(bool fireInput)
        {
            _isFiring = fireInput;
        }

        #endregion

        // Add missing components if necessary
        #region ComponentValidation

        private void OnValidate()
        {
            try
            {
                bool hasRigidBody = GetComponent<Rigidbody>();
                if (!hasRigidBody) gameObject.AddComponent<Rigidbody>();
            }
            catch (Exception e)
            {
                Debug.Log("Could not add Rigid body component." + e);
            }

            try
            {
                bool hasCollider = GetComponent<Collider>();
                if (!hasCollider) gameObject.AddComponent<Collider>();
            }
            catch (Exception e)
            {
                Debug.Log("Could not add Collider component." + e);
            }

            try
            {
                bool hasPlayerInput = GetComponent<PlayerInput>();
                if (!hasPlayerInput) gameObject.AddComponent<PlayerInput>();

            }
            catch (Exception e)
            {
                Debug.Log("Could not add PlayerInput component." + e);
            }
        }
        #endregion
    }
}