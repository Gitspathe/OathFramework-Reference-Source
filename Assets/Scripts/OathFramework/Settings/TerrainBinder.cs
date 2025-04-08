using UnityEngine;
using UnityEngine.Rendering;

namespace OathFramework.Settings
{

    [RequireComponent(typeof(Terrain))] 
    public class TerrainBinder : MonoBehaviour
    {
        private Terrain terrain;
        [SerializeField] private int[] detailShadowCast;

        private void Awake()
        {
            terrain = GetComponent<Terrain>();
            SettingsManager.RegisterTerrain(this);
        }

        private void OnDestroy()
        {
            SettingsManager.UnregisterTerrain(this);
        }

        public void Apply(SettingsManager.GraphicsSettings settings)
        {
#if !UNITY_IOS && !UNITY_ANDROID
            switch(settings.vegetation) {
                case 1:
                    terrain.detailObjectDensity = 0.5f;
                    break;
                case 2:
                    terrain.detailObjectDensity = 0.75f;
                    break;
                case 3:
                    terrain.detailObjectDensity = 1.0f;
                    break;

                case 0:
                default:
                    terrain.detailObjectDensity = 0.0f;
                    break;
            }
#else
            switch(settings.vegetation) {
                case 1:
                    terrain.detailObjectDensity = 0.0f;
                    break;
                case 2:
                    terrain.detailObjectDensity = 0.0f;
                    break;
                case 3:
                    terrain.detailObjectDensity = 0.5f;
                    break;

                case 0:
                default:
                    terrain.detailObjectDensity = 0.0f;
                    break;
            }
#endif
            int s = settings.shadows;
            for(int i = 0; i < detailShadowCast.Length; i++) {
                int minShadowCast    = detailShadowCast[i];
                GameObject prototype = terrain.terrainData.detailPrototypes[i].prototype;
                if(prototype == null || !prototype.TryGetComponent(out Renderer render))
                    continue;

                render.shadowCastingMode = s < 0 || s < minShadowCast ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }
            terrain.terrainData.RefreshPrototypes();
        }
    }

}
