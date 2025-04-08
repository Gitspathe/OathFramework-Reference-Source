using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Utility
{
    public struct HalfVector3 : INetworkSerializable
    {
        private ushort xHalf;
        private ushort yHalf;
        private ushort zHalf;

        public float X {
            get => Mathf.HalfToFloat(xHalf);
            set => xHalf = Mathf.FloatToHalf(value);
        }

        public float Y {
            get => Mathf.HalfToFloat(yHalf);
            set => yHalf = Mathf.FloatToHalf(value);
        }

        public float Z {
            get => Mathf.HalfToFloat(zHalf);
            set => zHalf = Mathf.FloatToHalf(value);
        }
        
        public HalfVector3(float x, float y, float z)
        {
            xHalf = Mathf.FloatToHalf(x);
            yHalf = Mathf.FloatToHalf(y);
            zHalf = Mathf.FloatToHalf(z);
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref xHalf);
            serializer.SerializeValue(ref yHalf);
            serializer.SerializeValue(ref zHalf);
        }
        
        public static explicit operator HalfVector3(Vector3 vec)
        {
            return new HalfVector3(vec.x, vec.y, vec.z);
        }

        public static explicit operator Vector3(HalfVector3 vec) => new(vec.X, vec.Y, vec.Z);
    }
    
    public struct HalfQuaternion : INetworkSerializable
    {
        private ushort xHalf;
        private ushort yHalf;
        private ushort zHalf;

        public float X {
            get => Mathf.HalfToFloat(xHalf);
            set => xHalf = Mathf.FloatToHalf(value);
        }

        public float Y {
            get => Mathf.HalfToFloat(yHalf);
            set => yHalf = Mathf.FloatToHalf(value);
        }

        public float Z {
            get => Mathf.HalfToFloat(zHalf);
            set => zHalf = Mathf.FloatToHalf(value);
        }

        public HalfQuaternion(float x, float y, float z)
        {
            xHalf = Mathf.FloatToHalf(x);
            yHalf = Mathf.FloatToHalf(y);
            zHalf = Mathf.FloatToHalf(z);
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref xHalf);
            serializer.SerializeValue(ref yHalf);
            serializer.SerializeValue(ref zHalf);
        }

        public static explicit operator HalfQuaternion(Quaternion q)
        {
            Vector3 euler = q.eulerAngles;
            return new HalfQuaternion(euler.x, euler.y, euler.z);
        }

        public static explicit operator Quaternion(HalfQuaternion q) => Quaternion.Euler(q.X, q.Y, q.Z);
    }
}
