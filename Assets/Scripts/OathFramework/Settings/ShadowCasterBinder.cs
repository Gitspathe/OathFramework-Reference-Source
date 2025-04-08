using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OathFramework.Settings
{

    public class ShadowCasterBinder : MonoBehaviour
    {
        [SerializeField] private ShadowCasterType type;
        [SerializeField] private Renderer render;

        private ShadowCastingMode defaultCastMode;
        
        private void Awake()
        {
            defaultCastMode = render.shadowCastingMode;
            SettingsManager.RegisterShadowCaster(this);
        }
        
        private void OnDestroy()
        {
            SettingsManager.UnregisterShadowCaster(this);
        }

        public void Apply(SettingsManager.GraphicsSettings settings)
        {
            switch(type) {
                case ShadowCasterType.Static:
                    render.shadowCastingMode = settings.shadows <= 1 ? ShadowCastingMode.Off : defaultCastMode;
                    break;
                case ShadowCasterType.Dynamic:
                    render.shadowCastingMode = defaultCastMode;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum ShadowCasterType
    {
        Static, 
        Dynamic
    }

}
