using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{ 

    public class HitSurface : MonoBehaviour, IHitSurface
    {
        [SerializeField] private float blockingPower;
        [SerializeField] private HitSurfaceMaterial material;

        [field: SerializeField] public bool IsStatic { get; private set; }
        
        public float BlockingPower {
            get => blockingPower;
            set => blockingPower = value;
        }
        
        public HitSurfaceMaterial Material {
            get => material;
            set => material = value;
        }

        public HitSurfaceParams GetHitSurfaceParams(Vector3 position) => new(blockingPower, material);
    }

    public interface IHitSurface
    {
        HitSurfaceParams GetHitSurfaceParams(Vector3 position);
        bool IsStatic { get; }
    }

    public struct HitSurfaceParams
    {
        public float BlockingPower         { get; }
        public HitSurfaceMaterial Material { get; }

        public HitSurfaceParams(float blockingPower, HitSurfaceMaterial material)
        {
            BlockingPower = blockingPower;
            Material = material;
        }
    }

}
