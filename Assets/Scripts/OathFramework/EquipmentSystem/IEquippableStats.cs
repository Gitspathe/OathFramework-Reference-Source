using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Projectiles;

namespace OathFramework.EquipmentSystem
{
    public interface IEquippableStats
    {
        float PutAwayTime   { get; }
        float TakeOutTime   { get; }
        ushort AmmoCapacity { get; }
        
        float GetTimeBetweenUses(float useRateMult);
    }

    public interface IStatsDamage : IEquippableStats
    {
        float Damage                    { get; }
        float DamageRand                { get; }
        float FireRate                  { get; }
        StaggerStrength StaggerStrength { get; }
        ushort StaggerAmount            { get; }
    }

    public interface IStatsPenetration : IEquippableStats
    {
        float Penetration { get; }
    }
    
    public interface IStatsReload : IEquippableStats
    {
        float ReloadBeginTime     { get; }
        float ReloadTime          { get; }
        float ReloadEndTime       { get; }
        bool ReloadIndividually   { get; }
        bool ReloadInterrupt      { get; }
        bool ReloadImmediately    { get; }
        float ReloadUseDelay      { get; }
        float AntiReloadInterrupt { get; }
        
        float ReloadBeginClipTime { get; }
        float ReloadClipTime      { get; }
        float ReloadEndClipTime   { get; }

        float GetFullReloadTime(int ammo = 1, float mult = 1.0f);
        float GetFullReloadClipTime(int ammo = 1, float mult = 1.0f);
    }

    public interface IStatsAccuracy : IEquippableStats
    {
        float Accuracy                { get; }
        float MoveAccuracyPenalty     { get; }
        float MoveAccuracyPenaltyTime { get; }
        float Recoil                  { get; }
        float RecoilTime              { get; }
        float MaxRecoil               { get; }
        float RecoilLoss              { get; }
        float PelletSpread            { get; }
    }

    public interface IStatsProjectile : IEquippableStats
    {
        ProjectileTemplate Projectile { get; }
        int Pellets                   { get; }
    }

    public interface IStatsRadius : IEquippableStats
    {
        float Radius { get; }
    }
}
