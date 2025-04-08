using OathFramework.Effects;
using OathFramework.Pooling;
using System.Collections.Generic;
using UnityEngine;
using MinMaxGradient = UnityEngine.ParticleSystem.MinMaxGradient;

namespace OathFramework.EntitySystem.Projectiles
{
    public partial class StdBulletData : 
        IProjectileData, IProjectileDataVFX
    {
        public IEntity Source             { get; private set; }
        public ushort BaseDamage          { get; set; }
        public ushort ExtraData           => EquippableID;
        
        public EffectParams VFX           { get; private set; }
        public MinMaxGradient EffectColor { get; private set; }

        public ushort EquippableID;
        public float Speed;
        public float Penetration;
        public float MinDistance;
        public float MaxDistance;
        public StaggerStrength StaggerStrength;
        public ushort StaggerAmount;
        public AnimationCurve DistanceMod;
        public List<HitEffectInfo> EffectOverrides;

        public bool IsInstant => Speed > ushort.MaxValue;
        
        public bool TryGetEffectOverride(HitSurfaceMaterial material, out HitEffectInfo effectOverride)
        {
            effectOverride = null;
            if(EffectOverrides == null || EffectOverrides.Count == 0)
                return false;
            
            foreach(HitEffectInfo effectInfo in EffectOverrides) {
                if(!effectInfo.ContainsMaterial(material))
                    continue;
                
                effectOverride = effectInfo;
                return true;
            }
            return false;
        }

        public void SetData(
            Entity source,
            ushort equippableID,
            EffectParams vfx,
            MinMaxGradient effectColor,
            ushort baseDamage,
            float speed,
            float penetration,
            float minDistance,
            float maxDistance,
            StaggerStrength staggerStrength,
            ushort staggerAmount,
            AnimationCurve distanceMod,
            List<HitEffectInfo> effectOverrides = null)
        {
            Source          = source;
            EquippableID    = equippableID;
            VFX             = vfx;
            EffectColor     = effectColor;
            BaseDamage      = baseDamage;
            Speed           = speed;
            Penetration     = penetration;
            MinDistance     = minDistance;
            MaxDistance     = maxDistance;
            StaggerStrength = staggerStrength;
            StaggerAmount   = staggerAmount;
            DistanceMod     = distanceMod;
            EffectOverrides = effectOverrides;
        }
        
        public static StdBulletData Retrieve() => StaticObjectPool<StdBulletData>.Retrieve();

        public void Return()
        {
            Source          = null;
            VFX             = null;
            EffectColor     = null;
            DistanceMod     = null;
            EffectOverrides = null;
            StaticObjectPool<StdBulletData>.Return(this);
        }

        public StdBulletData() { }
    }
}
