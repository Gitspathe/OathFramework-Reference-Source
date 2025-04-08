using OathFramework.Data.StatParams;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{
    public static class EquippableGrenadeUtil
    {
        public static bool GetEquippable(Entity source, ushort equippableID, out Equippable equippable)
        {
            equippable              = null;
            PlayerController player = source.Controller as PlayerController;
            return player?.Equipment.TryGetEquippable(equippableID, out equippable) ?? false;
        }
        
        public static bool GetDamageValue(
            Entity source, 
            Equippable equippable, 
            ref float refRadius, 
            out List<HitEffectInfo> hitEffects, 
            in DamageValue fallback, 
            out DamageValue retDamage, 
            bool allowZero = false)
        {
            retDamage            = fallback;
            hitEffects           = null;
            IStatsRadius iRadius = equippable.GetStatsInterface<IStatsRadius>();
            float radius         = iRadius?.Radius ?? refRadius;
            radius              *= source.CurStats.GetParam(ExplosiveRangeMult.Instance);
            refRadius            = radius;
            hitEffects           = equippable.GetRootStats().Effects;
            IStatsDamage dam     = equippable.GetStatsInterface<IStatsDamage>();
            if(dam == null)
                return false;

            retDamage = new DamageValue(
                (ushort)Mathf.Clamp(dam.Damage * Random.Range(1.0f - dam.DamageRand, 1.0f + dam.DamageRand), allowZero ? 0.0f : 1.0f, ushort.MaxValue),
                fallback.HitPosition,
                fallback.Source,
                dam.StaggerStrength,
                dam.StaggerAmount,
                fallback.Flags,
                source,
                fallback.SpEvents
            );
            return true;
        }

        public static bool GetHitEffects(Entity source, Equippable equippable, out List<HitEffectInfo> hitEffects)
        {
            hitEffects = equippable.GetRootStats().Effects;
            return true;
        }
    }
}
