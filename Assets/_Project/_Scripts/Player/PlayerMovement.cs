using System.Threading.Tasks;
using CosmicClash.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CosmicClash.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class PlayerMovement : NetworkBehaviour
    {
        private Transform m_tr;
        private Rigidbody m_rb;

        #region MovementVariables

        [Header("Movement")][SerializeField] private float m_acceleration = 5f;
        [SerializeField] private float m_deceleration = 5f;
        [SerializeField] private float m_maxSpeed = 5f;
        private Vector3 m_currentSpeed;

        #endregion

        #region FuelVariables

        [Header("Fuel")][SerializeField, Range(0, 100)] private float m_fuel;
        [SerializeField] private float m_fuelConsumptionRate;
        [SerializeField] private float m_fuelRegenerationRate;
        [SerializeField, Tooltip("Amount of time to wait before throttling is re-enabled")] private float m_waitForFuelRegeneration;

        private float m_currentFuel;
        private bool m_isThrottlingEnabled;

        #endregion

        #region RotationVariables

        [Header("Rotation")][SerializeField] private float m_rotationSpeed = 5f;
        [SerializeField] private AnimationCurve m_rotationSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        private Quaternion m_playerRotation;
        private Quaternion m_targetRotation;
        private float m_timeToRotate;
        private float m_elapsedTime;

        #endregion

        public void ReconcileRewindState(ref MovementStatePayload rewindState)
        {
            m_tr.position = rewindState.position;
            m_tr.rotation = rewindState.rotation;
            m_rb.velocity = rewindState.velocity;
            m_rb.angularVelocity = rewindState.angularVelocity;
            m_currentFuel = rewindState.currentFuel;
        }

        public MovementStatePayload SimulatePhysicsOnServer(ref MovementInputPayload input)
        {
            Physics.simulationMode = SimulationMode.Script;

            Look(input.look);
            Throttle(input.throttle);
            Physics.Simulate(Time.fixedDeltaTime);

            Physics.simulationMode = SimulationMode.FixedUpdate;

            return new MovementStatePayload
            {
                tick = input.tick,
                position = m_tr.position,
                rotation = m_tr.rotation,
                velocity = m_rb.velocity,
                angularVelocity = m_rb.angularVelocity,
                currentFuel = m_currentFuel
            };
        }

        public MovementStatePayload ProcessInput(ref MovementInputPayload input)
        {
            Look(input.look);
            Throttle(input.throttle);

            return new MovementStatePayload
            {
                tick = input.tick,
                position = m_tr.position,
                rotation = m_tr.rotation,
                velocity = m_rb.velocity,
                angularVelocity = m_rb.angularVelocity,
                currentFuel = m_currentFuel
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

            m_targetRotation = newTargetRotation;
            m_playerRotation = m_tr.rotation;

            m_tr.rotation = Quaternion.Slerp(m_playerRotation, m_targetRotation, m_rotationSpeed * Time.fixedDeltaTime);

            // TODO: Implement rotation with time 

            //_timeToRotate = Quaternion.Angle(_playerRotation, _targetRotation) / _rotationSpeed;

            //_elapsedTime = 0;

            //_elapsedTime += Time.fixedDeltaTime;
            //float percentage = _elapsedTime / _timeToRotate;
            //_playerTransform.rotation = Quaternion.Slerp(_playerRotation, _targetRotation, _rotationSpeedCurve.Evaluate(percentage));
        }

        private void Throttle(bool isThrottling)
        {
            m_currentSpeed = m_rb.velocity;

            if (isThrottling && m_currentFuel > 0f && m_isThrottlingEnabled)
            {
                m_currentSpeed.x = Mathf.MoveTowards(m_currentSpeed.x, m_tr.forward.x * m_maxSpeed,
                    m_acceleration * Time.fixedDeltaTime);
                m_currentSpeed.z = Mathf.MoveTowards(m_currentSpeed.z, m_tr.forward.z * m_maxSpeed,
                    m_acceleration * Time.fixedDeltaTime);
                m_currentFuel = Mathf.MoveTowards(m_currentFuel, 0, m_fuelConsumptionRate * Time.fixedDeltaTime);
                if (m_currentFuel == 0f)
                {
                    m_isThrottlingEnabled = false;
                    WaitForRegenerateFuel();
                }
            }
            else
            {
                m_currentSpeed.x = Mathf.MoveTowards(m_currentSpeed.x, 0, m_deceleration * Time.fixedDeltaTime);
                m_currentSpeed.z = Mathf.MoveTowards(m_currentSpeed.z, 0, m_deceleration * Time.fixedDeltaTime);
                m_currentFuel = Mathf.MoveTowards(m_currentFuel, m_fuel, m_fuelRegenerationRate * Time.fixedDeltaTime);
            }

            m_rb.velocity = m_currentSpeed;
        }

        private async void WaitForRegenerateFuel()
        {
            await Task.Delay((int)(m_waitForFuelRegeneration * 1000));
            m_isThrottlingEnabled = true;
        }

        private void Awake()
        {
            m_tr = transform;
            m_rb = GetComponent<Rigidbody>();

            m_currentFuel = m_fuel;
            m_isThrottlingEnabled = true;
        }
    }
}
