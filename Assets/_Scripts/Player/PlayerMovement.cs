using System.Threading.Tasks;
using Assets._Scripts.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Assets._Scripts.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class PlayerMovement : NetworkBehaviour
    {
        private Transform _playerTransform;
        private Rigidbody _playerRigidBody;

        #region MovementVariables

        [Header("Movement")][SerializeField] private float _acceleration = 5f;
        [SerializeField] private float _deceleration = 5f;
        [SerializeField] private float _maxSpeed = 5f;
        private Vector3 _currentSpeed;

        #endregion

        #region FuelVariables

        [Header("Fuel")][SerializeField, Range(0, 100)] private float _fuel;
        [SerializeField] private float _fuelConsumptionRate;
        [SerializeField] private float _fuelRegenerationRate;
        [SerializeField, Tooltip("Amount of time to wait before throttling is re-enabled")] private float _waitForFuelRegeneration;

        private float _currentFuel;
        private bool _isThrottlingEnabled;

        #endregion

        #region RotationVariables

        [Header("Rotation")][SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private AnimationCurve _rotationSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        private Quaternion _playerRotation;
        private Quaternion _targetRotation;
        private float _timeToRotate;
        private float _elapsedTime;

        #endregion

        internal void ReconcileRewindState(ref MovementStatePayload rewindState)
        {
            _playerTransform.position = rewindState.position;
            _playerTransform.rotation = rewindState.rotation;
            _playerRigidBody.velocity = rewindState.velocity;
            _playerRigidBody.angularVelocity = rewindState.angularVelocity;
            _currentFuel = rewindState.currentFuel;
        }

        internal MovementStatePayload SimulatePhysicsOnServer(ref MovementInputPayload input)
        {
            Physics.simulationMode = SimulationMode.Script;

            Look(input.look);
            Throttle(input.throttle);
            Physics.Simulate(Time.fixedDeltaTime);

            Physics.simulationMode = SimulationMode.FixedUpdate;

            return new MovementStatePayload
            {
                tick = input.tick,
                position = _playerTransform.position,
                rotation = _playerTransform.rotation,
                velocity = _playerRigidBody.velocity,
                angularVelocity = _playerRigidBody.angularVelocity,
                currentFuel = _currentFuel
            };
        }

        internal MovementStatePayload ProcessInput(ref MovementInputPayload input)
        {
            Look(input.look);
            Throttle(input.throttle);

            return new MovementStatePayload
            {
                tick = input.tick,
                position = _playerTransform.position,
                rotation = _playerTransform.rotation,
                velocity = _playerRigidBody.velocity,
                angularVelocity = _playerRigidBody.angularVelocity,
                currentFuel = _currentFuel
            };
        }

        /// <summary>
        /// Updates the player's rotation based on the input look value.
        /// </summary>
        /// <param name="lookVector">The input look value.</param>
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

        private void Throttle(bool isThrottling)
        {
            _currentSpeed = _playerRigidBody.velocity;

            if (isThrottling && _currentFuel > 0f && _isThrottlingEnabled)
            {
                _currentSpeed.x = Mathf.MoveTowards(_currentSpeed.x, _playerTransform.forward.x * _maxSpeed,
                    _acceleration * Time.fixedDeltaTime);
                _currentSpeed.z = Mathf.MoveTowards(_currentSpeed.z, _playerTransform.forward.z * _maxSpeed,
                    _acceleration * Time.fixedDeltaTime);
                _currentFuel = Mathf.MoveTowards(_currentFuel, 0, _fuelConsumptionRate * Time.fixedDeltaTime);
                if (_currentFuel == 0f)
                {
                    _isThrottlingEnabled = false;
                    WaitForRegenerateFuel();
                }
            }
            else
            {
                _currentSpeed.x = Mathf.MoveTowards(_currentSpeed.x, 0, _deceleration * Time.fixedDeltaTime);
                _currentSpeed.z = Mathf.MoveTowards(_currentSpeed.z, 0, _deceleration * Time.fixedDeltaTime);
                _currentFuel = Mathf.MoveTowards(_currentFuel, _fuel, _fuelRegenerationRate * Time.fixedDeltaTime);
            }

            _playerRigidBody.velocity = _currentSpeed;
        }

        private async void WaitForRegenerateFuel()
        {
            await Task.Delay((int)(_waitForFuelRegeneration * 1000));
            _isThrottlingEnabled = true;
        }

        private void Awake()
        {
            _playerTransform = transform;
            _playerRigidBody = GetComponent<Rigidbody>();

            _currentFuel = _fuel;
            _isThrottlingEnabled = true;
        }
    }
}
