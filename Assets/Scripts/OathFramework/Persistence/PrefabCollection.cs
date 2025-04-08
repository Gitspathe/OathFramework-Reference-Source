using OathFramework.Attributes;
using System;
using UnityEngine;

namespace OathFramework.Persistence
{
    [CreateAssetMenu(fileName = "Persistent Prefab Collection", menuName = "ScriptableObjects/Persistence/Prefab Collection", order = 1)]
    public class PrefabCollection : ScriptableObject
    {
        [field: SerializeField] public Node[] Prefabs { get; private set; }

        [Serializable]
        public class Node : IArrayElementTitle
        {
            [field: SerializeField] public string ID         { get; private set; }
            [field: SerializeField] public GameObject Prefab { get; private set; }
            
#if UNITY_EDITOR
            [field: SerializeField] public string DropdownValue { get; private set; }
#endif
            
            string IArrayElementTitle.Name => ID;
        }
    }
}
