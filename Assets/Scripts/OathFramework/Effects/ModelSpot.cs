using Sirenix.OdinInspector;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace OathFramework.Effects
{
    [Serializable]
    public class ModelSpot
    {
        [SerializeField, HideInInspector] private byte id;
        
#if UNITY_EDITOR
        [SerializeField, HideInInspector] private string name;
#endif
        
        [field: LabelText("@this.GetTitle()")]
        [field: SerializeField] public Transform Transform { get; private set; }

        public byte ID => id;
        
        public ModelSpot() { }

        [Preserve, MethodImpl(MethodImplOptions.NoOptimization)]
        private string GetTitle()
        {
#if UNITY_EDITOR
            return $"{id} : {name}";
#else
            return "";
#endif
        }

        public ModelSpot(byte id, string name, Transform transform)
        {
            this.id   = id;
#if UNITY_EDITOR
            this.name = name;
#endif
            Transform = transform;
        }
    }
}
