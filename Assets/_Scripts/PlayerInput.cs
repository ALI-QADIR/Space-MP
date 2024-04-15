using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerControls _playerControls;

    private Vector2 _lookInput;
    private Vector2 _lastValidLookInput;
    private bool _throttleInput;
    private bool _fireInput;
    public Vector2 LookInput => _lookInput;
    public bool ThrottleInput => _throttleInput;
    public bool FireInput => _fireInput;


    private void Awake()
    {
        _playerControls = new PlayerControls();
    }
    private void OnEnable()
    {
        _playerControls.Player.Enable();

        _playerControls.Player.Look.performed += ctx =>
        {
            _lookInput = ctx.ReadValue<Vector2>();
            if (_lookInput == Vector2.zero)
            {
                _lookInput = _lastValidLookInput;
            }
            else
            {
                _lastValidLookInput = _lookInput;
            }
        };
        
        _playerControls.Player.Throttle.performed += ctx => _throttleInput = ctx.ReadValueAsButton();
        _playerControls.Player.Throttle.canceled += ctx => _throttleInput = ctx.ReadValueAsButton();

        _playerControls.Player.Fire.started += ctx => _fireInput = ctx.ReadValueAsButton();
        _playerControls.Player.Fire.canceled += ctx => _fireInput = ctx.ReadValueAsButton();
    }

    private void OnDisable()
    {
        _playerControls.Player.Look.performed -= ctx => _lookInput = ctx.ReadValue<Vector2>();

        _playerControls.Player.Throttle.performed -= ctx => _throttleInput = ctx.ReadValueAsButton();
        _playerControls.Player.Throttle.canceled -= ctx => _throttleInput = ctx.ReadValueAsButton();

        _playerControls.Player.Fire.started -= ctx => _fireInput = ctx.ReadValueAsButton();
        _playerControls.Player.Fire.canceled -= ctx => _fireInput = ctx.ReadValueAsButton();

        _playerControls.Player.Disable();
    }
}
