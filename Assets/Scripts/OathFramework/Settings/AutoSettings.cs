using OathFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace OathFramework.Settings
{
    public static class AutoSettings
    {
        private const float ScorePerRam  = 8.0f;
        private const float ScorePerVRam = 20.0f;

        public static void Run()
        {
            bool except    = false;
            float ramScore = 0.0f, vramScore = 0.0f, score = 0.0f;
            try {
                bool isIGPU    = false;
                string gfx     = SystemInfo.graphicsDeviceName.ToLower();
                string gfxName = gfx.Replace("(r)", "").Replace("\u00ae", "").Replace("\u2122", "");
                string[] iGPUs = {
                    "intel hd", "intel uhd", "hd graphics", " iris", "iris ",
                    "radeon vega", "radeon r7", "radeon r5", "radeon r4", "radeon r3", "radeon rx vega", 
                    "radeon integrated", "ryzen vega"
                };
                foreach(string i in iGPUs) {
                    if(!gfxName.Contains(i))
                        continue;

                    isIGPU = true;
                    break;
                }

                float ramGB  = SystemInfo.systemMemorySize   / 1000.0f;
                float vramGB = SystemInfo.graphicsMemorySize / 1000.0f;
                if(isIGPU) {
                    score = ramGB >= 8.01f ? ramGB * 5.0f : 0.0f;
                } else {
                    ramScore  = Mathf.Clamp(ramGB  * ScorePerRam,  0.0f, 4.0f * ScorePerRam);
                    vramScore = Mathf.Clamp(vramGB * ScorePerVRam, 0.0f, 6.0f * ScorePerVRam);
                    score     = ramScore + vramScore;
                }
            } catch(Exception) {
                except = true;
            }
            if(except || Math.Abs(ramScore) < 0.01f || Math.Abs(vramScore) < 0.01f) {
                ModalUIScript.ShowGeneric(
                    title: SettingsManager.Instance.AutoSettingsTitleStr, 
                    text: SettingsManager.Instance.AutoSettingsFailedStr
                );
                SettingsManager.Instance.SetPreset((int)DesktopGraphicsPresets.Medium);
                SettingsManager.Instance.ApplyGfx();
                return;
            }
            
            if(score >= 100.0f) {
                SettingsManager.Instance.SetPreset((int)DesktopGraphicsPresets.Ultra);
                DisplayModal("Ultra");
            }
            else if(score >= 75.0f) {
                SettingsManager.Instance.SetPreset((int)DesktopGraphicsPresets.High);
                DisplayModal("High");
            }
            else if(score >= 60.0f) {
                SettingsManager.Instance.SetPreset((int)DesktopGraphicsPresets.Medium);
                DisplayModal("Medium");
            }
            else if(score >= 40.0f) {
                SettingsManager.Instance.SetPreset((int)DesktopGraphicsPresets.Low);
                DisplayModal("Low");
            } else {
                SettingsManager.Instance.SetPreset((int)DesktopGraphicsPresets.VeryLow);
                DisplayModal("Very Low");
            }
            SettingsManager.Instance.ApplyGfx();
        }

        private static void DisplayModal(string preset)
        {
            SettingsManager.Instance.AutoSettingsSuccessStr.Arguments = new List<object> { new Dictionary<string, string> { {"preset", preset} } };
            ModalUIScript.ShowGeneric(
                title: SettingsManager.Instance.AutoSettingsTitleStr, 
                text: SettingsManager.Instance.AutoSettingsSuccessStr
            );
        }
    }
}
