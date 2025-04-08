using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Settings
{
    public class MaterialBinderEx : MaterialBinderBase
    {
        [SerializeField] private List<MaterialArray> desktopMaterials = new();
        [SerializeField] private List<MaterialArray> mobileMaterials  = new();

        private byte currentSet;
        public byte CurrentSet {
            get => currentSet;
            set {
                if(currentSet == value)
                    return;

                bool desktop = SettingsManager.Instance.CurrentSettings.graphics.highQualityMaterials;
                if((desktop && value > desktopMaterials.Count - 1) || (!desktop && value > mobileMaterials.Count - 1))
                    throw new IndexOutOfRangeException(nameof(value));
                
                currentSet = value;
                Apply(SettingsManager.Instance.CurrentSettings.graphics);
            }
        }

        public override void Apply(SettingsManager.GraphicsSettings settings)
        {
            bool desktop = settings.highQualityMaterials;
            if(desktop) {
                if(meshRenderer != null) {
                    meshRenderer.sharedMaterials = desktopMaterials[currentSet].materials;
                }
                if(skinnedMeshRenderer != null) {
                    skinnedMeshRenderer.sharedMaterials = desktopMaterials[currentSet].materials;
                }
                return;
            }
            if(meshRenderer != null) {
                meshRenderer.sharedMaterials = mobileMaterials[currentSet].materials;
            }
            if(skinnedMeshRenderer != null) {
                skinnedMeshRenderer.sharedMaterials = mobileMaterials[currentSet].materials;
            }
        }

        public override Material[] GetMaterials()
        {
            List<Material> materials = new();
            foreach(MaterialArray mat in desktopMaterials) {
                materials.AddRange(mat.materials);
            }
            foreach(MaterialArray mat in mobileMaterials) {
                materials.AddRange(mat.materials);
            }
            return materials.ToArray();
        }

        [Serializable]
        private class MaterialArray
        {
            [SerializeField] public Material[] materials;
        }
    }
}
