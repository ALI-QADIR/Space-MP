using System;
using CosmicClash.Multiplayer;
using CosmicClash.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CosmicClash.Player
{
    [RequireComponent(typeof(ClientPredictor))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(ServerAuthoriser))]
    public class PlayerManager : NetworkBehaviour
    {
        #region Components

        private ClientPredictor m_clientPredictor;
        private ServerAuthoriser m_serverAuthoriser;
        private PlayerInput m_playerInput;
        [HideInInspector] public PlayerMovement playerMovement;

        #endregion

        #region InputVariables

        private Vector3 m_lookInput;
        private float m_steerInput;
        private bool m_throttleInput;
        private bool m_breakInput;
        private bool m_fireInput;
        
        #endregion

        #region WeaponVariables

        [Header("Weapon")][SerializeField] private Bullet m_bulletPrefab;
        [SerializeField] private float m_ammo;
        [SerializeField] private float m_fireRate;
        [SerializeField] private float m_ammoDepletionRate;
        [SerializeField] private float m_ammoRefillRate;
        [SerializeField, Tooltip("Amount of time to wait before firing is re-enabled")] private float m_waitForAmmoRefill;

        private float m_currentAmmo;
        private bool m_isFiringEnabled;

        #endregion

        #region ReconciliationVariables

        [Header("Reconciliation")] [SerializeField] private float m_rotationReconciliationThreshold = 5f;
        [SerializeField] private float m_positionReconciliationThreshold = 0.5f;

        #endregion

        #region NetworkGeneral

        private NetworkTimer m_networkTimer;
        public ref NetworkTimer NetworkTimer => ref m_networkTimer;

        private const float k_ServerTickRate = 60f;
        private const int k_BufferSize = 1024;
        public int BufferSize => k_BufferSize;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (!m_clientPredictor || !m_serverAuthoriser || !m_playerInput || !playerMovement) AddComponents();
        }

        private void Update()
        {
            GatherInput();
        }

        #endregion Unity Methods
        
        #region Private Methods

        private void GatherInput()
        {
            m_lookInput = m_playerInput.LookInput;
            m_steerInput = m_playerInput.Steer;
            m_throttleInput = m_playerInput.IsThrottling;
            m_breakInput = m_playerInput.IsBreaking;
            m_fireInput = m_playerInput.IsFiring;
        }

        #endregion Private Methods
        
        // Add missing components if necessary
        #region Component Validation

        private void OnValidate()
        {
            AddComponents();
        }

        private void AddComponents()
        {
            try
            {
                if (!GetComponent<Rigidbody>()) gameObject.AddComponent<Rigidbody>();
            }
            catch (Exception e)
            {
                Debug.LogError("Could not add Rigid body component." + e);
            }

            try
            {
                if (!GetComponent<Collider>()) gameObject.AddComponent<Collider>();
            }
            catch (Exception e)
            {
                Debug.LogError("Could not add Collider component." + e);
            }

            try
            {
                m_playerInput = GetComponent<PlayerInput>();
                if (!m_playerInput) m_playerInput = gameObject.AddComponent<PlayerInput>();

            }
            catch (Exception e)
            {
                Debug.LogError("Could not add PlayerInput component." + e);
            }
            
            try
            {
                m_serverAuthoriser = GetComponent<ServerAuthoriser>();
                if (!m_serverAuthoriser) m_serverAuthoriser = gameObject.AddComponent<ServerAuthoriser>();

            }
            catch (Exception e)
            {
                Debug.LogError("Could not add ServerAuthoriser component." + e);
            }
            
            try
            {
                m_clientPredictor = GetComponent<ClientPredictor>();
                if (!m_clientPredictor) m_clientPredictor = gameObject.AddComponent<ClientPredictor>();

            }
            catch (Exception e)
            {
                Debug.LogError("Could not add ClientPredictor component." + e);
            }
        }
        
        #endregion Component Validation
    }
}