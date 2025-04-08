using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{
    [CreateAssetMenu(fileName = "Projectile Template", menuName = "ScriptableObjects/Projectile Template", order = 1)]
    public class ProjectileTemplate : ScriptableObject
    {
        [field: SerializeField] public PoolParams PoolParams { get; private set; }
        [field: SerializeField] public string LookupKey      { get; private set; }
        [field: SerializeField] public ushort DefaultID      { get; private set; }
        
        public ushort ID { get; set; }
    }
}
