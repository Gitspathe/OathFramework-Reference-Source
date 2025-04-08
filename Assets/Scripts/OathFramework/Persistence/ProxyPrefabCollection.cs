using OathFramework.Attributes;
using System;
using UnityEngine;

namespace OathFramework.Persistence
{
    [CreateAssetMenu(fileName = "Persistent Proxy Prefab Collection", menuName = "ScriptableObjects/Persistence/Proxy Prefab Collection", order = 1)]
    public class ProxyPrefabCollection : ScriptableObject
    {
        [field: SerializeField] public Node[] Proxies { get; private set; }

        [Serializable]
        public class Node : IArrayElementTitle
        {
            [field: SerializeField] public string ID              { get; private set; }
            [field: SerializeField] public GameObject ProxyPrefab { get; private set; }
            
#if UNITY_EDITOR
            [field: SerializeField] public string DropdownValue { get; private set; }
#endif
            
            string IArrayElementTitle.Name => ID;
        }
    }
}
