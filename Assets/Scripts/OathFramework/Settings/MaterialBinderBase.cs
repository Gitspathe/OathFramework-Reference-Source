using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace OathFramework.Settings
{
    public abstract class MaterialBinderBase : MonoBehaviour, IMaterialPreloaderDataProvider
    {
        [SerializeField] protected MeshRenderer meshRenderer;
        [SerializeField] protected SkinnedMeshRenderer skinnedMeshRenderer;
        
        private void Awake()
        {
            SettingsManager.RegisterMaterial(this);
        }

        private void OnDestroy()
        {
            SettingsManager.UnregisterMaterial(this);
        }

        public abstract void Apply(SettingsManager.GraphicsSettings settings);
        public abstract Material[] GetMaterials();
    }
}
