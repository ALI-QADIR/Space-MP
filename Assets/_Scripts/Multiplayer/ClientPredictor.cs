using UnityEngine;
using Assets._Scripts.Player;
using Assets._Scripts.Utils;
using Unity.Netcode;

namespace Assets._Scripts.Multiplayer
{
    [RequireComponent(typeof(PlayerMovement))]
    public class ClientPredictor : NetworkBehaviour
    {
        #region Components

        private PlayerManager _playerManager;

        #endregion

        #region NetcodeClientSideVariables

        private int _bufferSize;
        private CircularBuffer<MovementInputPayload> _clientInputBuffer;
        private CircularBuffer<MovementStatePayload> _clientStateBuffer;
        private MovementStatePayload _lastServerState;
        private MovementStatePayload _lastProcessedState;

        #endregion

        internal void HandleClientTick(MovementInputPayload inputPayload)
        {
            if (!IsClient) return;
            int currentTick = _playerManager.NetworkTimer.CurrentTick;
            int bufferIndex = currentTick % _bufferSize;

            _clientInputBuffer.Add(inputPayload, bufferIndex);

            MovementStatePayload statePayload = _playerManager.playerMovement.ProcessInput(ref inputPayload);
            _clientStateBuffer.Add(statePayload, bufferIndex);

            _playerManager.HandleServerReconciliation();
        }

        [ClientRpc]
        internal void SendToClientRpc(MovementStatePayload statePayload)
        {
            if (!IsOwner) return;
            _lastServerState = statePayload;
        }

        internal bool ShouldReconcile()
        {
            bool isNewServerState = !_lastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = !_lastProcessedState.Equals(default) && !_lastProcessedState.Equals(_lastServerState);

            return isNewServerState && isLastStateUndefinedOrDifferent;
        }

        internal int GetBufferIndexOfLastState(int bufferSize) => _lastServerState.tick % bufferSize;

        internal MovementStatePayload GetLastServerState() => _lastServerState;

        internal MovementInputPayload GetInputAtBufferIndex(int bufferIndex) => _clientInputBuffer.Get(bufferIndex);

        internal void SetLastProcessedState() => _lastProcessedState = _lastServerState;

        internal void AddToClientStateBuffer(MovementStatePayload statePayload, int bufferIndex) => _clientStateBuffer.Add(statePayload, bufferIndex);

        internal float GetRotationErrorForBufferIndex(int bufferIndex, Quaternion rewindStateRotation)
        {
            return Quaternion.Angle(_clientStateBuffer.Get(bufferIndex).rotation, rewindStateRotation);
        }

        internal float GetPositionErrorForBufferIndex(int bufferIndex, Vector3 rewindStatePosition)
        {
            return Vector3.Distance(_clientStateBuffer.Get(bufferIndex).position, rewindStatePosition);
        }

        #region UnityMethods

        private void Awake()
        {
            _playerManager = GetComponent<PlayerManager>();

            _bufferSize = _playerManager.BufferSize;
            _clientInputBuffer = new CircularBuffer<MovementInputPayload>(_bufferSize);
            _clientStateBuffer = new CircularBuffer<MovementStatePayload>(_bufferSize);
        }

        #endregion
        
    }
}
