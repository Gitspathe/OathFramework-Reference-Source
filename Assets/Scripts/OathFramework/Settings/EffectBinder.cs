using UnityEngine;

namespace OathFramework.Settings
{

    public class EffectBinder : MonoBehaviour
    {
        [SerializeField] private GameObject[] elements;
        
        [Space(10)]
        
        [SerializeField] private GameObject[] elementsLow;
        [SerializeField] private GameObject[] elementsMedium;
        [SerializeField] private GameObject[] elementsHigh;
        [SerializeField] private GameObject[] elementsUltra;

        private void Awake()
        {
            if(ReferenceEquals(SettingsManager.Instance, null))
                return;
            
            SettingsManager.RegisterEffect(this);
        }

        private void OnDestroy()
        {
            SettingsManager.UnregisterEffect(this);
        }

        public void Apply(SettingsManager.GraphicsSettings settings)
        {
            foreach(GameObject go in elements) {
                go.SetActive(false);
            }

            GameObject[] collection;
            switch(settings.effects) {
                case 1:
                    collection = elementsMedium;
                    break;
                case 2:
                    collection = elementsHigh;
                    break;
                case 3:
                    collection = elementsUltra;
                    break;
                
                case 0:
                default:
                    collection = elementsLow;
                    break;    
            }
            foreach(GameObject go in collection) {
                go.SetActive(true);
                if(gameObject.activeSelf && go.TryGetComponent(out ParticleSystem ps)) {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            OnApplied(settings, collection);
        }
        
        protected virtual void OnApplied(SettingsManager.GraphicsSettings settings, GameObject[] collection) { }
    }

}
