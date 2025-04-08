using System;
using System.Collections.Generic;
using UnityEngine;
using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.EntitySystem.Players;

namespace OathFramework.EntitySystem 
{

    public abstract class FootstepControllerBase : LoopComponent
    {
        public override int UpdateOrder => GameUpdateOrder.Finalize;

        [SerializeField] protected FootstepParams @params;
        [SerializeField] protected float ActiveRange = 30.0f;
        
        protected IFootstepSource FootstepSource;
        
        protected static LayerMask RayMask => FootstepManager.Instance.RaycastLayerMask;
        
        public void Initialize(IFootstepSource source)
        {
            FootstepSource = source;
        }

        protected bool IsInRange()
        {
            if(FootstepSource == null)
                return false;
            
            if(ReferenceEquals(PlayerController.Active, null)) {
                Camera main = Camera.main;
                if(main == null)
                    return false;
                
                // Slow fallback.
                return Vector3.Distance(transform.position, main.transform.position) <= ActiveRange;
            }
            EntityTargeting targeting = FootstepSource.Entity.Targeting;
            return targeting.GetDistance(PlayerController.Active.Entity) < ActiveRange;
        }
        
        protected void PlaySound(IFootstepSurface surface, Vector3 position) 
            => FootstepManager.CreateFootstep(@params, surface.GetFootstepMaterial(position), position, FootstepSource.Spatialized);
    }

    public class FootstepController : FootstepControllerBase, ILoopLateUpdate
    {
        [SerializeField] private List<Foot> feet = new();

        [Space(10)]

        [SerializeField] private FootRaycastDirection rayDir = FootRaycastDirection.DownGlobal;
        [SerializeField] private float raycastDownLength     = 0.05f;
        [SerializeField] private float raycastUpLength       = 0.05f;
        [SerializeField] private float repeatDelay           = 0.1f;

        [Space(10)]

        [SerializeField] private bool requireVelocity = true;
        [SerializeField] private float minVelocity    = 0.035f;

        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(!IsInRange())
                return;

            foreach(Foot foot in feet) {
                ProcessFoot(foot);
            }
        }

        private void ProcessFoot(Foot foot)
        {
            foot.curDelay       -= Time.deltaTime;
            Vector3 footPosition = foot.transform.position;
            foot.curVelocity     = (footPosition - foot.lastPos).magnitude / Time.deltaTime;
            float heightDelta    = footPosition.y - foot.lastPos.y;
            foot.lastPos         = footPosition;
            if(requireVelocity && foot.curVelocity <= minVelocity)
                return;

            switch(foot.state) {
                case FootState.Up: {
                    foot.upDist = 0.0f;
                    if(heightDelta > 0.01f)
                        break;
                    
                    Ray ray = new(foot.transform.position, rayDir == FootRaycastDirection.DownGlobal ? Vector3.down : -foot.transform.up);
                    if(!Physics.Raycast(ray, out RaycastHit hitInfo, raycastDownLength, RayMask, QueryTriggerInteraction.UseGlobal))
                        break;
                    if(!hitInfo.collider.TryGetComponent(out IFootstepSurface surface))
                        break;
                    
                    foot.curDelay = repeatDelay;
                    foot.state    = FootState.Down;
                    PlaySound(surface, hitInfo.point);
                } break;
                case FootState.Down: {
                    foot.upDist = Mathf.Clamp(foot.upDist + heightDelta, 0.0f, 100.0f);
                    if(foot.upDist >= raycastUpLength) {
                        foot.state = FootState.Up;
                    }
                } break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        [Serializable]
        public class Foot
        {
            public Transform transform;
            [NonSerialized] public FootState state;
            [NonSerialized] public float curDelay;
            [NonSerialized] public float curVelocity;
            [NonSerialized] public Vector3 lastPos;
            [NonSerialized] public float upDist;
        }

        public enum FootState
        {
            Up,
            Down
        }
    }
    
    public enum FootRaycastDirection
    {
        DownGlobal,
        DownLocal
    }

    public interface IFootstepSource
    {
        Entity Entity    { get; }
        bool Spatialized { get; }
    }

    [Serializable]
    public class FootstepParams
    {
        public FootstepType footstepType = FootstepType.Human;
        public float impulse             = 0.0f;
        public float impulseRadius       = 15.0f;
        public AudioParams additionalSound;
    }

}
