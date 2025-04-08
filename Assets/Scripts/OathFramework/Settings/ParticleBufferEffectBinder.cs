using UnityEngine;

namespace OathFramework.Settings
{
    public class ParticleBufferEffectBinder : EffectBinder
    {
        [SerializeField] private int countLow;
        [SerializeField] private int countMedium;
        [SerializeField] private int countHigh;
        [SerializeField] private int countUltra;

        protected override void OnApplied(SettingsManager.GraphicsSettings settings, GameObject[] collection)
        {
            foreach(GameObject go in collection) {
                go.SetActive(true);
                if(!gameObject.activeSelf || !go.TryGetComponent(out ParticleSystem ps))
                    continue;
                
                ParticleSystem.MainModule main = ps.main;
                switch(settings.effects) {
                    case 1:
                        main.maxParticles = countMedium;
                        break;
                    case 2:
                        main.maxParticles = countHigh;
                        break;
                    case 3:
                        main.maxParticles = countUltra;
                        break;
            
                    case 0:
                    default:
                        main.maxParticles = countLow;
                        break;
                }
            }
        }
    }
}
