using UnityEngine;

namespace Assets._Scripts
{
    public class Player : MonoBehaviour
    {
        private PlayerControls _playerControls;

        private Transform _playerTransform;
        private Rigidbody _playerRigidBody;

        private Quaternion _playerRotation;
        private Quaternion _targetRotation;
        [SerializeField] private float _rotationSpeed = 5f;

        [SerializeField] private float _accelaration = 5f;
        [SerializeField] private float _decelaration = 5f;
        [SerializeField] private float _maxSpeed = 5f;
        private Vector3 _currentSpeed;
        private bool _isThrottling;

        [SerializeField] private AnimationCurve _rotationSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private void Awake()
        {
            _playerControls = new PlayerControls();
            _playerControls.Enable();

            _playerControls.Player.Look.performed += ctx => Look(ctx.ReadValue<Vector2>());

            _playerControls.Player.Throttle.performed += ctx => Throttle(ctx.ReadValueAsButton());
            _playerControls.Player.Throttle.canceled += ctx => Throttle(ctx.ReadValueAsButton());

            _playerControls.Player.Fire.performed += ctx => Fire(ctx.ReadValueAsButton());
            _playerControls.Player.Fire.canceled += ctx => Fire(ctx.ReadValueAsButton());

            _playerTransform = transform;
            _playerRigidBody = GetComponent<Rigidbody>();
        }

        private float _timeToRotate;
        private float _elapsedTime;

        /// <summary>
        /// Updates the player's rotation based on the input look value.
        /// </summary>
        /// <param name="lookValue">The input look value.</param>
        private void Look(Vector2 lookValue)
        {
            if (lookValue == Vector2.zero) return;
            float lookAngle = Mathf.Atan2(lookValue.x, lookValue.y) * Mathf.Rad2Deg;

            // convert the angle to be within the range of 0 to 360 degrees
            lookAngle = (lookAngle + 360) % 360;
            Quaternion newTargetRotation = Quaternion.Euler(0, lookAngle, 0);

            // TODO: Add a check to see if the player is already rotating to the new target rotation/is already looking at the target rotation

            _targetRotation = newTargetRotation;
            _playerRotation = _playerTransform.rotation;

            _timeToRotate = Quaternion.Angle(_playerRotation, _targetRotation) / _rotationSpeed;

            // Debug.log all the values
            // Debug.Log($"Player Rotation: {_playerRotation.eulerAngles.y} Look Angle: {lookAngle} Time to Rotate: {_timeToRotate}");

            _elapsedTime = 0;
        }

        private void Fire(bool isFiring)
        {
            Debug.Log(isFiring ? "Firing" : "Not Firing");
        }

        private void Throttle(bool isThrottling)
        {
            _isThrottling = isThrottling;
        }

        private void FixedUpdate()
        {
            _elapsedTime += Time.fixedDeltaTime;
            float percentage = _elapsedTime / _timeToRotate;
            _playerTransform.rotation = Quaternion.Slerp(_playerRotation, _targetRotation, _rotationSpeedCurve.Evaluate(percentage));

            _currentSpeed = _playerRigidBody.velocity;

            if (_isThrottling)
            {
                _currentSpeed.x = Mathf.MoveTowards(_currentSpeed.x, _playerTransform.forward.x * _maxSpeed,
                    _accelaration * Time.fixedDeltaTime);
                _currentSpeed.z = Mathf.MoveTowards(_currentSpeed.z, _playerTransform.forward.z * _maxSpeed,
                    _accelaration * Time.fixedDeltaTime);
            }
            else
            {
                _currentSpeed.x = Mathf.MoveTowards(_currentSpeed.x, 0, _decelaration * Time.fixedDeltaTime);
                _currentSpeed.z = Mathf.MoveTowards(_currentSpeed.z, 0, _decelaration * Time.fixedDeltaTime);
            }

            _playerRigidBody.velocity = _currentSpeed;
        }
    }
}