using OathFramework.Pooling;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OathFramework.Effects
{

    public class Decal : MonoBehaviour, IPoolableComponent
    {
        [SerializeField] private float randomDepth = 0.1f;
        [SerializeField] private float randomScale = 0.25f;
        [SerializeField] private float depthMalus;
        [SerializeField] private bool projected;

        [SerializeField] private bool randomPosition;
        
        [SerializeField, ShowIf("@randomPosition")] 
        private Vector3 randomMin;
        
        [SerializeField, ShowIf("@randomPosition")] 
        private Vector3 randomMax;
        
        private Transform cTransform;
        private Vector3 baseScale;
        private Quaternion baseRot;
        private Vector3 basePos;
        private bool init;
        
        public PoolableGameObject PoolableGO { get; set; }

        private void Awake()
        {
            cTransform = transform;
        }

        private void OnEnable()
        {
            if(!init)
                return;

            FRandom rand        = FRandom.Cache;
            Transform trans     = cTransform;
            float randScale     = randomScale > 0.0f ? 1.0f + rand.Range(-randomScale, randomScale) : 1.0f;
            trans.localPosition = basePos;
            trans.localRotation = baseRot;
            if(randomDepth > 0.0f) {
                transform.position += projected 
                    ? transform.forward * rand.Range(0.0f, randomDepth) 
                    : transform.up * rand.Range(0.0f, randomDepth);
            }
            
            trans.localScale = new Vector3(
                baseScale.x * randScale,
                baseScale.y * randScale, 
                baseScale.z * randScale);
            trans.localRotation *= projected 
                ? Quaternion.Euler(0.0f, 0.0f, rand.Range(0.0f, 360.0f)) 
                : Quaternion.Euler(0.0f, rand.Range(0.0f, 360.0f), 0.0f);

            if(randomPosition) {
                trans.localPosition = new Vector3(rand.Range(
                    randomMin.x, randomMax.x), 
                    Random.Range(randomMin.y, randomMax.y), 
                    Random.Range(randomMin.z, randomMax.z)
                );
            }
            if(Mathf.Abs(depthMalus) > 0.0f) {
                trans.position -= projected ? transform.forward * depthMalus : transform.up * depthMalus;
            }
        }

        public void OnRetrieve()
        {
            
        }

        public void OnReturn(bool initialization)
        {
            if(!initialization)
                return;

            cTransform = transform;
            baseScale  = cTransform.localScale;
            basePos    = cTransform.localPosition;
            baseRot    = cTransform.localRotation;
            init       = true;
        }
    }

}
