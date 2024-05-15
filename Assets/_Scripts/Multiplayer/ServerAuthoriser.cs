using System.Collections.Generic;
using Assets._Scripts.Player;
using Assets._Scripts.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Assets._Scripts.Multiplayer
{
    [RequireComponent(typeof(PlayerMovement))]
    public class ServerAuthoriser : MonoBehaviour
    {

        #region Components

        private PlayerManager _playerManager;

        #endregion

        #region NetcodeServerSide

        private int _bufferSize;
        private CircularBuffer<MovementStatePayload> _serverStateBuffer;
        private Queue<MovementInputPayload> _serverInputQueue;

        #endregion

        [ServerRpc]
        internal void SendToServerRpc(MovementInputPayload inputPayload)
        {
            _serverInputQueue.Enqueue(inputPayload);
        }

        internal void HandleServerTick()
        {
            int bufferIndex = -1;
            while (_serverInputQueue.Count > 0)
            {
                MovementInputPayload inputPayload = _serverInputQueue.Dequeue();

                bufferIndex = inputPayload.tick % _bufferSize;

                MovementStatePayload statePayload = _playerManager.playerMovement.SimulatePhysicsOnServer(ref inputPayload);
                _serverStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;
            _playerManager.SendStateToClient(_serverStateBuffer.Get(bufferIndex));
        }

        internal MovementStatePayload GetStateFromServer(int bufferIndex) => _serverStateBuffer.Get(bufferIndex);

        #region UnityMethods

        private void Awake()
        {
            _playerManager = GetComponent<PlayerManager>();

            _bufferSize = _playerManager.BufferSize;
            _serverStateBuffer = new CircularBuffer<MovementStatePayload>(_bufferSize);
            _serverInputQueue = new Queue<MovementInputPayload>();
        }

        #endregion
    }
}
