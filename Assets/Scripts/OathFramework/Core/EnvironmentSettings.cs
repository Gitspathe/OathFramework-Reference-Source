using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace OathFramework.Core
{
    [CreateAssetMenu(fileName = "Environment Settings", menuName = "ScriptableObjects/Environment Settings", order = 1)]
    public class EnvironmentSettings : ScriptableObject
    {
        [field: SerializeField] public AmbientMode AmbientMode { get; private set; }
        [field: SerializeField] public float AmbientIntensity  { get; private set; }
        [field: SerializeField] public Color AmbientLight      { get; private set; }
        [field: SerializeField] public Color AmbientSky        { get; private set; }
        [field: SerializeField] public Color AmbientEquator    { get; private set; }
        [field: SerializeField] public Color AmbientGround     { get; private set; }
        
        [field: Space(10)]

        [field: SerializeField] public bool Fog                { get; private set; }
        [field: SerializeField] public FogMode FogMode         { get; private set; }
        [field: SerializeField] public Color FogColor          { get; private set; }
        [field: SerializeField] public float FogDensity        { get; private set; }
        [field: SerializeField] public float FogStartDistance  { get; private set; }
        [field: SerializeField] public float FogEndDistance    { get; private set; }

        public void Apply()
        {
            RenderSettings.ambientMode         = AmbientMode;
            RenderSettings.ambientLight        = AmbientLight;
            RenderSettings.ambientSkyColor     = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor  = AmbientGround;
            RenderSettings.ambientIntensity    = AmbientIntensity;
            RenderSettings.fog                 = Fog;
            RenderSettings.fogMode             = FogMode;
            RenderSettings.fogColor            = FogColor;
            RenderSettings.fogDensity          = FogDensity;
            RenderSettings.fogStartDistance    = FogStartDistance;
            RenderSettings.fogEndDistance      = FogEndDistance;
        }

        [Button("Copy Existing")]
        private void CopyExisting()
        {
            AmbientMode      = RenderSettings.ambientMode;
            AmbientLight     = RenderSettings.ambientLight;
            AmbientSky       = RenderSettings.ambientSkyColor;
            AmbientEquator   = RenderSettings.ambientEquatorColor;
            AmbientGround    = RenderSettings.ambientGroundColor;
            AmbientIntensity = RenderSettings.ambientIntensity;
            Fog              = RenderSettings.fog;
            FogMode          = RenderSettings.fogMode;
            FogColor         = RenderSettings.fogColor;
            FogDensity       = RenderSettings.fogDensity;
            FogStartDistance = RenderSettings.fogStartDistance;
            FogEndDistance   = RenderSettings.fogEndDistance;
        }
    }
}
