using UnityEngine;

namespace OathFramework.Effects
{
    public interface IProjectileVFX
    {
        void Initialize(Transform bulletTransform, Transform muzzleTransform, ParticleSystem.MinMaxGradient? color);
        void OnBulletReturn();
    }
}
