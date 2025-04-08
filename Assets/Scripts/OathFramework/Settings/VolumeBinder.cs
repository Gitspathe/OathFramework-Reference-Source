using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OathFramework.Settings
{

    [RequireComponent(typeof(Volume))]
    public class VolumeBinder : MonoBehaviour
    {
        private Volume volume;
        [SerializeField] private bool highQuality;

        private void Awake()
        {
            volume = GetComponent<Volume>();
        }

        private void Start()
        {
            SettingsManager.RegisterVolume(this);
            bool setting = SettingsManager.Instance.CurrentSettings.graphics.postProcessing;
            gameObject.SetActive(setting == highQuality);
        }

        private void OnDestroy()
        {
            SettingsManager.UnregisterVolume(this);
        }

        public void Apply(SettingsManager.GraphicsSettings settings)
        {
            gameObject.SetActive(highQuality && settings.postProcessing);
            if(volume.profile.TryGet(out Bloom blo)) {
                blo.active = settings.bloom;
            }
            if(volume.profile.TryGet(out ChromaticAberration cAbbr)) {
                cAbbr.active = settings.chromaticAberration;
            }
            if(volume.profile.TryGet(out MotionBlur mBlur)) {
                mBlur.active = settings.motionBlur;
            }
            if(volume.profile.TryGet(out Vignette vignette)) {
                vignette.active = settings.vignette;
            }
        }
    }

}
