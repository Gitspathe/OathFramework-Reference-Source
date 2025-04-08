using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Projectiles;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using MinMaxGradient = UnityEngine.ParticleSystem.MinMaxGradient;

namespace OathFramework.EquipmentSystem
{
    public class EquippableRanged : Equippable, IProjectileDataProvider
    {
        public override EquippableTypes Type => Stats.Pellets <= 1 ? EquippableTypes.Ranged : EquippableTypes.RangedMultiShot;
        
        [field: SerializeField] public EquippableRangedStats BaseStats { get; private set; }
        
        [field: HideInEditorMode]
        [field: SerializeField] public EquippableRangedStats Stats     { get; private set; }

        public override EquippableStats GetRootStats() => Stats;
        
        private void Awake()
        {
            Stats = new EquippableRangedStats();
            BaseStats.CopyTo(Stats);
        }
        
        public override void RegisterToProjectileProvider(ProjectileProvider projectileProvider) 
            => projectileProvider.RegisterProvider(Stats.Projectile.ID, this);
        public override void UnregisterFromProjectileProvider(ProjectileProvider projectileProvider) 
            => projectileProvider.UnregisterProvider(Stats.Projectile.ID);

        public StdBulletData GetStdBulletData(Entity entity, ushort extraData) 
            => Stats.GetStdBulletData(entity, extraData);
        public IProjectileData GetProjectileData(Entity entity,  ushort extraData) 
            => GetStdBulletData(entity, extraData);
    }

    [Serializable]
    public class EquippableRangedStats : EquippableStats, 
        IProjectileDataProvider, IStatsDamage, IStatsPenetration, 
        IStatsReload, IStatsAccuracy, IStatsProjectile
    {
        [field: Header("Equippable Stats (Ranged)")]
        
        [field: SerializeField] public EffectParams VFX                { get; set; }
        [field: SerializeField] public MinMaxGradient EffectColor      { get; set; }
        
        [field: Header("Main Stats")]

        [field: SerializeField] public ProjectileTemplate Projectile   { get; set; }
        [field: SerializeField] public float Damage                    { get; set; } = 10.0f;

        [field: SerializeField] public float DamageRand                { get; set; } = 0.15f;
        [field: SerializeField] public float FireRate                  { get; set; } = 2.0f;
        [field: SerializeField] public float ProjectileSpeed           { get; set; } = ushort.MaxValue + 1.0f;
        [field: SerializeField] public float ProjectileSpeedRand       { get; set; }

        [field: Space(5)]
        
        [field: SerializeField] public int Pellets                     { get; set; } = 1;
        [field: SerializeField] public float PelletSpread              { get; set; }
        
        [field: Space(5)]

        [field: SerializeField] public float Penetration               { get; set; } = 100.0f;

        [field: Space(5)]

        [field: SerializeField] public float CriticalChance            { get; set; } = 5.0f;
        [field: SerializeField] public float CriticalMult              { get; set; } = 2.0f;

        [field: Space(5)]

        [field: SerializeField] public float Accuracy                  { get; set; } = 1.0f;
        [field: SerializeField] public float MoveAccuracyPenalty       { get; set; } = 0.1f;
        [field: SerializeField] public float MoveAccuracyPenaltyTime   { get; set; } = 0.33f;
        [field: SerializeField] public float Recoil                    { get; set; } = 0.1f;
        [field: SerializeField] public float RecoilTime                { get; set; } = 0.25f;
        [field: SerializeField] public float MaxRecoil                 { get; set; } = 1.0f;
        [field: SerializeField] public float RecoilLoss                { get; set; } = 0.5f;

        [field: Space(5)]

        [field: SerializeField] public float ReloadBeginTime           { get; set; }
        [field: SerializeField] public float ReloadTime                { get; set; } = 2.0f;
        [field: SerializeField] public float ReloadEndTime             { get; set; }
        [field: SerializeField] public bool ReloadIndividually         { get; set; }
        [field: SerializeField] public bool ReloadInterrupt            { get; set; }
        [field: SerializeField] public bool ReloadImmediately          { get; set; }

        [field: SerializeField] public float ReloadUseDelay            { get; set; } = 0.125f;
        [field: SerializeField] public float AntiReloadInterrupt       { get; set; } = 0.5f;

        [field: Space(5)]
        
        [field: SerializeField] public StaggerStrength StaggerStrength { get; set; }
        [field: SerializeField] public ushort StaggerAmount            { get; set; }
        
        [field: Space(5)]
        
        [field: SerializeField] public float MinRange                  { get; set; }
        [field: SerializeField] public float MaxRange                  { get; set; } = 100.0f;
        [field: SerializeField] public AnimationCurve DamageCurve      { get; set; }

        [field: Header("Animation Timings")]
        
        [field: SerializeField] public float ReloadBeginClipTime       { get; set; }
        [field: SerializeField] public float ReloadClipTime            { get; set; } = 3.0f;
        [field: SerializeField] public float ReloadEndClipTime         { get; set; }

        public bool IsInstant => ProjectileSpeed >= ushort.MaxValue;
        
        public override float GetTimeBetweenUses(float fireRateMult) => 60.0f / (FireRate * fireRateMult);

        public float GetFullReloadTime(int ammo = 1, float mult = 1.0f)
        {
            if(Mathf.Abs(mult) < 0.001f)
                return 9999.0f;

            return ReloadIndividually 
                ? (ReloadBeginTime + ReloadEndTime + (ReloadTime * ammo)) / mult 
                : (ReloadBeginTime + ReloadTime + ReloadEndTime) / mult;
        }

        public float GetFullReloadClipTime(int ammo = 1, float mult = 1.0f)
        {
            if(Mathf.Abs(mult) < 0.001f)
                return 9999.0f;

            return ReloadIndividually
                ? (ReloadBeginClipTime + ReloadEndClipTime + (ReloadClipTime * ammo)) / mult 
                : (ReloadBeginClipTime + ReloadClipTime + ReloadEndClipTime) / mult;
        }

        public IProjectileData GetProjectileData(Entity entity, ushort equippableID) 
            => GetStdBulletData(entity, equippableID);

        public StdBulletData GetStdBulletData(Entity source, ushort equippableID)
        {
            FRandom rand = FRandom.Cache;
            float adjDamage = DamageRand == 0.0f 
                ? Damage 
                : Damage * rand.Range(1.0f - DamageRand, 1.0f + DamageRand);
            float adjSpeed = IsInstant || ProjectileSpeedRand == 0.0f 
                ? ProjectileSpeed 
                : ProjectileSpeed * rand.Range(1.0f - ProjectileSpeedRand, 1.0f + ProjectileSpeedRand);

            StdBulletData data = StdBulletData.Retrieve();
            data.SetData(
                source,
                equippableID,
                VFX,
                EffectColor,
                (ushort)Mathf.Clamp(adjDamage, 1.0f, ushort.MaxValue - 1.0f),
                Mathf.Clamp(adjSpeed, 1.0f, ushort.MaxValue + 1.0f),
                Mathf.Clamp(Penetration, 1.0f, float.MaxValue),
                Mathf.Clamp(MinRange, 1.0f, float.MaxValue),
                Mathf.Clamp(MaxRange, 1.0f, float.MaxValue),
                StaggerStrength,
                StaggerAmount,
                DamageCurve,
                Effects
            );
            return data;
        }
        
        public override void CopyTo(EquippableStats other)
        {
            base.CopyTo(other);
            EquippableRangedStats otherDerived = other.As<EquippableRangedStats>();
            otherDerived.VFX                   = VFX;
            otherDerived.EffectColor           = EffectColor;
            otherDerived.Projectile            = Projectile;
            otherDerived.Damage                = Damage;
            otherDerived.DamageRand            = DamageRand;
            otherDerived.FireRate              = FireRate;
            otherDerived.ProjectileSpeed       = ProjectileSpeed;
            otherDerived.ProjectileSpeedRand   = ProjectileSpeedRand;
            otherDerived.Pellets               = Pellets;
            otherDerived.PelletSpread          = PelletSpread;
            otherDerived.Penetration           = Penetration;
            otherDerived.CriticalChance        = CriticalChance;
            otherDerived.CriticalMult          = CriticalMult;
            otherDerived.Accuracy              = Accuracy;
            otherDerived.MoveAccuracyPenalty   = MoveAccuracyPenalty;
            otherDerived.Recoil                = Recoil;
            otherDerived.RecoilTime            = RecoilTime;
            otherDerived.MaxRecoil             = MaxRecoil;
            otherDerived.RecoilLoss            = RecoilLoss;
            otherDerived.ReloadBeginTime       = ReloadBeginTime;
            otherDerived.ReloadTime            = ReloadTime;
            otherDerived.ReloadEndTime         = ReloadEndTime;
            otherDerived.ReloadIndividually    = ReloadIndividually;
            otherDerived.ReloadInterrupt       = ReloadInterrupt;
            otherDerived.ReloadImmediately     = ReloadImmediately;
            otherDerived.ReloadUseDelay        = ReloadUseDelay;
            otherDerived.AntiReloadInterrupt   = AntiReloadInterrupt;
            otherDerived.StaggerStrength       = StaggerStrength;
            otherDerived.StaggerAmount         = StaggerAmount;
            otherDerived.MinRange              = MinRange;
            otherDerived.MaxRange              = MaxRange;
            otherDerived.DamageCurve           = DamageCurve;
            otherDerived.ReloadBeginClipTime   = ReloadBeginClipTime;
            otherDerived.ReloadClipTime        = ReloadClipTime;
            otherDerived.ReloadEndClipTime     = ReloadEndClipTime;
        }
    }
}
