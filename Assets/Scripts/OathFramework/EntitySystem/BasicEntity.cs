using UnityEngine;

namespace OathFramework.EntitySystem
{
    public class BasicEntity : MonoBehaviour, IEntity
    {
        [field: SerializeField] public EntityTeams Team { get; private set; } = EntityTeams.None;
        
        public bool IsDead                   { get; private set; }
        public bool IsOwner                  { get; private set; }
        public bool PlayedHitEffectThisFrame { get; set; }

        public bool TryGetEffectColorOverride(HitSurfaceMaterial material, out Color? color)
        {
            color = null;
            return false;
        }
        
        public void Damage(DamageValue val, HitEffectValue? effect)
        {
            
        }

        public void Die(DamageValue lastDamageVal)
        {
            
        }

        public void Heal(HealValue val, HitEffectValue? effect)
        {
            
        }
    }
}
