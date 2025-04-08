using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Map Decal Params Collection", menuName = "ScriptableObjects/Effects/Map Decal Params Collection", order = 1)]
    public class MapDecalParamsCollection : ScriptableObject
    { 
        public MapDecalParams[] collection;
    }
}
