using UnityEngine;

namespace OathFramework.EntitySystem
{

    public class BasicFootstepController : FootstepControllerBase
    {
        [SerializeField] private Transform[] feet;
        
        [SerializeField] private FootRaycastDirection rayDir = FootRaycastDirection.DownGlobal;
        [SerializeField] private float raycastUpOffset       = 0.25f;
        [SerializeField] private float raycastLength         = 1.0f;

        public void PlayFootstepSound(int index)
        {
            if(!IsInRange())
                return;
            
            Transform foot = feet[index];
            Vector3 origin = GetOrigin(foot);
            Ray ray        = new(origin, rayDir == FootRaycastDirection.DownGlobal ? Vector3.down : -foot.transform.up);
            if(!Physics.Raycast(ray, out RaycastHit hitInfo, raycastLength, RayMask, QueryTriggerInteraction.UseGlobal))
                return;
            
            GameObject hitGO = hitInfo.collider.gameObject;
            if(!hitGO.TryGetComponent(out IFootstepSurface surface))
                return;
            
            PlaySound(surface, hitInfo.point);
        }

        private Vector3 GetOrigin(Transform foot) 
            => foot.position + (rayDir == FootRaycastDirection.DownGlobal ? Vector3.up * raycastUpOffset : foot.transform.up * raycastUpOffset);
    }

}
