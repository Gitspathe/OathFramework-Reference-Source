using OathFramework.Pooling;
using System;
using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Prop Collection", menuName = "ScriptableObjects/Effects/Prop Collection", order = 1)]
    public class PropCollection : ScriptableObject
    {
        public PoolParams[] pools;
    }
}
