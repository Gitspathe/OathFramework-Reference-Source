using UnityEngine;
using OathFramework.Pooling;

namespace OathFramework.Effects
{ 

    [RequireComponent(typeof(PoolableGameObject))]
    public class CameraImpulseEffectGO : MonoBehaviour, IPoolableComponent
    {
        [SerializeField] private LayerMask layerMask;

        PoolableGameObject IPoolableComponent.PoolableGO { get; set; }

        public void Initialize(float radius, float duration, float magnitude, AnimationCurve falloff = null)
        {
            Camera main = Camera.main;
            if(main == null)
                return;
            
            CameraShake shake = main.GetComponent<CameraShake>();
            Vector3 camVec    = new (main.transform.position.x, 0.0f, main.transform.position.z);
            Vector3 thisVec   = new(transform.position.x, 0.0f, transform.position.z);
            float dist        = Vector3.Distance(camVec, thisVec);
            if(dist > radius)
                return;
            
            magnitude *= falloff?.Evaluate(dist / radius) ?? 1.0f;
            shake.Shake(duration, magnitude);
            PoolManager.Return(this);
        }

        void IPoolableComponent.OnRetrieve()
        {

        }

        void IPoolableComponent.OnReturn(bool initialization)
        {

        }
    }

}
