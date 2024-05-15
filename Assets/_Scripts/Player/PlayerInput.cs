using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets._Scripts.Player
{
    [RequireComponent(typeof(PlayerManager))]
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private PlayerManager _playerManager;
        private PlayerControls _playerControls;

        private Vector3 _lookInput;
        private Vector3 _lastValidLookInput;
        private bool _throttleInput;
        private bool _fireInput;


        private void Awake()
        {
            _playerControls = new PlayerControls();
            _playerManager = GetComponent<PlayerManager>();
        }

        private void OnEnable()
        {
            _playerControls.Player.Enable();

            _playerControls.Player.Look.performed += OnLookPressed;
        
            _playerControls.Player.Throttle.performed += OnThrottlePressed;
            _playerControls.Player.Throttle.canceled += OnThrottlePressed;

            _playerControls.Player.Fire.started += OnFirePressed;
            _playerControls.Player.Fire.canceled += OnFirePressed;
        }

        private void OnLookPressed(InputAction.CallbackContext ctx)
        {
            Vector2 lookInput = ctx.ReadValue<Vector2>();
            _lookInput = new Vector3(lookInput.x, 0, lookInput.y);
            if (_lookInput == Vector3.zero)
            {
                _lookInput = _lastValidLookInput;
            }
            else
            {
                _lastValidLookInput = _lookInput;
            }
            _playerManager.SetLookInput(_lookInput);
        }

        private void OnThrottlePressed(InputAction.CallbackContext ctx)
        {
            _throttleInput = ctx.ReadValueAsButton();
            _playerManager.SetThrottleInput(_throttleInput);
        }

        private void OnFirePressed(InputAction.CallbackContext ctx)
        {
            _fireInput = ctx.ReadValueAsButton();
            _playerManager.SetFireInput(_fireInput);
        }

        private void OnDisable()
        {
            _playerControls.Player.Look.performed -= OnLookPressed;

            _playerControls.Player.Throttle.performed -= OnThrottlePressed;
            _playerControls.Player.Throttle.canceled -= OnThrottlePressed;

            _playerControls.Player.Fire.started -= OnFirePressed;
            _playerControls.Player.Fire.canceled -= OnFirePressed;

            _playerControls.Player.Disable();
        }
    }
}
