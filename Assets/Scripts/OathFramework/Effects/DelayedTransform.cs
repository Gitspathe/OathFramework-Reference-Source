using UnityEngine;

namespace OathFramework.Effects
{
    public class DelayedTransform : MonoBehaviour
    {
        [field: SerializeField] public Transform TargetTransform { get; private set; }
        
        private Transform cTransform;

        private void Awake()
        {
            cTransform = transform;
        }

        public void SetTarget(Transform target)
        {
            TargetTransform = target;
        }

        private void LateUpdate()
        {
            if(TargetTransform == null)
                return;

            cTransform.SetPositionAndRotation(TargetTransform.position, TargetTransform.rotation);
        }
    }
}
