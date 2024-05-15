using Unity.Netcode;
using UnityEngine;

namespace Assets._Scripts.Utils
{
    public struct MovementInputPayload : INetworkSerializable
    {
        public int tick;
        public Vector3 look;
        public bool throttle;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref look);
            serializer.SerializeValue(ref throttle);
        }
    }


    //public struct FireInputPayload : INetworkSerializable
    //{
    //    public int tick;
    //    public bool fire;

    //    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    //    {
    //        serializer.SerializeValue(ref tick);
    //        serializer.SerializeValue(ref fire);
    //    }
    //}


    public struct MovementStatePayload : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float currentFuel;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref angularVelocity);
            serializer.SerializeValue(ref currentFuel);
        }
    }
}
