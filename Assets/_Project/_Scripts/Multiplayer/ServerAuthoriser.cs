using System.Collections.Generic;
using CosmicClash.Player;
using CosmicClash.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CosmicClash.Multiplayer
{
    [RequireComponent(typeof(PlayerMovement))]
    public class ServerAuthoriser : MonoBehaviour
    {

        #region Components

        private PlayerManager m_playerManager;

        #endregion

        #region NetcodeServerSide

        private int m_bufferSize;
        private CircularBuffer<MovementStatePayload> m_serverStateBuffer;
        private Queue<MovementInputPayload> m_serverInputQueue;

        #endregion

        [ServerRpc]
        internal void SendToServerRpc(MovementInputPayload inputPayload)
        {
            m_serverInputQueue.Enqueue(inputPayload);
        }

        internal void HandleServerTick()
        {
            int bufferIndex = -1;
            while (m_serverInputQueue.Count > 0)
            {
                MovementInputPayload inputPayload = m_serverInputQueue.Dequeue();

                bufferIndex = inputPayload.tick % m_bufferSize;

                MovementStatePayload statePayload = m_playerManager.playerMovement.SimulatePhysicsOnServer(ref inputPayload);
                m_serverStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;
            // m_playerManager.SendStateToClient(m_serverStateBuffer.Get(bufferIndex));
        }

        internal MovementStatePayload GetStateFromServer(int bufferIndex) => m_serverStateBuffer.Get(bufferIndex);

        #region UnityMethods

        private void Awake()
        {
            m_playerManager = GetComponent<PlayerManager>();

            m_bufferSize = m_playerManager.BufferSize;
            m_serverStateBuffer = new CircularBuffer<MovementStatePayload>(m_bufferSize);
            m_serverInputQueue = new Queue<MovementInputPayload>();
        }

        #endregion
    }
}
