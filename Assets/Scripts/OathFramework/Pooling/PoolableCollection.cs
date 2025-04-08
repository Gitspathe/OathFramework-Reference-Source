using UnityEngine;
using OathFramework.Attributes;

namespace OathFramework.Pooling
{ 

    [CreateAssetMenu(fileName = "Poolable Collection", menuName = "ScriptableObjects/Poolable Collection", order = 1)]
    public class PoolableCollection : ScriptableObject
    {
        [ArrayElementTitle] public PoolManager.GameObjectPool[] pools;
    }

}
