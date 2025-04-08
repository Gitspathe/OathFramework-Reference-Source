using UnityEngine;
#pragma warning disable CS0618 // Type or member is obsolete

namespace OathFramework.Effects
{
    public class MeshParticleEffectNode : ParticleEffectNode, ICopyableModelPlugComponent
    {
        private ParticleSystemShapeType defaultShapeType;

        private ParticleSystemShapeType shapeType;
        private Mesh sMesh;
        private MeshRenderer sMeshRenderer;
        private SkinnedMeshRenderer sSkinnedMeshRenderer;
        private CopyData copyData;
        
        string ICopyableModelPlugComponent.ID => "core:mesh_particle_effect_node";
        
        protected override void Awake()
        {
            base.Awake();
            defaultShapeType = PS.shape.shapeType;
        }

        public void ResetShape()
        {
            ParticleSystem.ShapeModule shapeSettings = PS.shape;
            shapeSettings.shapeType = defaultShapeType;
            shapeType               = defaultShapeType;
            sMesh                   = null;
            sMeshRenderer           = null;
            sSkinnedMeshRenderer    = null;
        }
        
        public void SetShape(Mesh mesh)
        {
            if(mesh == null) {
                ResetShape();
                return;
            }
            ParticleSystem.ShapeModule shapeSettings = PS.shape;
            shapeSettings.shapeType = ParticleSystemShapeType.Mesh;
            shapeSettings.mesh      = mesh;
            shapeType               = ParticleSystemShapeType.Mesh;
            sMesh                   = mesh;
            sMeshRenderer           = null;
            sSkinnedMeshRenderer    = null;
        }
        
        public void SetShape(MeshRenderer meshRenderer)
        {
            if(meshRenderer == null) {
                ResetShape();
                return;
            }
            ParticleSystem.ShapeModule shapeSettings = PS.shape;
            shapeSettings.shapeType    = ParticleSystemShapeType.MeshRenderer;
            shapeSettings.meshRenderer = meshRenderer;
            shapeType                  = ParticleSystemShapeType.MeshRenderer;
            sMesh                      = null;
            sMeshRenderer              = meshRenderer;
            sSkinnedMeshRenderer       = null;
        }
        
        public void SetShape(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            if(skinnedMeshRenderer == null) {
                ResetShape();
                return;
            }
            ParticleSystem.ShapeModule shapeSettings = PS.shape;
            shapeSettings.shapeType           = ParticleSystemShapeType.SkinnedMeshRenderer;
            shapeSettings.skinnedMeshRenderer = skinnedMeshRenderer;
            shapeType                         = ParticleSystemShapeType.SkinnedMeshRenderer;
            sMesh                             = null;
            sMeshRenderer                     = null;
            sSkinnedMeshRenderer              = skinnedMeshRenderer;
        }

        void ICopyableModelPlugComponent.Initialize()
        {
            copyData = new CopyData();
        }

        ICopyableModelPlugComponentData ICopyableModelPlugComponent.GetData()
        {
            return copyData.Setup(this);
        }

        void ICopyableModelPlugComponent.ApplyData(ICopyableModelPlugComponentData data)
        {
            CopyData unpacked = (CopyData)data;
            switch(copyData.Shape) {
                case ParticleSystemShapeType.Mesh: {
                    SetShape(unpacked.Mesh);
                } break;
                case ParticleSystemShapeType.MeshRenderer: {
                    SetShape(unpacked.MeshRenderer);
                } break;
                case ParticleSystemShapeType.SkinnedMeshRenderer: {
                    SetShape(unpacked.SkinnedMeshRenderer);
                } break;

                case ParticleSystemShapeType.Sphere:
                case ParticleSystemShapeType.SphereShell:
                case ParticleSystemShapeType.Hemisphere:
                case ParticleSystemShapeType.HemisphereShell:
                case ParticleSystemShapeType.Cone:
                case ParticleSystemShapeType.Box:
                case ParticleSystemShapeType.ConeShell:
                case ParticleSystemShapeType.ConeVolume:
                case ParticleSystemShapeType.ConeVolumeShell:
                case ParticleSystemShapeType.Circle:
                case ParticleSystemShapeType.CircleEdge:
                case ParticleSystemShapeType.SingleSidedEdge:
                case ParticleSystemShapeType.BoxShell:
                case ParticleSystemShapeType.BoxEdge:
                case ParticleSystemShapeType.Donut:
                case ParticleSystemShapeType.Rectangle:
                case ParticleSystemShapeType.Sprite:
                case ParticleSystemShapeType.SpriteRenderer:
                default: {
                    ResetShape();
                } break;
            }
        }

        public class CopyData : ICopyableModelPlugComponentData
        {
            public ParticleSystemShapeType Shape           { get; private set; }
            public Mesh Mesh                               { get; private set; }
            public MeshRenderer MeshRenderer               { get; private set; }
            public SkinnedMeshRenderer SkinnedMeshRenderer { get; private set; }
            
            public CopyData Setup(MeshParticleEffectNode node)
            {
                Shape               = node.shapeType;
                Mesh                = null;
                MeshRenderer        = null;
                SkinnedMeshRenderer = null;
                switch(node.shapeType) {
                    case ParticleSystemShapeType.Mesh: {
                        Mesh = node.sMesh;
                    } break;
                    case ParticleSystemShapeType.MeshRenderer: {
                        MeshRenderer = node.sMeshRenderer;
                    } break;
                    case ParticleSystemShapeType.SkinnedMeshRenderer: {
                        SkinnedMeshRenderer = node.sSkinnedMeshRenderer;
                    } break;
                    
                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.SphereShell:
                    case ParticleSystemShapeType.Hemisphere:
                    case ParticleSystemShapeType.HemisphereShell:
                    case ParticleSystemShapeType.Cone:
                    case ParticleSystemShapeType.Box:
                    case ParticleSystemShapeType.ConeShell:
                    case ParticleSystemShapeType.ConeVolume:
                    case ParticleSystemShapeType.ConeVolumeShell:
                    case ParticleSystemShapeType.Circle:
                    case ParticleSystemShapeType.CircleEdge:
                    case ParticleSystemShapeType.SingleSidedEdge:
                    case ParticleSystemShapeType.BoxShell:
                    case ParticleSystemShapeType.BoxEdge:
                    case ParticleSystemShapeType.Donut:
                    case ParticleSystemShapeType.Rectangle:
                    case ParticleSystemShapeType.Sprite:
                    case ParticleSystemShapeType.SpriteRenderer:
                    default: break;
                }
                return this;
            }
        }
    }
}
