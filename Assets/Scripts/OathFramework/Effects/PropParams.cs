using System;
using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Prop Params", menuName = "ScriptableObjects/Effects/Prop Params", order = 1)]
    public class PropParams : ScriptableObject
    {
        [field: SerializeField] public string Key           { get; private set; }
        [field: SerializeField] public ushort DefaultID     { get; private set; }
        [field: SerializeField] public OffsetNode[] Offsets { get; private set; }
        
        public ushort ID { get; set; }

        public bool TryGetOffset(int modelSpot, out Vector3 offset, out Quaternion rotOffset)
        {
            offset    = Vector3.zero;
            rotOffset = Quaternion.identity;
            for(int i = 0; i < Offsets.Length; i++) {
                if(Offsets[i].ModelSpot == modelSpot) {
                    offset    = Offsets[i].Offset;
                    rotOffset = Offsets[i].RotOffset;
                    return true;
                }
            }
            return false;
        }

        [Serializable]
        public class OffsetNode
        {
            [field: SerializeField] public int ModelSpot        { get; private set; }
            [field: SerializeField] public Vector3 Offset       { get; private set; }
            [field: SerializeField] public Quaternion RotOffset { get; private set; }
        }
    }
}
