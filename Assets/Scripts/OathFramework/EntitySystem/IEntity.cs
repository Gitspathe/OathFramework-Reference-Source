using UnityEngine;

namespace OathFramework.EntitySystem
{
    public interface IEntity
    {
        EntityTeams Team              { get; }
        bool PlayedHitEffectThisFrame { get; set; }
        bool IsDead                   { get; }
        bool IsOwner                  { get; }
        
        void Damage(DamageValue val, HitEffectValue? effect = null);
        void Die(DamageValue lastDamageVal);
        void Heal(HealValue val, HitEffectValue? effect = null);
        bool TryGetEffectColorOverride(HitSurfaceMaterial material, out Color? color);
    }
}
