using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Effect Collection", menuName = "ScriptableObjects/Effects/Effect Collection", order = 1)]
    public class EffectCollection : ScriptableObject
    {
        public PoolParams[] pools;
    }
}
