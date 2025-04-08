using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Projectiles;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{
    public class EquippableGrenade : Equippable, 
        IProjectileDataProvider
    {
        public override EquippableTypes Type => EquippableTypes.Grenade;

        [field: SerializeField] public EquippableGrenadeStats BaseStats { get; private set; }
        
        [field: HideInEditorMode]
        [field: SerializeField] public EquippableGrenadeStats Stats     { get; private set; }

        public override EquippableStats GetRootStats() => Stats;
        
        private void Awake()
        {
            Stats = new EquippableGrenadeStats();
            BaseStats.CopyTo(Stats);
        }

        protected override void OnTakeOut(EntityEquipment equipment)
        {
            if(!(equipment.Model is IEntityModelThrow mThrow))
                return;
            
            mThrow.SetAimAnim(true);
        }

        protected override void OnPutAway(EntityEquipment equipment)
        {
            if(!(equipment.Model is IEntityModelThrow mThrow))
                return;

            mThrow.SetAimAnim(false);
            EquippableManager.ReturnTrajectoryArc(mThrow.TrajectoryArc);
        }
        
        public override void RegisterToProjectileProvider(ProjectileProvider projectileProvider) 
            => projectileProvider.RegisterProvider(Stats.Projectile.ID, this);
        public override void UnregisterFromProjectileProvider(ProjectileProvider projectileProvider) 
            => projectileProvider.UnregisterProvider(Stats.Projectile.ID);

        public GrenadeProjectileData GetGrenadeProjectileData(Entity entity, ushort equippableID) 
            => Stats.GetGrenadeProjectileData(entity, equippableID);
        public IProjectileData GetProjectileData(Entity entity, ushort extraData) 
            => GetGrenadeProjectileData(entity, extraData);
    }

    [Serializable]
    public class EquippableGrenadeStats : EquippableStats, 
        IProjectileDataProvider, IStatsDamage, IStatsProjectile, 
        IStatsRadius
    {
        [field: Header("Equippable Stats (Thrown)")]
        [field: SerializeField] public float TimeBetween               { get; private set; } = 1.0f;
        
        [field: Header("Main Stats")]
        
        [field: SerializeField] public ProjectileTemplate Projectile   { get; set; }
        [field: SerializeField] public EffectParams Explosion          { get; set; }
        
        [field: SerializeField] public float Radius                    { get; private set; } = 6.0f;
        [field: SerializeField] public float Damage                    { get; private set; } = 100.0f;
        [field: SerializeField] public int Pellets                     { get; private set; } = 1;
        
        [field: Space(5)]
        
        [field: SerializeField] public StaggerStrength StaggerStrength { get; set; }
        [field: SerializeField] public ushort StaggerAmount            { get; set; }

        public float DamageRand => 0.0f;
        public float FireRate   => 0.0f;
        
        public override float GetTimeBetweenUses(float useRateMult) => TimeBetween * useRateMult;
        
        public IProjectileData GetProjectileData(Entity entity, ushort extraData) 
            => GetGrenadeProjectileData(entity, extraData);

        public GrenadeProjectileData GetGrenadeProjectileData(Entity source, ushort equippableID)
        {
            if(!(source.EntityModel is IEntityModelThrow mThrow)) {
                Debug.LogError($"{source.EntityModel} does not implement {nameof(IEntityModelThrow)}");
                return null;
            }
            
            GrenadeProjectileData data = GrenadeProjectileData.Retrieve();
            data.SetData(
                source, 
                (ushort)Mathf.Clamp(Damage, 0.0f, ushort.MaxValue - 1.0f), 
                equippableID,
                Explosion.ID,
                mThrow.GetThrowStrength(), 
                Radius, 
                StaggerStrength, 
                StaggerAmount
            );
            return data;
        }

        public override void CopyTo(EquippableStats other)
        {
            base.CopyTo(other);
            EquippableGrenadeStats otherDerived = other.As<EquippableGrenadeStats>();
            otherDerived.TimeBetween            = TimeBetween;
            otherDerived.Projectile             = Projectile;
            otherDerived.Explosion              = Explosion;
            otherDerived.TimeBetween            = TimeBetween;
            otherDerived.Radius                 = Radius;
            otherDerived.Damage                 = Damage;
            otherDerived.Pellets                = Pellets;
            otherDerived.StaggerStrength        = StaggerStrength;
            otherDerived.StaggerAmount          = StaggerAmount;
        }
    }
}
