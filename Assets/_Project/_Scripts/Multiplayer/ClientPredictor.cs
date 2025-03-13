using CosmicClash.Player;
using CosmicClash.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CosmicClash.Multiplayer
{
    [RequireComponent(typeof(PlayerMovement))]
    public class ClientPredictor : NetworkBehaviour
    {
        #region Components

        private PlayerManager m_playerManager;

        #endregion

        #region NetcodeClientSideVariables

        private int m_bufferSize;
        private CircularBuffer<MovementInputPayload> m_clientInputBuffer;
        private CircularBuffer<MovementStatePayload> m_clientStateBuffer;
        private MovementStatePayload m_lastServerState;
        private MovementStatePayload m_lastProcessedState;

        #endregion

        internal void HandleClientTick(MovementInputPayload inputPayload)
        {
            if (!IsClient) return;
            int currentTick = m_playerManager.NetworkTimer.CurrentTick;
            int bufferIndex = currentTick % m_bufferSize;

            m_clientInputBuffer.Add(inputPayload, bufferIndex);

            MovementStatePayload statePayload = m_playerManager.playerMovement.ProcessInput(ref inputPayload);
            m_clientStateBuffer.Add(statePayload, bufferIndex);

            // m_playerManager.HandleServerReconciliation();
        }

        [ClientRpc]
        internal void SendToClientRpc(MovementStatePayload statePayload)
        {
            if (!IsOwner) return;
            m_lastServerState = statePayload;
        }

        internal bool ShouldReconcile()
        {
            bool isNewServerState = !m_lastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = !m_lastProcessedState.Equals(default) && !m_lastProcessedState.Equals(m_lastServerState);

            return isNewServerState && isLastStateUndefinedOrDifferent;
        }

        internal int GetBufferIndexOfLastState(int bufferSize) => m_lastServerState.tick % bufferSize;

        internal MovementStatePayload GetLastServerState() => m_lastServerState;

        internal MovementInputPayload GetInputAtBufferIndex(int bufferIndex) => m_clientInputBuffer.Get(bufferIndex);

        internal void SetLastProcessedState() => m_lastProcessedState = m_lastServerState;

        internal void AddToClientStateBuffer(MovementStatePayload statePayload, int bufferIndex) => m_clientStateBuffer.Add(statePayload, bufferIndex);

        internal float GetRotationErrorForBufferIndex(int bufferIndex, Quaternion rewindStateRotation)
        {
            return Quaternion.Angle(m_clientStateBuffer.Get(bufferIndex).rotation, rewindStateRotation);
        }

        internal float GetPositionErrorForBufferIndex(int bufferIndex, Vector3 rewindStatePosition)
        {
            return Vector3.Distance(m_clientStateBuffer.Get(bufferIndex).position, rewindStatePosition);
        }

        #region UnityMethods

        private void Awake()
        {
            m_playerManager = GetComponent<PlayerManager>();

            m_bufferSize = m_playerManager.BufferSize;
            m_clientInputBuffer = new CircularBuffer<MovementInputPayload>(m_bufferSize);
            m_clientStateBuffer = new CircularBuffer<MovementStatePayload>(m_bufferSize);
        }

        #endregion
        
    }
}
