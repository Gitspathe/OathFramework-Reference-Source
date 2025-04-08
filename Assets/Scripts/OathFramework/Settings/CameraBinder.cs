using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OathFramework.Settings
{

    public class CameraBinder : MonoBehaviour
    {
        private UniversalAdditionalCameraData urpData;
        private Camera cam;
        
        private void Awake()
        {
            urpData = GetComponent<UniversalAdditionalCameraData>();
            cam     = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            SettingsManager.RegisterCamera(this);
        }

        private void OnDisable()
        {
            SettingsManager.UnregisterCamera(this);
        }

        public void Apply(SettingsManager.GraphicsSettings settings)
        {
            urpData.renderPostProcessing = settings.postProcessing;
            urpData.dithering            = settings.postProcessing;
            urpData.antialiasingQuality  = AntialiasingQuality.High;
            switch(settings.extraAntialiasing) {
                case 1:
                    urpData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    break;
                case 2:
                    urpData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                    break;
                case 3:
                    urpData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    break;

                case 0:
                default:
                    urpData.antialiasing = AntialiasingMode.None;
                    break;
            }
        }
    }

}
