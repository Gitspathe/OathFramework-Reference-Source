using OathFramework.EquipmentSystem;
using System;
using UnityEngine;
using UnityEngine.Localization;

namespace OathFramework.UI.Info
{
    public class EquipmentInfoHelper : MonoBehaviour
    {
        [field: SerializeField] public LocalizedString DamageTitle       { get; private set; }
        
        [field: SerializeField] public LocalizedString DPSTitle          { get; private set; }
        [field: SerializeField] public LocalizedString DPSMetric         { get; private set; }

        [field: SerializeField] public LocalizedString RateOfFireTitle   { get; private set; }
        [field: SerializeField] public LocalizedString RateOfFireMetric  { get; private set; }
        
        [field: SerializeField] public LocalizedString ReloadSpeedTitle  { get; private set; }
        [field: SerializeField] public LocalizedString ReloadSpeedMetric { get; private set; }
        
        [field: SerializeField] public LocalizedString AccuracyTitle     { get; private set; }
        [field: SerializeField] public LocalizedString HandlingTitle     { get; private set; }
        [field: SerializeField] public LocalizedString PenetrationTitle  { get; private set; }
        
        public static EquipmentInfoHelper Instance { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate singleton for '{nameof(EquipmentInfoHelper)}'.");
                Destroy(Instance);
            }

            Instance = this;
        }

        public bool TryGetInfo(UIEquippableParams param, Equippable template, out string title, out string metric)
        {
            title = "";
            metric = "";
            switch(param) {
                case UIEquippableParams.None:
                    break;
                case UIEquippableParams.Damage: {
                    IStatsDamage iDamage         = template.GetStatsInterface<IStatsDamage>();
                    IStatsProjectile iProjectile = template.GetStatsInterface<IStatsProjectile>();
                    if(iDamage == null)
                        return false;
                    
                    title     = DamageTitle.GetLocalizedString();
                    metric    = $"{iDamage.Damage * iProjectile?.Pellets ?? 1:0}";
                } return true;
                case UIEquippableParams.DamagePerSecond: {
                    IStatsDamage iDamage         = template.GetStatsInterface<IStatsDamage>();
                    IStatsProjectile iProjectile = template.GetStatsInterface<IStatsProjectile>();
                    if(iDamage == null)
                        return false;
                    
                    float val = iDamage.Damage * (iProjectile?.Pellets ?? 1) * (iDamage.FireRate / 60.0f);
                    title     = DPSTitle.GetLocalizedString();
                    metric    = $"{val:0} {DPSMetric.GetLocalizedString()}";
                } return true;
                case UIEquippableParams.RateOfFire: {
                    IStatsDamage iDamage = template.GetStatsInterface<IStatsDamage>();
                    if(iDamage == null)
                        return false;
                    
                    title     = RateOfFireTitle.GetLocalizedString();
                    metric    = $"{iDamage.FireRate:0} {RateOfFireMetric.GetLocalizedString()}";
                } return true;
                case UIEquippableParams.ReloadSpeed: {
                    IStatsReload iReload = template.GetStatsInterface<IStatsReload>();
                    if(iReload == null)
                        return false;
                    
                    float val = iReload.GetFullReloadTime(iReload.AmmoCapacity);
                    title     = ReloadSpeedTitle.GetLocalizedString();
                    metric    = $"{val:0.0} {ReloadSpeedMetric.GetLocalizedString()}";
                } return true;
                case UIEquippableParams.Accuracy: {
                    IStatsAccuracy iAccuracy = template.GetStatsInterface<IStatsAccuracy>();
                    if(iAccuracy == null)
                        return false;
                    
                    float val = iAccuracy.Accuracy;
                    title     = AccuracyTitle.GetLocalizedString();
                    metric    = $"{val * 100.0f:0}";
                } return true;
                case UIEquippableParams.Handling: {
                    IStatsAccuracy iAccuracy = template.GetStatsInterface<IStatsAccuracy>();
                    if(iAccuracy == null)
                        return false;
                    
                    float val = 1.0f;
                    val      -= ((iAccuracy.Recoil * iAccuracy.MaxRecoil * 4.0f) / iAccuracy.RecoilLoss) + (iAccuracy.MoveAccuracyPenalty * 0.5f);
                    title     =  HandlingTitle.GetLocalizedString();
                    metric    =  $"{val * 100.0f:0}";
                } return true;
                case UIEquippableParams.Penetration: {
                    IStatsPenetration iPenetration = template.GetStatsInterface<IStatsPenetration>();
                    if(iPenetration == null)
                        return false;
                    
                    title  = PenetrationTitle.GetLocalizedString();
                    metric = $"{iPenetration.Penetration:0}";
                } return true;
                case UIEquippableParams.Destruction: {
                    
                } return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(param), param, null);
            }
            return false;
        }
    }
}
