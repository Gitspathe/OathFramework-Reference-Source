using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.UI.Info;
using TMPro;
using UnityEngine;

namespace OathFramework.UI.Builds
{ 
    public class StatNode : MonoBehaviour
    {
        public string statType;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI valText;
        public Color normalColor;
        public Color incrementColor;
        public Color decrementColor;

        public void Tick(Stats oldStats, Stats curStats)
        {
            float oldVal, val;
            switch(statType) {
                default: {
                    if(!StatParamManager.TryGet(statType, out StatParam param)
                       || !param.GetUIInfo(oldStats, curStats, out string valStr, out StatParam.UIDiff diff)
                       || UIInfoManager.GetStatParamInfo(param.LookupKey) == null) {
                        titleText.text = "NULL";
                        valText.text   = "";
                        return;
                    }

                    UIStatParamInfo info = UIInfoManager.GetStatParamInfo(param.LookupKey);
                    titleText.text       = info.Title.GetLocalizedString();
                    valText.text         = valStr;
                    switch(diff) {
                        case StatParam.UIDiff.Increment: {
                            titleText.color = incrementColor;
                            valText.color   = incrementColor;
                        } break;
                        case StatParam.UIDiff.Decrement: {
                            titleText.color = decrementColor;
                            valText.color   = decrementColor;
                        } break;

                        case StatParam.UIDiff.None:
                        default: {
                            titleText.color = normalColor;
                            valText.color   = normalColor;
                        } break;
                    }
                    return;
                }
                
                case "core:hp": {
                    UIStatParamInfo info = UIInfoManager.GetStatParamInfo(statType);
                    titleText.text       = info.Title.GetLocalizedString();
                    oldVal               = oldStats?.health ?? 0.0f;
                    val                  = curStats.health;
                    valText.text         = val.ToString("0");
                } break;
                case "core:max_hp": {
                    UIStatParamInfo info = UIInfoManager.GetStatParamInfo(statType);
                    titleText.text       = info.Title.GetLocalizedString();
                    oldVal               = oldStats?.maxHealth ?? 0.0f;
                    val                  = curStats.maxHealth;
                    valText.text         = val.ToString("0");
                } break;
                case "core:stamina": {
                    UIStatParamInfo info = UIInfoManager.GetStatParamInfo(statType);
                    titleText.text       = info.Title.GetLocalizedString();
                    oldVal               = oldStats?.stamina ?? 0.0f;
                    val                  = curStats.stamina;
                    valText.text         = val.ToString("0");
                } break;
                case "core:max_stamina": {
                    UIStatParamInfo info = UIInfoManager.GetStatParamInfo(statType);
                    titleText.text       = info.Title.GetLocalizedString();
                    oldVal               = oldStats?.maxStamina ?? 0.0f;
                    val                  = curStats.maxStamina;
                    valText.text         = val.ToString("0");
                } break;
                case "core:movement_speed": {
                    UIStatParamInfo info = UIInfoManager.GetStatParamInfo(statType);
                    titleText.text       = info.Title.GetLocalizedString();
                    oldVal               = oldStats?.speed ?? 0.0f;
                    val                  = curStats.speed;
                    valText.text         = val.ToString("0.00") + "m/s";
                } break;
            }

            // Update color.
            titleText.color = normalColor;
            valText.color   = normalColor;
            if(oldStats != null && oldVal > val) {
                titleText.color = decrementColor;
                valText.color   = decrementColor;
            } else if(oldStats != null && oldVal < val) {
                titleText.color = incrementColor;
                valText.color   = incrementColor;
            }
        }
    }
}
