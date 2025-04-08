using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{
    public class GrenadeProjectileData : IProjectileData
    {
        public IEntity Source    { get; private set; }
        public ushort BaseDamage { get; set; }
        public ushort ExtraData  => EquippableID;

        public ushort EquippableID;
        public ushort ExplosionEffectID;
        public StaggerStrength StaggerStrength;
        public Vector3 Force;
        public float Radius;
        public ushort StaggerAmount;
        
        public void SetData(
            Entity source,
            ushort baseDamage,
            ushort equippableID,
            ushort explosionEffectID,
            Vector3 force,
            float radius,
            StaggerStrength staggerStrength,
            ushort staggerAmount)
        {
            Source            = source;
            BaseDamage        = baseDamage;
            EquippableID      = equippableID;
            ExplosionEffectID = explosionEffectID;
            Force             = force;
            Radius            = radius;
            StaggerStrength   = staggerStrength;
            StaggerAmount     = staggerAmount;
        }
        
        public static GrenadeProjectileData Retrieve() => StaticObjectPool<GrenadeProjectileData>.Retrieve();

        public void Return()
        {
            Source = null;
            StaticObjectPool<GrenadeProjectileData>.Return(this);
        }
    }
}
