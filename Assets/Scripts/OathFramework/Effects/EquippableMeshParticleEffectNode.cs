using OathFramework.EquipmentSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OathFramework.Effects
{
    public class EquippableMeshParticleEffectNode : ParticleEffectNode
    {
        [SerializeField] private bool adjustEmissionBasedOnSize;

        [SerializeField, ShowIf("@adjustEmissionBasedOnSize")]
        private float defaultSize = 1.0f;

        private float originalEmissionOverTimeMult;
        private float originalEmissionOverDistanceMult;

        protected override void Awake()
        {
            base.Awake();
            if(PS == null)
                return;
            
            ParticleSystem.EmissionModule emission = PS.emission;
            originalEmissionOverTimeMult           = emission.rateOverTimeMultiplier;
            originalEmissionOverDistanceMult       = emission.rateOverDistanceMultiplier;
        }

        protected override void OnAddedToSockets(byte spot, ModelSocketHandler sockets)
        {
            EquippableModel model            = sockets.GetComponent<EquippableModel>();
            ParticleSystem.ShapeModule shape = PS.shape;
            shape.shapeType                  = ParticleSystemShapeType.MeshRenderer;
            if(model.Meshes == null || model.Meshes.Count == 0) {
                Debug.LogError("Model does not contain the correct amount of Meshes.");
                return;
            }

            MeshRenderer mesh = model.Meshes[0];
            if(mesh == null)
                return;

            shape.meshRenderer = mesh;
            if(adjustEmissionBasedOnSize && defaultSize > 0.0f) {
                float size = (mesh.bounds.size.x + mesh.bounds.size.y + mesh.bounds.size.z) / 3.0f;
                if(size == 0.0f) {
                    PS.Clear();
                    PS.Play();
                    return;
                }
                ParticleSystem.EmissionModule emission = PS.emission;
                emission.rateOverTimeMultiplier        = originalEmissionOverTimeMult * (size / defaultSize);
                emission.rateOverDistanceMultiplier    = originalEmissionOverDistanceMult * (size / defaultSize);
            }
            PS.Clear();
            PS.Play();
        }
    }
}
