using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EquipmentSystem;
using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.Effects
{
    [RequireComponent(typeof(LineRenderer))]
    public class PreviewTrajectory : LoopComponent, 
        ILoopUpdate, IPoolableComponent
    {
        [SerializeField] private LayerMask collisionMask;

        private IEntityModelThrow parent;
        private LineRenderer lineRenderer;
        
        public PoolableGameObject PoolableGO { get; set; }

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        public PreviewTrajectory Initialize(IEntityModelThrow parent)
        {
            this.parent = parent;
            Exec();
            return this;
        }

        void ILoopUpdate.LoopUpdate()
        {
            Exec();
        }

        private void Exec()
        {
            EquippableManager equipInst = EquippableManager.Instance;
            lineRenderer.positionCount  = Mathf.CeilToInt(equipInst.ArcPointCount / equipInst.ArcTimeBetweenPoints) + 1;
            Vector3 startPosition       = parent.ThrowOffsetTransform.position;
            Vector3 startVelocity       = parent.GetThrowStrength();
            int i                       = 0;
            lineRenderer.SetPosition(i, startPosition);
            for(float time = 0.0f; time < equipInst.ArcPointCount; time += equipInst.ArcTimeBetweenPoints) {
                i++;
                Vector3 point = startPosition + time * startVelocity;
                point.y = startPosition.y + startVelocity.y * time + (Physics.gravity.y / 2.0f * time * time);
                lineRenderer.SetPosition(i, point);
                
                Vector3 lastPos = lineRenderer.GetPosition(i - 1);
                if(!Physics.Raycast(lastPos, (point - lastPos).normalized, out RaycastHit hit, (point - lastPos).magnitude, collisionMask))
                    continue;

                lineRenderer.SetPosition(i, hit.point);
                lineRenderer.positionCount = i + 1;
                break;
            }
        }

        public void OnRetrieve() { }

        public void OnReturn(bool initialization)
        {
            if(parent == null)
                return;

            parent.TrajectoryArc = null;
            parent               = null;
        }
    }
}
