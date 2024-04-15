using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using Assets._Scripts.Utils;

namespace Assets._Scripts
{
    public struct InputPayload : INetworkSerializable
    {
        public Vector3 look;
        public bool throttle;
        public int tick;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref look);
            serializer.SerializeValue(ref throttle);
            serializer.SerializeValue(ref tick);
        }
    }

    public struct StatePayload : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref angularVelocity);
        }
    }

    [RequireComponent(typeof(PlayerInput), typeof(Rigidbody), typeof(Collider))]
    public class Player : NetworkBehaviour // Singleton<Player>
    {
        #region Components

        private PlayerInput _playerInput;

        private Transform _playerTransform;
        public ref Transform PlayerTransform => ref _playerTransform;
        private Rigidbody _playerRigidBody;

        #endregion

        #region RotationVariables

        [Header("Rotation")] [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private AnimationCurve _rotationSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        private Vector2 _lookInput;
        private Quaternion _playerRotation;
        private Quaternion _targetRotation;
        private float _timeToRotate;
        private float _elapsedTime;
        public float ElapsedTime => _elapsedTime;


        #endregion

        #region MovementVariables

        [Header("Movement")][SerializeField] private float _accelaration = 5f;
        [SerializeField] private float _decelaration = 5f;
        [SerializeField] private float _maxSpeed = 5f;
        private Vector3 _currentSpeed;

        #endregion

        #region FuelVariables

        [Header("Fuel")][SerializeField, Range(0, 100)] private float _fuel;
        [SerializeField] private float _fuelConsumptionRate;
        [SerializeField] private float _fuelRegenerationRate;
        [SerializeField, Tooltip("Amount of time to wait before throttling is re-enabled")] private float _waitForFuelRegeneration;

        private float _currentFuel;
        private bool _isThrottling;
        public bool IsThrottling => _isThrottling;
        private bool _isThrottlingEnabled;

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

        #region NetworkGeneral

        private NetworkTimer _networkTimer;

        private const float k_serverTickRate = 60f;
        private const int k_bufferSize = 1024;

        #endregion

        #region NetcodeClientSide

        private CircularBuffer<InputPayload> _clientInputBuffer;

        private CircularBuffer<StatePayload> _clientStateBuffer;
        private StatePayload _lastServerState;
        private StatePayload _lastProcessedState;

        #endregion

        #region NetcodeServerSide
        
        private CircularBuffer<StatePayload> _serverStateBuffer;

        private Queue<InputPayload> _serverInputQueue;

        #endregion

        protected void Awake()
        {
            _playerTransform = transform;
            _playerRigidBody = GetComponent<Rigidbody>();
            _playerInput = GetComponent<PlayerInput>();

            _currentFuel = _fuel;
            _isThrottlingEnabled = true;
            _currentAmmo = _ammo;
            _isFiringEnabled = true;

            _networkTimer = new NetworkTimer(k_serverTickRate);
            _clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
            _clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _serverInputQueue = new Queue<InputPayload>();
        }

        private void Start()
        {
        }

        private void Update()
        {
            _networkTimer.Update(Time.deltaTime);
            if (!IsOwner) return;

            GatherInputs();

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
            Look(new Vector3(_lookInput.x, 0f, _lookInput.y));

            _currentSpeed = _playerRigidBody.velocity;

            if (_isThrottling && _currentFuel > 0f && _isThrottlingEnabled)
            {
                _currentSpeed.x = Mathf.MoveTowards(_currentSpeed.x, _playerTransform.forward.x * _maxSpeed,
                    _accelaration * Time.fixedDeltaTime);
                _currentSpeed.z = Mathf.MoveTowards(_currentSpeed.z, _playerTransform.forward.z * _maxSpeed,
                    _accelaration * Time.fixedDeltaTime);
                _currentFuel = Mathf.MoveTowards(_currentFuel, 0, _fuelConsumptionRate * Time.fixedDeltaTime);
                if (_currentFuel == 0f)
                {
                    _isThrottlingEnabled = false;
                    WaitForRegenerateFuel();
                }
            }
            else
            {
                _currentSpeed.x = Mathf.MoveTowards(_currentSpeed.x, 0, _decelaration * Time.fixedDeltaTime);
                _currentSpeed.z = Mathf.MoveTowards(_currentSpeed.z, 0, _decelaration * Time.fixedDeltaTime);
                _currentFuel = Mathf.MoveTowards(_currentFuel, _fuel, _fuelRegenerationRate * Time.fixedDeltaTime);
            }

            _playerRigidBody.velocity = _currentSpeed;
        }
        
        private void GatherInputs()
        {
            _lookInput = _playerInput.LookInput;
            _isFiring = _playerInput.FireInput;
            _isThrottling = _playerInput.ThrottleInput;
        }

        /// <summary>
        /// Updates the player's rotation based on the input look value.
        /// </summary>
        /// <param name="lookValue">The input look value.</param>
        private void Look(Vector3 lookVector)
        {
            float lookAngle = Mathf.Atan2(lookVector.x, lookVector.z) * Mathf.Rad2Deg;

            // convert the angle to be within the range of 0 to 360 degrees
            lookAngle = (lookAngle + 360) % 360;
            Quaternion newTargetRotation = Quaternion.Euler(0, lookAngle, 0);

            _targetRotation = newTargetRotation;
            _playerRotation = _playerTransform.rotation;

            _playerTransform.rotation = Quaternion.Slerp(_playerRotation, _targetRotation, _rotationSpeed * Time.fixedDeltaTime);

            // TODO: Implement rotation with time 

            //_timeToRotate = Quaternion.Angle(_playerRotation, _targetRotation) / _rotationSpeed;

            //_elapsedTime = 0;

            //_elapsedTime += Time.fixedDeltaTime;
            //float percentage = _elapsedTime / _timeToRotate;
            //_playerTransform.rotation = Quaternion.Slerp(_playerRotation, _targetRotation, _rotationSpeedCurve.Evaluate(percentage));
        }

        private async void Fire()
        {
            if (!_isFiring || !_isFiringEnabled) return;
            Instantiate(_bulletPrefab, _playerTransform.position, _playerTransform.rotation);
            await Task.Delay((int)(1000 / _fireRate));
            Fire();
        }

        private async void WaitForRegenerateFuel()
        {
            await Task.Delay((int)(_waitForFuelRegeneration * 1000));
            _isThrottlingEnabled = true;
        }

        private async void WaitForRefillAmmo()
        {
            await Task.Delay((int)(_waitForAmmoRefill * 1000));
            _isFiringEnabled = true;
        }

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