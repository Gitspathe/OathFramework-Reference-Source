using OathFramework.Audio;
using OathFramework.Pooling;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Hit Effect Params", menuName = "ScriptableObjects/Effects/Hit Effect Params", order = 1)]
    public class HitEffectParams : ScriptableObject, IStringDropdownValue
    {
        [field: SerializeField] public string Key         { get; private set; }
        [field: SerializeField] public ushort DefaultID   { get; private set; }
        [field: SerializeField] public string DropdownVal { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public PoolParams PrefabPool { get; private set; }
        [field: SerializeField] public AudioParams Audio     { get; private set; }
        [field: SerializeField] public bool RotateToSource   { get; private set; } = true;

        public ushort ID         { get; set; }
        public string TrueVal    => Key;
        public GameObject Prefab => PrefabPool.Prefab;
    }
}
