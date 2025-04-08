using OathFramework.Pooling;
using OathFramework.Settings;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OathFramework.Effects
{
    [RequireComponent(typeof(PoolableGameObject))]
    public class MapDecal : DecalBinder, 
        IColorable, IPoolableComponent
    {
        [SerializeField] private MapDecalParams @params;
        
        [Space(10)]
        
        [SerializeField] private Material mobileMaterial;
        [SerializeField] private Renderer mobileRenderer;
        [SerializeField] private string mobileColorParam = "_BaseColor";
        
        [SerializeField, ColorUsage(true, true)] 
        private Color mobileDefaultColor;

        [Space(5)]
        
        [SerializeField] private Material desktopMaterial;
        [SerializeField] private DecalProjector desktopRenderer;
        [SerializeField] private string desktopColorParam = "_BaseColor";
        
        [SerializeField, ColorUsage(true, true)] 
        private Color desktopDefaultColor;

        private ParticleSystem.MinMaxGradient? curColor;
        private int mobileColorParamHash;
        private int desktopColorParamHash;
        private MaterialPropertyBlock propertyBlock;

        public override Func<bool> ShouldRegisterDelegate => () => true;

        public PoolableGameObject PoolableGO { get; set; }
        
        protected override void Awake()
        {
            base.Awake();
            mobileColorParamHash  = Shader.PropertyToID(mobileColorParam);
            desktopColorParamHash = Shader.PropertyToID(desktopColorParam);
            propertyBlock         = new MaterialPropertyBlock();
        }
        
        public void SetColor(ParticleSystem.MinMaxGradient? color = null)
        {
            curColor         = color;
            bool highQuality = SettingsManager.Instance.CurrentSettings.graphics.highQualityDecals;
            Material origMat = highQuality ? desktopMaterial : mobileMaterial;
            Material mat     = MapDecalsManager.GetDerivedMaterial(@params, origMat, highQuality, curColor?.color, out bool newInst);
            if(highQuality) {
                desktopRenderer.material = mat;
                if(newInst) {
                    desktopRenderer.material.SetColor(desktopColorParamHash, curColor?.color ?? desktopDefaultColor);
                }
            } else {
                mobileRenderer.sharedMaterial = mat;
                propertyBlock.SetColor(mobileColorParamHash, curColor?.color ?? mobileDefaultColor);
                mobileRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        public override void Apply(SettingsManager.GraphicsSettings settings)
        {
            base.Apply(settings);
            if(mobileRenderer.gameObject.activeSelf && mobileRenderer.gameObject.TryGetComponent(out ParticleSystem ps)) {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            SetColor(curColor);
        }

        public void Return(bool immediate = false)
        {
            if(immediate) {
                PoolableGO.Return();
                return;
            }
            Fade();
        }
        
        public void Fade()
        {
            // todo.
        }

        void IPoolableComponent.OnRetrieve() {}
        void IPoolableComponent.OnReturn(bool initialization) {}
    }
}
