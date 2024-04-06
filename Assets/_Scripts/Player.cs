using UnityEngine;
using System.Threading.Tasks;

namespace Assets._Scripts
{
    public class Player : Singleton<Player>
    {
        private PlayerControls _playerControls;

        private Transform _playerTransform;
        public ref Transform PlayerTransform => ref _playerTransform;
        private Rigidbody _playerRigidBody;

        [Header("Rotation")] private Quaternion _playerRotation;
        private Quaternion _targetRotation;

        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private AnimationCurve _rotationSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Movement")][SerializeField] private float _accelaration = 5f;
        [SerializeField] private float _decelaration = 5f;
        [SerializeField] private float _maxSpeed = 5f;
        private Vector3 _currentSpeed;

        [Header("Fuel")][SerializeField, Range(0, 100)] private float _fuel;
        [SerializeField] private float _fuelConsumptionRate;
        [SerializeField] private float _fuelRegenerationRate;
        [SerializeField, Tooltip("Amount of time to wait before throttling is re-enabled")] private float _waitForFuelRegeneration;

        private float _currentFuel;
        private bool _isThrottling;
        public bool IsThrottling => _isThrottling;
        private bool _isThrottlingEnabled;

        [Header("Weapon")][SerializeField] private Bullet _bulletPrefab;
        [SerializeField] private float _ammo;
        [SerializeField] private float _fireRate;
        [SerializeField] private float _ammoDepletionRate;
        [SerializeField] private float _ammoRefillRate;
        [SerializeField, Tooltip("Amount of time to wait before firing is re-enabled")] private float _waitForAmmoRefill;

        private float _currentAmmo;
        private bool _isFiring;
        private bool _isFiringEnabled;

        protected override void Awake()
        {
            base.Awake();
            _playerControls = new PlayerControls();
            _playerControls.Enable();

            _playerControls.Player.Look.performed += ctx => Look(ctx.ReadValue<Vector2>());

            _playerControls.Player.Throttle.performed += ctx => Throttle(ctx.ReadValueAsButton());
            _playerControls.Player.Throttle.canceled += ctx => Throttle(ctx.ReadValueAsButton());

            _playerControls.Player.Fire.started += ctx => Fire(ctx.ReadValueAsButton());
            _playerControls.Player.Fire.canceled += ctx => Fire(ctx.ReadValueAsButton());

            _playerTransform = transform;
            _playerRigidBody = GetComponent<Rigidbody>();
        }

        private float _timeToRotate;
        private float _elapsedTime;
        public float ElapsedTime => _elapsedTime;

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
            _isFiring = isFiring;
            Fire();
        }

        private void Throttle(bool isThrottling)
        {
            _isThrottling = isThrottling;
        }

        private void Start()
        {
            _currentFuel = _fuel;
            _isThrottlingEnabled = true;
            _currentAmmo = _ammo;
            _isFiringEnabled = true;
        }

        private void Update()
        {
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
            _elapsedTime += Time.fixedDeltaTime;
            float percentage = _elapsedTime / _timeToRotate;
            _playerTransform.rotation = Quaternion.Slerp(_playerRotation, _targetRotation, _rotationSpeedCurve.Evaluate(percentage));

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

        private void OnEnable()
        {
            _playerControls.Enable();
        }

        private void OnDisable()
        {
            _playerControls.Disable();
        }
    }
}