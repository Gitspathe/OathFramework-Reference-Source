using OathFramework.Effects;
using UnityEngine;
using MinMaxGradient = UnityEngine.ParticleSystem.MinMaxGradient;

namespace OathFramework.EntitySystem.Projectiles
{
    public interface IProjectile
    {
        public IProjectileData ProjectileData { get; }
        void Initialize(bool isOwner, EntityTeams[] targets, in IProjectileData data, float? distanceOverride = null);
    }

    public interface IProjectileData
    {
        public IEntity Source             { get; }
        public ushort BaseDamage          { get; set; }
        public ushort ExtraData           { get; }
    }

    public interface IProjectileDataVFX
    {
        public EffectParams VFX           { get; }
        public MinMaxGradient EffectColor { get; }
    }
}
