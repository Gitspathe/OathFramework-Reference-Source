using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Ragdoll Params", menuName = "ScriptableObjects/Effects/Ragdoll Params", order = 1)]
    public class RagdollParams : ScriptableObject
    {
        [field: SerializeField] public PoolParams PrefabPool { get; private set; }
    }
}
