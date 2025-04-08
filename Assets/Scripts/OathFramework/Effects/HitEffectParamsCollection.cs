using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Hit Effect Collection", menuName = "ScriptableObjects/Effects/Hit Effect Collection", order = 1)]
    public class HitEffectParamsCollection : ScriptableObject
    { 
        public HitEffectParams[] collection;
    }
}
