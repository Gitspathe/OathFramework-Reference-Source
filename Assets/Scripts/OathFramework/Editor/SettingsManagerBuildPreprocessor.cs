using OathFramework.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OathFramework.Editor
{
    
    public class SettingsManagerBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -999;

        public void OnPreprocessBuild(BuildReport report)
        {
            SettingsManager inst = GameObject.Find("_SETTINGS").GetComponent<SettingsManager>();
#if UNITY_ANDROID || UNITY_IOS
            Debug.Log("Applying settings preprocessor (mobile).");
            inst.SsaoRenderFeature.SetActive(false);
            inst.DecalRenderFeature.SetActive(false);
            inst.TransUIRenderFeature.SetActive(false);
            foreach(UniversalRenderPipelineAsset urpAsset in inst.PipelineAssets) {
                urpAsset.supportsHDR = false;
                urpAsset.msaaSampleCount = 0;
            }
            inst.RenderDataAsset.postProcessData = null;
            
#else
            Debug.Log("Applying settings preprocessor (desktop).");
            inst.SsaoRenderFeature.SetActive(true);
            inst.DecalRenderFeature.SetActive(true);
            inst.TransUIRenderFeature.SetActive(true);
            foreach(UniversalRenderPipelineAsset urpAsset in inst.PipelineAssets) {
                urpAsset.supportsHDR = true;
                urpAsset.msaaSampleCount = 8;
            }
            inst.RenderDataAsset.postProcessData = inst.PostProcessData;
#endif
        }
    }

}
