using UnityEngine;

namespace OathFramework.EntitySystem
{ 

    public class FootstepSurface : MonoBehaviour, IFootstepSurface
    {
        [SerializeField] private FootstepMaterial material;

        public FootstepMaterial GetFootstepMaterial(Vector3 position) => material;
    }

    public interface IFootstepSurface
    {
        FootstepMaterial GetFootstepMaterial(Vector3 position);
    }

}
