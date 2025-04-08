using UnityEngine;

namespace OathFramework.Pooling
{
    [CreateAssetMenu(fileName = "Pool Params", menuName = "ScriptableObjects/Pool Params", order = 1)]
    public class PoolParamsSO : ScriptableObject
    {
        [field: SerializeField] public PoolParams @Params { get; private set; }
    }
}
