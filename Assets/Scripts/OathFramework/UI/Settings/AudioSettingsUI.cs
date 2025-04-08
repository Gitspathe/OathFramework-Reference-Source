using OathFramework.Settings;
using UnityEngine;
using UnityEngine.UI;
using AudioSettings = OathFramework.Settings.SettingsManager.AudioSettings;

namespace OathFramework.UI.Settings
{
    public class AudioSettingsUI : MonoBehaviour
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider ambienceVolumeSlider;
        
        private static AudioSettings ManagerAudioSettings {
            get => SettingsManager.Instance.CurrentSettings.audio;
            set => SettingsManager.Instance.CurrentSettings.audio = value;
        }

        public static AudioSettingsUI Instance { get; private set; }

        private bool init;
        
        public AudioSettingsUI Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(AudioSettingsUI)} singleton.");
                Destroy(Instance);
                return null;
            }
            
            Instance = this;
            init     = true;
            return this;
        }

        public void AudioSettingsChanged()
        {
            ManagerAudioSettings.master   = masterVolumeSlider.value / 10.0f;
            ManagerAudioSettings.music    = musicVolumeSlider.value / 10.0f;
            ManagerAudioSettings.sfx      = sfxVolumeSlider.value / 10.0f;
            ManagerAudioSettings.ambience = ambienceVolumeSlider.value / 10.0f;
            SettingsManager.Instance.ApplyAudio();
        }

        public void Tick()
        {
            masterVolumeSlider.SetValueWithoutNotify((int)(ManagerAudioSettings.master * 10));
            musicVolumeSlider.SetValueWithoutNotify((int)(ManagerAudioSettings.music * 10));
            sfxVolumeSlider.SetValueWithoutNotify((int)(ManagerAudioSettings.sfx * 10));
            ambienceVolumeSlider.SetValueWithoutNotify((int)(ManagerAudioSettings.ambience * 10));
        }
    }
}
