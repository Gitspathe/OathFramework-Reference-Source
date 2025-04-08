using System;
using UnityEngine;

namespace OathFramework.Settings
{
    public class DecalBinder : DecalBinderBase
    {
        [SerializeField] protected GameObject lowDecal;
        [SerializeField] protected GameObject highDecal;

        public override Func<bool> ShouldRegisterDelegate => () => false;

        public override void Apply(SettingsManager.GraphicsSettings settings)
        {
            if(lowDecal != null) {
                lowDecal.SetActive(false);
            }
            if(highDecal != null) {
                highDecal.SetActive(false);
            }

            if(settings.highQualityDecals && highDecal != null) {
                highDecal.SetActive(true);
            } else if(!settings.highQualityDecals && lowDecal != null) {
                lowDecal.SetActive(true);
            }
        }
    }
}
