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

        #region RotationVariables

        [Header("Rotation")][SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private AnimationCurve _rotationSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        private Quaternion _playerRotation;
        private Quaternion _targetRotation;
        private float _timeToRotate;
        private float _elapsedTime;

        #endregion

        internal void ReconcileRewindState(ref StatePayload rewindState)
        {
            _playerTransform.position = rewindState.position;
            _playerTransform.rotation = rewindState.rotation;
            _playerRigidBody.velocity = rewindState.velocity;
            _playerRigidBody.angularVelocity = rewindState.angularVelocity;
        }

        internal StatePayload SimulatePhysicsOnServer(ref MovementInputPayload input)
        {
            Physics.simulationMode = SimulationMode.Script;

            Look(input.look);
            Physics.Simulate(Time.fixedDeltaTime);
            Physics.simulationMode = SimulationMode.FixedUpdate;

            return new StatePayload
            {
                tick = input.tick,
                position = _playerTransform.position,
                rotation = _playerTransform.rotation,
                velocity = _playerRigidBody.velocity,
                angularVelocity = _playerRigidBody.angularVelocity
            };
        }

        internal StatePayload ProcessInput(ref MovementInputPayload input)
        {
            Look(input.look);

            return new StatePayload
            {
                tick = input.tick,
                position = _playerTransform.position,
                rotation = _playerTransform.rotation,
                velocity = _playerRigidBody.velocity,
                angularVelocity = _playerRigidBody.angularVelocity
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

        private void Awake()
        {
            _playerTransform = transform;
            _playerRigidBody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
        }
    }
}
