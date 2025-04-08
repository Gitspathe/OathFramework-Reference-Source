using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{ 

    public static class ProjectileUtils
    {
        public static ushort DamageDistanceFunc(ushort damage, float distance, float minDistance, float maxDistance, AnimationCurve distanceFalloff) 
            => (ushort)((float)damage * (float)distanceFalloff.Evaluate(Mathf.Clamp((distance - minDistance) / (maxDistance - minDistance), 0.0f, 1.0f)));
                        // DO NOT REMOVE THE FLOAT CASTS, RIDER LIES!!!

        public static void ProjectileDespawned(IProjectile projectile, bool missed)
        {
            if(ReferenceEquals(projectile.ProjectileData.Source, null) || projectile.ProjectileData.Source.IsDead)
                return;
            
            IEntity iEntity = projectile.ProjectileData.Source;
            if(iEntity is Entity e && e.Controller is IEquipmentUserController equipmentUser) {
                equipmentUser.Equipment.Projectiles.OnProjectileDespawned(projectile, missed);
            }
        }
    }

}
