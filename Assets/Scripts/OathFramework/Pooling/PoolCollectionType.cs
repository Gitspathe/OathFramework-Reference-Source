using System;
using UnityEngine;

namespace OathFramework.Pooling
{
    [CreateAssetMenu(fileName = "Pool Collection Type", menuName = "ScriptableObjects/Pool Collection Type", order = 1)]
    public class PoolCollectionType : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
    }
}
