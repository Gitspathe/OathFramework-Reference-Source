using UnityEngine;

namespace OathFramework.Effects
{
    public interface IColorable
    {
        void SetColor(ParticleSystem.MinMaxGradient? color = null);
    }
}
