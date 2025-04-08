using OathFramework.EquipmentSystem;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.UI.Info
{
    public static class EquipmentInfoUtil
    {
        public static ParamDisplayInfo GetDisplayInfo(UIEquippableParams param, Equippable template)
        {
            EquipmentInfoHelper.Instance.TryGetInfo(param, template, out string title, out string metric);
            switch(param) {
                case UIEquippableParams.Damage: {
                    IStatsDamage iDamage         = template.GetStatsInterface<IStatsDamage>();
                    IStatsProjectile iProjectile = template.GetStatsInterface<IStatsProjectile>();
                    if(iDamage == null) {
                        Debug.LogError($"No {nameof(IStatsDamage)} found on template {template.name}");
                        return new ParamDisplayInfo();
                    }
                    
                    return new ParamDisplayInfo(
                        title,
                        metric,
                        iDamage.Damage * (iProjectile?.Pellets ?? 1.0f),
                        0.0f,
                        1000.0f
                    );
                }
                case UIEquippableParams.DamagePerSecond: {
                    IStatsDamage iDamage         = template.GetStatsInterface<IStatsDamage>();
                    IStatsProjectile iProjectile = template.GetStatsInterface<IStatsProjectile>();
                    if(iDamage == null) {
                        Debug.LogError($"No {nameof(IStatsDamage)} found on template {template.name}");
                        return new ParamDisplayInfo();
                    }
                    
                    float val = iDamage.Damage * (iProjectile?.Pellets ?? 1.0f) * (iDamage.FireRate / 60.0f);
                    return new ParamDisplayInfo(
                        title,
                        metric,
                        val,
                        0.0f,
                        750.0f
                    );
                }
                case UIEquippableParams.RateOfFire: {
                    IStatsDamage iDamage = template.GetStatsInterface<IStatsDamage>();
                    if(iDamage == null) {
                        Debug.LogError($"No {nameof(IStatsDamage)} found on template {template.name}");
                        return new ParamDisplayInfo();
                    }
                    
                    return new ParamDisplayInfo(
                        title,
                        metric,
                        iDamage.FireRate,
                        15.0f,
                        700.0f
                    );
                }
                case UIEquippableParams.ReloadSpeed: {
                    IStatsReload iReload = template.GetStatsInterface<IStatsReload>();
                    if(iReload == null) {
                        Debug.LogError($"No {nameof(IStatsReload)} found on template {template.name}");
                        return new ParamDisplayInfo();
                    }
                    
                    float reloadTime = iReload.GetFullReloadTime(iReload.AmmoCapacity);
                    float val        = -reloadTime + 6.0f;
                    return new ParamDisplayInfo(
                        title,
                        metric,
                        val,
                        1.0f,
                        5.0f
                    );
                }
                case UIEquippableParams.Accuracy: {
                    IStatsAccuracy iAccuracy = template.GetStatsInterface<IStatsAccuracy>();
                    if(iAccuracy == null) {
                        Debug.LogError($"No {nameof(IStatsAccuracy)} found on template {template.name}");
                        return new ParamDisplayInfo();
                    }
                    
                    float val = iAccuracy.Accuracy;
                    return new ParamDisplayInfo(
                        title,
                        metric,
                        val,
                        0.25f,
                        1.0f
                    );
                }
                case UIEquippableParams.Handling: {
                    IStatsAccuracy iAccuracy = template.GetStatsInterface<IStatsAccuracy>();
                    if(iAccuracy == null) {
                        Debug.LogError($"No {nameof(IStatsAccuracy)} found on template {template.name}");
                        return new ParamDisplayInfo();
                    }

                    float val = 1.0f;
                    val -= ((iAccuracy.Recoil * iAccuracy.MaxRecoil * 4.0f) / iAccuracy.RecoilLoss) + (iAccuracy.MoveAccuracyPenalty * 0.5f);
                    return new ParamDisplayInfo(
                        title,
                        metric,
                        val,
                        0.5f,
                        1.0f
                    );
                }
                case UIEquippableParams.Penetration: {
                    IStatsPenetration iPenetration = template.GetStatsInterface<IStatsPenetration>();
                    if(iPenetration == null) {
                        Debug.LogError($"No {nameof(IStatsPenetration)} found on template {template.name}");
                        return new ParamDisplayInfo();
                    }
                    
                    return new ParamDisplayInfo(
                        title,
                        metric,
                        iPenetration.Penetration,
                        0.0f,
                        200.0f
                    );
                }
                case UIEquippableParams.Destruction: {
                    return default;
                }
                
                case UIEquippableParams.None:
                default:
                    return default;
            }
        }
        
        public struct ParamDisplayInfo
        {
            public string Title;
            public string DisplayValue;
            public float Value;
            public float Min;
            public float Max;

            public float NormalizedValue {
                get {
                    if(Mathf.Abs(Value - Min) < 0.0001f 
                       || Mathf.Abs(Max - Min) < 0.0001f 
                       || Min > Max 
                       || Value < Min)
                        return 0.0f;
                    if(Value > Max)
                        return 1.0f;

                    return (Value - Min) / (Max - Min);
                }
            }

            public ParamDisplayInfo(string title, string displayValue, float value, float min, float max)
            {
                Title        = title;
                DisplayValue = displayValue;
                Value        = value;
                Min          = min;
                Max          = max;
            }
        }
    }
}
