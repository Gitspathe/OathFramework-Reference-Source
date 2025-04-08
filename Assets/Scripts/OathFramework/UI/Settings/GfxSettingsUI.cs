using OathFramework.Settings;
using OathFramework.UI.Platform;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static OathFramework.Settings.SettingsManager;

namespace OathFramework.UI.Settings
{
    public class GfxSettingsUI : MonoBehaviour
    {
        [SerializeField] private Button graphicsApplyButton;
        [SerializeField] private Button graphicsRevertButton;

        [Space(10)] 
        
        [SerializeField] private TMP_Dropdown desktopPresetDropdown;
        [SerializeField] private TMP_Dropdown mobilePresetDropdown;
        [SerializeField] private TMP_Dropdown mobileMaxFpsDropdown;

        [Space(10)] 
        
        [SerializeField] private TMP_Dropdown texturesSetting;
        [SerializeField] private TMP_Dropdown shadowsSetting;
        [SerializeField] private TMP_Dropdown effectsSetting;
        [SerializeField] private TMP_Dropdown ragdollSetting;
        [SerializeField] private TMP_Dropdown vegetationSetting;
        [SerializeField] private TMP_Dropdown antiAliasingSetting;
        [SerializeField] private TMP_Dropdown extraAntiAliasingSetting;
        [SerializeField] private Toggle fogSetting;
        [SerializeField] private Toggle postProcessingSetting;
        [SerializeField] private Toggle advancedMeshesSetting;
        [SerializeField] private Toggle advancedShadersSetting;
        [SerializeField] private Toggle advancedDecalsSetting;
        [SerializeField] private Toggle advancedLightingSetting;
        [SerializeField] private Toggle textureFilterSetting;
        [SerializeField] private Toggle bloomSetting;
        [SerializeField] private Toggle chromaticSetting;
        [SerializeField] private Toggle motionBlurSetting;
        [SerializeField] private Toggle vignetteSetting;
        [SerializeField] private Toggle ssaoSetting;
        [SerializeField] private TMP_Dropdown maxFPSSetting;
        [SerializeField] private TMP_Dropdown displayModeSetting;
        [SerializeField] private TMP_Dropdown resolutionSetting;
        [SerializeField] private Toggle vsyncSetting;
        
        private bool init;
        private int desktopCustomPreset = 5;
        private UIDropdownParent dropdownParent;
        private GraphicsSettings curGfxSettings;
        private GraphicsSettings prevGfxSettings;
        
        private static GraphicsSettings ManagerGfxSettings {
            get => SettingsManager.Instance.CurrentSettings.graphics;
            set => SettingsManager.Instance.CurrentSettings.graphics = value;
        }

        public bool PendingChanges => !curGfxSettings.EqualTo(prevGfxSettings, true);

        public static GfxSettingsUI Instance { get; private set; }
        
        public GfxSettingsUI Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(GfxSettingsUI)} singleton.");
                Destroy(Instance);
                return null;
            }

            dropdownParent  = GetComponent<UIDropdownParent>();
            curGfxSettings  = ManagerGfxSettings.DeepCopy();
            prevGfxSettings = curGfxSettings.DeepCopy();
#if UNITY_IOS || UNITY_ANDROID
            desktopPresetDropdown.transform.parent.gameObject.SetActive(false);
            mobilePresetDropdown.transform.parent.gameObject.SetActive(true);
            mobileMaxFpsDropdown.transform.parent.gameObject.SetActive(true);
            texturesSetting.transform.parent.gameObject.SetActive(false);
            shadowsSetting.transform.parent.gameObject.SetActive(false);
            effectsSetting.transform.parent.gameObject.SetActive(false);
            ragdollSetting.transform.parent.gameObject.SetActive(false);
            vegetationSetting.transform.parent.gameObject.SetActive(false);
            antiAliasingSetting.transform.parent.gameObject.SetActive(false);
            fogSetting.transform.parent.gameObject.SetActive(false);
            postProcessingSetting.transform.parent.gameObject.SetActive(false);
            extraAntiAliasingSetting.transform.parent.gameObject.SetActive(false);
            advancedMeshesSetting.transform.parent.gameObject.SetActive(false);
            advancedShadersSetting.transform.parent.gameObject.SetActive(false);
            advancedDecalsSetting.transform.parent.gameObject.SetActive(false);
            advancedLightingSetting.transform.parent.gameObject.SetActive(false);
            textureFilterSetting.transform.parent.gameObject.SetActive(false);
            bloomSetting.transform.parent.gameObject.SetActive(false);
            chromaticSetting.transform.parent.gameObject.SetActive(false);
            motionBlurSetting.transform.parent.gameObject.SetActive(false);
            vignetteSetting.transform.parent.gameObject.SetActive(false);
            ssaoSetting.transform.parent.gameObject.SetActive(false);
            maxFPSSetting.transform.parent.gameObject.SetActive(false);
            displayModeSetting.transform.parent.gameObject.SetActive(false);
            resolutionSetting.transform.parent.gameObject.SetActive(false);
            vsyncSetting.transform.parent.gameObject.SetActive(false);
#else
            desktopPresetDropdown.transform.parent.gameObject.SetActive(true);
            mobilePresetDropdown.transform.parent.gameObject.SetActive(false);
            mobileMaxFpsDropdown.transform.parent.gameObject.SetActive(false);
            advancedDecalsSetting.transform.parent.gameObject.SetActive(!GfxIsOpenGL);
#endif
            InitializeResolutions();
            Instance = this;
            init     = true;
            return this;
        }
        
        public void RebindManagerSettings()
        {
            curGfxSettings = ManagerGfxSettings.DeepCopy();
            prevGfxSettings = curGfxSettings.DeepCopy();
            UpdateGraphicsUI();
        }

        private void InitializeResolutions()
        {
            IEnumerable<Resolution> resolutions = Screen.resolutions
                .Select(resolution => new Resolution { width = resolution.width, height = resolution.height })
                .Distinct();

            resolutionSetting.ClearOptions();
            List<string> options = new();
            foreach(Resolution res in resolutions) {
                options.Add($"{res.width}x{res.height}");
            }
            resolutionSetting.AddOptions(options);
        }
        
        public void ChangePreset()
        {
#if UNITY_IOS || UNITY_ANDROID
            //if(mobilePresetDropdown.value == mobileCustomPreset)
            //    return;

            SettingsManager.Instance.SetPreset(mobilePresetDropdown.value);
#else
            if(desktopPresetDropdown.value == desktopCustomPreset)
                return;

            SettingsManager.Instance.SetPreset(desktopPresetDropdown.value);
#endif
            curGfxSettings = ManagerGfxSettings.DeepCopy();
            UpdateGraphicsUI();
        }

        public void UpdatePostProcessing()
        {
            curGfxSettings.hdrRendering   = postProcessingSetting.isOn;
            curGfxSettings.postProcessing = postProcessingSetting.isOn;
            if(!curGfxSettings.postProcessing) {
                curGfxSettings.extraAntialiasing   = 0;
                curGfxSettings.bloom               = false;
                curGfxSettings.chromaticAberration = false;
                curGfxSettings.motionBlur          = false;
                curGfxSettings.vignette            = false;
                curGfxSettings.ssao                = false;
            }
            UpdateGraphicsUI();
            UpdateCurrentSettings();
        }

        public void UpdateCurrentSettings()
        {
            curGfxSettings.textures = texturesSetting.value;
            if(curGfxSettings.shadows == 0 && shadowsSetting.value == 0) {
                curGfxSettings.shadows = 0;
            } else {
                curGfxSettings.shadows = shadowsSetting.value + 1;
            }
            curGfxSettings.effects              = effectsSetting.value;
            curGfxSettings.ragdolls             = ragdollSetting.value;
            curGfxSettings.vegetation           = vegetationSetting.value;
            curGfxSettings.antialiasing         = antiAliasingSetting.value;
            curGfxSettings.extraAntialiasing    = extraAntiAliasingSetting.value;
            curGfxSettings.fog                  = fogSetting.isOn;
            curGfxSettings.highQualityMeshes    = advancedMeshesSetting.isOn;
            curGfxSettings.highQualityMaterials = advancedShadersSetting.isOn;
            curGfxSettings.highQualityDecals    = advancedDecalsSetting.isOn;
            curGfxSettings.highQualityLighting  = advancedLightingSetting.isOn;
            curGfxSettings.anisotropicFiltering = textureFilterSetting.isOn;
            curGfxSettings.bloom                = bloomSetting.isOn;
            curGfxSettings.chromaticAberration  = chromaticSetting.isOn;
            curGfxSettings.motionBlur           = motionBlurSetting.isOn;
            curGfxSettings.vignette             = vignetteSetting.isOn;
            curGfxSettings.ssao                 = ssaoSetting.isOn;
            curGfxSettings.displayMode          = displayModeSetting.value;
            curGfxSettings.vSync                = vsyncSetting.isOn;
#if UNITY_IOS || UNITY_ANDROID
            switch(mobileMaxFpsDropdown.value) {
                case 1:
                    curGfxSettings.maxFps = 60;
                    break;

                case 0:
                default:
                    curGfxSettings.maxFps = 30;
                    break;
            }
#else
            switch(maxFPSSetting.value) {
                case 1:
                    curGfxSettings.maxFps = 30;
                    break;
                case 2:
                    curGfxSettings.maxFps = 60;
                    break;
                case 3:
                    curGfxSettings.maxFps = 120;
                    break;
                case 4:
                    curGfxSettings.maxFps = 144;
                    break;

                case 0:
                default:
                    curGfxSettings.maxFps = 0;
                    break;
            }
#endif

            string resolution          = resolutionSetting.options[resolutionSetting.value].text;
            curGfxSettings.resolutionX = int.Parse(resolution.Split('x')[0]);
            curGfxSettings.resolutionY = int.Parse(resolution.Split('x')[1]);
            UpdateGraphicsUI();
        }

        public void UpdateGraphicsUI()
        {
            texturesSetting.SetValueWithoutNotify(curGfxSettings.textures);
            shadowsSetting.SetValueWithoutNotify(curGfxSettings.shadows <= 1 ? 0 : curGfxSettings.shadows - 1);
            effectsSetting.SetValueWithoutNotify(curGfxSettings.effects);
            ragdollSetting.SetValueWithoutNotify(curGfxSettings.ragdolls);
            vegetationSetting.SetValueWithoutNotify(curGfxSettings.vegetation);
            antiAliasingSetting.SetValueWithoutNotify(curGfxSettings.antialiasing);
            fogSetting.SetIsOnWithoutNotify(curGfxSettings.fog);
            postProcessingSetting.SetIsOnWithoutNotify(curGfxSettings.postProcessing);
            extraAntiAliasingSetting.SetValueWithoutNotify(curGfxSettings.extraAntialiasing);
            advancedMeshesSetting.SetIsOnWithoutNotify(curGfxSettings.highQualityMeshes);
            advancedShadersSetting.SetIsOnWithoutNotify(curGfxSettings.highQualityMaterials);
            advancedDecalsSetting.SetIsOnWithoutNotify(curGfxSettings.highQualityDecals);
            advancedLightingSetting.SetIsOnWithoutNotify(curGfxSettings.highQualityLighting);
            textureFilterSetting.SetIsOnWithoutNotify(curGfxSettings.anisotropicFiltering);
            bloomSetting.SetIsOnWithoutNotify(curGfxSettings.bloom);
            chromaticSetting.SetIsOnWithoutNotify(curGfxSettings.chromaticAberration);
            motionBlurSetting.SetIsOnWithoutNotify(curGfxSettings.motionBlur);
            vignetteSetting.SetIsOnWithoutNotify(curGfxSettings.vignette);
            ssaoSetting.SetIsOnWithoutNotify(curGfxSettings.ssao);
            displayModeSetting.SetValueWithoutNotify(curGfxSettings.displayMode);
            vsyncSetting.SetIsOnWithoutNotify(curGfxSettings.vSync);
#if UNITY_IOS || UNITY_ANDROID
            switch(curGfxSettings.maxFps) {
                case 60:
                    mobileMaxFpsDropdown.SetValueWithoutNotify(1);
                    break;

                default:
                    mobileMaxFpsDropdown.SetValueWithoutNotify(0);
                    break;
            }
#else
            extraAntiAliasingSetting.transform.parent.gameObject.SetActive(curGfxSettings.postProcessing);
            bloomSetting.transform.parent.gameObject.SetActive(curGfxSettings.postProcessing);
            chromaticSetting.transform.parent.gameObject.SetActive(curGfxSettings.postProcessing);
            motionBlurSetting.transform.parent.gameObject.SetActive(curGfxSettings.postProcessing);
            vignetteSetting.transform.parent.gameObject.SetActive(curGfxSettings.postProcessing);
            ssaoSetting.transform.parent.gameObject.SetActive(curGfxSettings.postProcessing);
            switch(curGfxSettings.maxFps) {
                case 30:
                    maxFPSSetting.SetValueWithoutNotify(1);
                    break;
                case 60:
                    maxFPSSetting.SetValueWithoutNotify(2);
                    break;
                case 120:
                    maxFPSSetting.SetValueWithoutNotify(3);
                    break;
                case 144:
                    maxFPSSetting.SetValueWithoutNotify(4);
                    break;

                case 0:
                default:
                    maxFPSSetting.SetValueWithoutNotify(0);
                    break;
            }
#endif
            if(!curGfxSettings.EqualTo(prevGfxSettings, true)) {
                graphicsRevertButton.gameObject.SetActive(true);
                graphicsApplyButton.gameObject.SetActive(true);
            } else {
                graphicsRevertButton.gameObject.SetActive(false);
                graphicsApplyButton.gameObject.SetActive(false);
            }

            string resolution = $"{curGfxSettings.resolutionX}x{curGfxSettings.resolutionY}";
            for(int i = 0; i < resolutionSetting.options.Count; i++) {
                if(resolutionSetting.options[i].text != resolution)
                    continue;

                resolutionSetting.SetValueWithoutNotify(i);
                break;
            }
            UpdateSelectedPreset();
        }

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void UpdateSelectedPreset()
        {
            int customPreset      = 0;
            int curPreset         = SettingsManager.Instance.GetGraphicsPreset(curGfxSettings);
            TMP_Dropdown dropdown = null;
#if UNITY_IOS || UNITY_ANDROID
            dropdown              = mobilePresetDropdown;
            //customPreset        = mobileCustomPreset;
#else
            dropdown              = desktopPresetDropdown;
            customPreset          = desktopCustomPreset;
#endif
            int preset = curPreset == -1 ? customPreset : curPreset;
            TMP_Dropdown.OptionData data = dropdown.options[dropdown.options.Count - 1];
            if(preset == customPreset) {
                if(data.text != "CUSTOM") {
                    dropdown.options.Add(new TMP_Dropdown.OptionData("CUSTOM"));
                }
            } else {
                if(data.text == "CUSTOM") {
                    dropdown.options.RemoveAt(dropdown.options.Count - 1);
                }
            }
            dropdown.SetValueWithoutNotify(curPreset == -1 ? customPreset : curPreset);
        }
        
        public void GraphicsApplyPressed()
        {
            prevGfxSettings    = curGfxSettings.DeepCopy();
            ManagerGfxSettings = curGfxSettings.DeepCopy();
            SettingsManager.Instance.ApplyGfx();
            UIInitialSelect.Sort();
            UpdateGraphicsUI();
        }

        public void GraphicsRevertPressed()
        {
            RevertChanges();
            UpdateGraphicsUI();
        }

        public void RevertChanges()
        {
            curGfxSettings = prevGfxSettings.DeepCopy();
            UpdateGraphicsUI();
        }
    }
}
