using OathFramework.Pooling;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Map Decal Params", menuName = "ScriptableObjects/Effects/Map Decal Params", order = 1)]
    public class MapDecalParams : ScriptableObject, IStringDropdownValue
    {
        [field: SerializeField] public string Key         { get; private set; }
        [field: SerializeField] public ushort DefaultID   { get; private set; }
        [field: SerializeField] public string DropdownVal { get; private set; }
    
        [field: Space(10)]
    
        [field: SerializeField] public PoolParams PrefabPool { get; private set; }

        public ushort ID         { get; set; }
        public string TrueVal    => Key;
        public GameObject Prefab => PrefabPool.Prefab;
    }
}
