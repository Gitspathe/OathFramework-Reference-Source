using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Settings
{ 
    
    public class MaterialBinder : MaterialBinderBase, IMaterialPreloaderDataProvider
    {
        [SerializeField] private List<Material> desktopMaterials;
        [SerializeField] private List<Material> mobileMaterials;

        public override void Apply(SettingsManager.GraphicsSettings settings)
        {
            if(meshRenderer != null) {
                meshRenderer.SetSharedMaterials(settings.highQualityMaterials ? desktopMaterials : mobileMaterials);
            }
            if(skinnedMeshRenderer != null) {
                skinnedMeshRenderer.SetSharedMaterials(settings.highQualityMaterials ? desktopMaterials : mobileMaterials);
            }
        }

        public override Material[] GetMaterials()
        {
            List<Material> mats = new();
            mats.AddRange(desktopMaterials);
            mats.AddRange(mobileMaterials);
            return mats.ToArray();
        }
    }

}
