using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace OathFramework.Settings
{ 

    public class MeshBinder : MonoBehaviour
    {
        [SerializeField] private Mode mode = Mode.ChangeMesh;
        
        [SerializeField] 
        private MeshFilter meshFilter;
        
        [SerializeField] 
        private SkinnedMeshRenderer skinnedMeshRenderer;
        
        [SerializeField, ShowIf("@mode == MeshBinder.Mode.ChangeMesh")] 
        private Mesh desktopMesh;
        
        [SerializeField, ShowIf("@mode == MeshBinder.Mode.ChangeMesh")] 
        private Mesh mobileMesh;
        
        private LightProbeUsage originalLightProbeUsage;
        private MeshRenderer render;

        private void Awake()
        {
            if(meshFilter != null) {
                render                  = meshFilter.gameObject.GetComponent<MeshRenderer>();
                originalLightProbeUsage = render.lightProbeUsage;
            } else {
                originalLightProbeUsage = skinnedMeshRenderer.lightProbeUsage;
            }
            SettingsManager.RegisterMesh(this);
        }

        private void OnDestroy()
        {
            SettingsManager.UnregisterMesh(this);
        }

        public void Apply(SettingsManager.GraphicsSettings settings)
        {
            if(mode == Mode.HideMesh) {
                if(meshFilter != null) {
                    render.enabled = settings.highQualityMeshes;
                }
                if(skinnedMeshRenderer != null) {
                    skinnedMeshRenderer.enabled = settings.highQualityMeshes;
                }
                return;
            }
            
            if(meshFilter != null && !render.isPartOfStaticBatch) {
                meshFilter.sharedMesh  = settings.highQualityMeshes ? desktopMesh : mobileMesh;
                render.lightProbeUsage = !settings.highQualityLighting ? LightProbeUsage.Off : originalLightProbeUsage;
            }
            if(skinnedMeshRenderer != null) {
                skinnedMeshRenderer.sharedMesh      = settings.highQualityMeshes ? desktopMesh : mobileMesh;
                skinnedMeshRenderer.lightProbeUsage = !settings.highQualityLighting ? LightProbeUsage.Off : originalLightProbeUsage;
            }
        }
        
        public enum Mode
        {
            ChangeMesh, 
            HideMesh
        }
    }

}
