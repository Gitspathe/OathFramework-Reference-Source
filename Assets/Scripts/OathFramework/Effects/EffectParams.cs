using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Effect Params", menuName = "ScriptableObjects/Effects/Effect Params", order = 1)]
    public class EffectParams : ScriptableObject
    {
        [field: SerializeField] public string Key       { get; private set; }
        [field: SerializeField] public ushort DefaultID { get; private set; }
        
        public ushort ID { get; set; }
    }
}
