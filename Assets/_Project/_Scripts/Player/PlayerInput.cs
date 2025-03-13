using TripleA.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.PlayerControls;

namespace CosmicClash.Player
{
    public class PlayerInput : MonoBehaviour, IPlayerActions
    {
        private PlayerControls m_playerControls;
        
        public Vector3 LookInput { get; private set; }
        public float Steer { get; private set; }
        public bool IsThrottling { get; private set; }
        public bool IsBreaking { get; private set; }
        public bool IsFiring { get; private set; }

        #region Unity Methods

        private void Awake()
        {
            m_playerControls = new PlayerControls();
        }

        private void OnEnable()
        {
            m_playerControls.Player.Steer.performed += OnSteer;
            m_playerControls.Player.Steer.canceled += OnSteer;
            
            m_playerControls.Player.Look.performed += OnLook;
            m_playerControls.Player.Look.canceled += OnLook;
            
            m_playerControls.Player.Fire.performed += OnFire;
            m_playerControls.Player.Fire.canceled += OnFire;
            
            m_playerControls.Player.Throttle.performed += OnThrottle;
            m_playerControls.Player.Throttle.canceled += OnThrottle;
            
            m_playerControls.Player.Break.performed += OnBreak;
            m_playerControls.Player.Break.canceled += OnBreak;
        }
        
        private void OnDisable()
        {
            m_playerControls.Player.Steer.performed -= OnSteer;
            m_playerControls.Player.Steer.canceled -= OnSteer;
            
            m_playerControls.Player.Look.performed -= OnLook;
            m_playerControls.Player.Look.canceled -= OnLook;
            
            m_playerControls.Player.Fire.performed -= OnFire;
            m_playerControls.Player.Fire.canceled -= OnFire;
            
            m_playerControls.Player.Throttle.performed -= OnThrottle;
            m_playerControls.Player.Throttle.canceled -= OnThrottle;
            
            m_playerControls.Player.Break.performed -= OnBreak;
            m_playerControls.Player.Break.canceled -= OnBreak;
        }

        #endregion

        public void EnableControls()
        {
            m_playerControls.Player.Enable();
        }

        public void DisableControls()
        {
            m_playerControls.Player.Disable();
        }

        public void OnSteer(InputAction.CallbackContext context)
        {
            Steer = context.ReadValue<float>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>().ToVector3WithoutZ();
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            IsFiring = context.ReadValueAsButton();
        }

        public void OnThrottle(InputAction.CallbackContext context)
        {
            IsThrottling = context.ReadValueAsButton();
        }

        public void OnBreak(InputAction.CallbackContext context)
        {
            IsBreaking = context.ReadValueAsButton();
        }
    }
}
