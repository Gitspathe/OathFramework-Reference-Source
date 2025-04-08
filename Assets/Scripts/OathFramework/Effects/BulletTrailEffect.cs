using UnityEngine;

namespace OathFramework.Effects
{
    
    [RequireComponent(typeof(Effect))]
    public class BulletTrailEffect : MonoBehaviour, IProjectileVFX
    {
        [SerializeField] private ParticleSystem particles;

        private Transform bulletTransform;
        private Transform muzzleTransform;
        private Transform cTransform;
        private int frame;
        
        private void Awake()
        {
            cTransform = transform;
        }

        public void Initialize(Transform bulletTransform, Transform muzzleTransform, ParticleSystem.MinMaxGradient? color)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            frame                = 0;
            this.bulletTransform = bulletTransform;
            this.muzzleTransform = muzzleTransform;
            GetComponent<ColorableEffect>().SetColor(color);
        }
        
        private void LateUpdate()
        {
            if(frame++ <= 1) {
                if(muzzleTransform != null) {
                    cTransform.SetPositionAndRotation(muzzleTransform.position, muzzleTransform.rotation);
                }
                particles.Play();
                return;
            }
            if(ReferenceEquals(bulletTransform, null))
                return;
            
            cTransform.SetPositionAndRotation(bulletTransform.position, bulletTransform.rotation);
        }

        public void OnBulletReturn()
        {
            bulletTransform = null;
        }
    }

}
