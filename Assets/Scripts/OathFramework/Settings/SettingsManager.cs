using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.Networking;
using OathFramework.UI.Settings;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace OathFramework.Settings
{ 

    public sealed class SettingsManager : Subsystem, IInitialized
    {
        [field: SerializeField] public ScriptableRendererFeature SsaoRenderFeature    { get; private set; }
        [field: SerializeField] public ScriptableRendererFeature DecalRenderFeature   { get; private set; }
        [field: SerializeField] public ScriptableRendererFeature TransUIRenderFeature { get; private set; }
        [field: SerializeField] public UniversalRenderPipelineAsset[] PipelineAssets  { get; private set; }
        [field: SerializeField] public UniversalRendererData RenderDataAsset          { get; private set; }
        [field: SerializeField] public PostProcessData PostProcessData                { get; private set; }

        [field: SerializeField] public InputActionAsset Controls                      { get; private set; }
        [field: SerializeField] public GameSettings DefaultGameSettings               { get; private set; }
        [field: SerializeField] public AudioSettings DefaultAudioSettings             { get; private set; }
        [field: SerializeField] public MobileGraphicsPresets MobileDefaultGfxPreset   { get; private set; }
        [field: SerializeField] public DesktopGraphicsPresets DesktopDefaultGfxPreset { get; private set; }


        [field: SerializeField, ListDrawerSettings(NumberOfItemsPerPage = 1)] 
        public GraphicsSettings[] DesktopGraphicsPresets { get; private set; }

        [field: SerializeField, ListDrawerSettings(NumberOfItemsPerPage = 1)]
        public GraphicsSettings[] MobileGraphicsPresets  { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public AudioMixer AudioMixer                  { get; private set; }

        [field: Space(10), Header("Strings")]
        
        [field: SerializeField] public LocalizedString AutoSettingsTitleStr   { get; private set; }
        [field: SerializeField] public LocalizedString AutoSettingsFailedStr  { get; private set; }
        [field: SerializeField] public LocalizedString AutoSettingsSuccessStr { get; private set; }

        public Settings CurrentSettings { get; private set; }

        private static HashSet<VolumeBinder> regVolumes         = new();
        private static HashSet<TerrainBinder> regTerrains       = new();
        private static HashSet<MaterialBinderBase> regMaterials = new();
        private static HashSet<MeshBinder> regMeshes            = new();
        private static HashSet<EffectBinder> regEffects         = new();
        private static HashSet<RagdollTarget> regRagdolls       = new();
        private static HashSet<DecalBinderBase> regDecals       = new();
        private static HashSet<CameraBinder> regCameras         = new();
        private static HashSet<ShadowCasterBinder> regShadows   = new();
        private static bool isSaving;

        public static GraphicsDeviceType GfxDeviceType { get; private set; }
        public static bool GfxIsOpenGL                 { get; private set; }
        public static SettingsManager Instance         { get; private set; }

        public override string Name    => "Settings Manager";
        public override uint LoadOrder => SubsystemLoadOrders.SettingsManager;
        
        uint ILockableOrderedListElement.Order => 0;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(SettingsManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            
            try {
                GfxDeviceType = SystemInfo.graphicsDeviceType;
                GfxIsOpenGL   = GfxDeviceType == GraphicsDeviceType.OpenGLCore 
                                || GfxDeviceType == GraphicsDeviceType.OpenGLES2 
                                || GfxDeviceType == GraphicsDeviceType.OpenGLES3;
                if(GfxIsOpenGL) {
                    DecalRenderFeature.SetActive(false);
                }
            } catch(Exception _) {
                GfxIsOpenGL = false; // ???
            }
            
            CreateDefault(false);
            GameCallbacks.Register((IInitialized)this);
            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
            return UniTask.CompletedTask;
        }

        async UniTask IInitialized.OnGameInitialized()
        {
            await LoadControls();
        }

        public async UniTask ApplyInitialSettings()
        {
            if(!FileIO.FileExists($"{FileIO.SavePath}settings.json")) {
                if(Game.Platform == Platforms.Desktop) {
                    CreateDefault(false);
                    AutoSettings.Run();
                    SettingsUI.Instance.RebindManagerSettings();
                    ApplyGame();
                    ApplyGfx();
                    ApplyAudio();
                    return;
                }
                CreateDefault(true);
                return;
            }
            
            try { 
                CurrentSettings = JsonUtility.FromJson<Settings>(await FileIO.LoadFile($"{FileIO.SavePath}settings.json", noHeader: true));
                ApplyGame();
                ApplyGfx();
                ApplyAudio();
            } catch (Exception) {
                Debug.LogError("Failed to load existing settings. Loading default instead.");
                CreateDefault(true);
            }
            SettingsUI.Instance.RebindManagerSettings();
        }

        private void CreateDefault(bool apply) 
        {
            CurrentSettings = new Settings {
                game     = DefaultGameSettings.DeepCopy(),
                audio    = DefaultAudioSettings.DeepCopy(),
#if UNITY_IOS || UNITY_ANDROID
                graphics = MobileGraphicsPresets[(int)MobileDefaultGfxPreset].DeepCopy()
#else
                graphics = DesktopGraphicsPresets[(int)DesktopDefaultGfxPreset].DeepCopy()
#endif
            };
            Resolution[] resolutions             = Screen.resolutions;
            Resolution res                       = resolutions[resolutions.Length - 1];
            CurrentSettings.graphics.resolutionX = res.width;
            CurrentSettings.graphics.resolutionY = res.height;
            CurrentSettings.graphics.vSync       = false;
            CurrentSettings.graphics.displayMode = 1;
#if UNITY_IOS || UNITY_ANDROID
            CurrentSettings.graphics.maxFps = 30;
#else
            CurrentSettings.graphics.maxFps = (int)res.refreshRateRatio.value;
#endif

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            // This updates the UI only. Locale is initially managed by Unity.
            switch(LocalizationSettings.SelectedLocale.Identifier.Code) {
                case "es": {
                    CurrentSettings.game.language = 1;
                } break;

                case "en":
                default: { 
                    CurrentSettings.game.language = 0;
                } break;
            }
            if(apply) {
                ApplyGame();
                ApplyGfx();
                ApplyAudio();
            }
        }
        
        private void SceneManagerOnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            RenderSettings.fog = CurrentSettings.graphics.fog;
        }

        public void SetPreset(int index)
        {
#if UNITY_IOS || UNITY_ANDROID
            GraphicsSettings settings = MobileGraphicsPresets[index];
#else
            GraphicsSettings settings = DesktopGraphicsPresets[index];
#endif
            CurrentSettings.graphics.ApplyPreset(settings);
        }

        public int GetGraphicsPreset(GraphicsSettings gfxSettings)
        {
#if UNITY_IOS || UNITY_ANDROID
            for(int i = 0; i < MobileGraphicsPresets.Length; i++) {
                GraphicsSettings settings = MobileGraphicsPresets[i];
                if(settings.EqualTo(gfxSettings, false))
                    return i;
            }
#else
            for(int i = 0; i < DesktopGraphicsPresets.Length; i++) { 
                GraphicsSettings settings = DesktopGraphicsPresets[i];
                if(settings.EqualTo(gfxSettings, false))
                    return i;
            }
#endif
            return -1;
        }

        public static void RegisterVolume(VolumeBinder volume)
        {
            if(regVolumes.Add(volume) && Game.Initialized) {
                volume.Apply(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterVolume(VolumeBinder volume)
        {
            regVolumes.Remove(volume);
        }

        public static void RegisterTerrain(TerrainBinder terrain)
        {
            if(regTerrains.Add(terrain) && Game.Initialized) {
                terrain.Apply(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterTerrain(TerrainBinder terrain)
        {
            regTerrains.Remove(terrain);
        }

        public static void RegisterMaterial(MaterialBinderBase materialBinder)
        {
            if(regMaterials.Add(materialBinder) && Game.Initialized) {
                materialBinder.Apply(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterMaterial(MaterialBinderBase materialBinder)
        {
            regMaterials.Remove(materialBinder);
        }

        public static void RegisterMesh(MeshBinder meshBinder)
        {
            if(regMeshes.Add(meshBinder) && Game.Initialized) {
                meshBinder.Apply(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterMesh(MeshBinder meshBinder)
        {
            regMeshes.Remove(meshBinder);
        }

        public static void RegisterEffect(EffectBinder effect)
        {
            if(regEffects.Add(effect) && Game.Initialized) {
                effect.Apply(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterEffect(EffectBinder effect)
        {
            regEffects.Remove(effect);
        }
        
        public static void RegisterRagdoll(RagdollTarget ragdoll)
        {
            if(regRagdolls.Add(ragdoll) && Game.Initialized) {
                ragdoll.ApplySettings(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterRagdoll(RagdollTarget ragdoll)
        {
            regRagdolls.Remove(ragdoll);
        }

        public static void RegisterDecal(DecalBinderBase decal)
        {
            if(regDecals.Add(decal) && Game.Initialized) {
                decal.Apply(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterDecal(DecalBinderBase decal)
        {
            regDecals.Remove(decal);
        }
        
        public static void RegisterCamera(CameraBinder cam)
        {
            if(regCameras.Add(cam) && Game.Initialized) {
                cam.Apply(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterCamera(CameraBinder cam)
        {
            regCameras.Remove(cam);
        }

        public static void RegisterShadowCaster(ShadowCasterBinder caster)
        {
            if(regShadows.Add(caster) && Game.Initialized) {
                caster.Apply(Instance.CurrentSettings.graphics);
            }
        }

        public static void UnregisterShadowCaster(ShadowCasterBinder caster)
        {
            regShadows.Remove(caster);
        }

        public void ApplyGame()
        {
            GameSettings settings               = CurrentSettings.game;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[settings.language];
            NetGame.Instance.UseSteam           = settings.networkType == 0;
            
            _ = Save();
        }

        public void ApplyAudio()
        {
            AudioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(CurrentSettings.audio.master, 0.0001f, 1000.0f)) * 20);
            AudioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(CurrentSettings.audio.music, 0.0001f, 1000.0f)) * 20);
            AudioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(CurrentSettings.audio.sfx, 0.0001f, 1000.0f)) * 20);
            AudioMixer.SetFloat("AmbienceVolume", Mathf.Log10(Mathf.Clamp(CurrentSettings.audio.ambience, 0.0001f, 1000.0f)) * 20);

            _ = Save();
        }

        public void ApplyGfx()
        {
            GraphicsSettings settings = CurrentSettings.graphics;
            QualitySettings.SetQualityLevel(settings.shadows);
            QualitySettings.skinWeights = settings.highQualityMeshes ? SkinWeights.FourBones : SkinWeights.OneBone;
            RenderSettings.fog          = settings.fog;
            
            foreach(CameraBinder cam in regCameras) {
                cam.Apply(settings);
            }
            foreach(VolumeBinder volume in regVolumes) {
                volume.Apply(settings);
            }
            foreach(TerrainBinder terrain in regTerrains) {
                terrain.Apply(settings);
            }
            foreach(MaterialBinderBase mat in regMaterials) {
                mat.Apply(settings);
            }
            foreach(MeshBinder mesh in regMeshes) {
                mesh.Apply(settings);
            }
            foreach(EffectBinder effect in regEffects) {
                effect.Apply(settings);
            }
            foreach(RagdollTarget ragdoll in regRagdolls) {
                ragdoll.ApplySettings(settings);
            }
            foreach(DecalBinderBase decal in regDecals) {
                decal.Apply(settings);
            }
            foreach(ShadowCasterBinder caster in regShadows) {
                caster.Apply(settings);
            }

            FullScreenMode screenMode;
            switch(settings.displayMode) {
                case 1: {
                    screenMode = FullScreenMode.FullScreenWindow;
                } break;
                case 2: {
                    screenMode = FullScreenMode.Windowed;
                } break;

                case 0:
                default: {
                    screenMode = FullScreenMode.ExclusiveFullScreen;
                } break;
            }
#if !UNITY_IOS && !UNITY_ANDROID
            Screen.SetResolution(settings.resolutionX, settings.resolutionY, screenMode);
#else
            QualitySettings.resolutionScalingFixedDPIFactor = settings.dpiScale;
#endif
            QualitySettings.anisotropicFiltering = settings.anisotropicFiltering ? AnisotropicFiltering.Enable : AnisotropicFiltering.Disable;
            QualitySettings.vSyncCount           = settings.vSync ? 1 : 0;
            Application.targetFrameRate          = settings.maxFps;
            foreach(UniversalRenderPipelineAsset urpAsset in PipelineAssets) {
                urpAsset.supportsHDR = settings.hdrRendering;
                if(settings.extraAntialiasing == 2) {
                    urpAsset.msaaSampleCount = 1;
                } else {
                    switch(settings.antialiasing) {
                        case 1:
                            urpAsset.msaaSampleCount = 2;
                            break;
                        case 2:
                            urpAsset.msaaSampleCount = 4;
                            break;
                        case 3:
                            urpAsset.msaaSampleCount = 8;
                            break;

                        case 0:
                        default:
                            urpAsset.msaaSampleCount = 1;
                            break;
                    }
                }
            }
            RenderDataAsset.postProcessData = settings.postProcessing ? PostProcessData : null;
            SsaoRenderFeature.SetActive(settings.ssao);
            DecalRenderFeature.SetActive(!GfxIsOpenGL && settings.highQualityDecals);
#if !UNITY_IOS && !UNITY_ANDROID
            TransUIRenderFeature.SetActive(settings.postProcessing);
#endif
            switch(settings.textures) {
                case 1: {
                    QualitySettings.globalTextureMipmapLimit = 0;
                } break;

                case 0:
                default: {
                    QualitySettings.globalTextureMipmapLimit = 1;
                } break;
            }
            
            Resources.UnloadUnusedAssets();
            _ = Save();
        }
        
        private async UniTask Save()
        {
            if(isSaving)
                return;
            
            isSaving = true;
            await FileIO.SaveFile($"{FileIO.SavePath}settings.json", JsonUtility.ToJson(CurrentSettings), noHeader: true);
            isSaving = false;
        }
        
        public static async UniTask SaveControls()
        {
            try {
                await FileIO.SaveFile($"{FileIO.SavePath}controls.json", Instance.Controls.SaveBindingOverridesAsJson(), noHeader: true);
            } catch(Exception e) {
                Debug.LogError(e);
            }
        }

        public static async UniTask LoadControls()
        {
            if(!FileIO.FileExists($"{FileIO.SavePath}controls.json"))
                return;

            try {
                Instance.Controls.LoadBindingOverridesFromJson(await FileIO.LoadFile($"{FileIO.SavePath}controls.json", noHeader: true));
                ControlsSettingsUI.Instance.Tick();
            } catch(Exception e) {
                Debug.LogError(e);
            }
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
        }
        
#if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            SsaoRenderFeature.SetActive(true);
            DecalRenderFeature.SetActive(true);
            foreach(UniversalRenderPipelineAsset urpAsset in PipelineAssets) {
                urpAsset.supportsHDR     = true;
                urpAsset.msaaSampleCount = 0;
            }
            RenderDataAsset.postProcessData = PostProcessData;
        }
#endif

        [Serializable]
        public class Settings
        {
            public GameSettings game         = new();
            public GraphicsSettings graphics = new();
            public AudioSettings audio       = new();

            public Settings DeepCopy()
            {
                return new Settings {
                    game     = game.DeepCopy(),
                    graphics = graphics.DeepCopy(),
                    audio    = audio.DeepCopy()
                };
            }
        }

        [Serializable]
        public class GameSettings
        {
            public int language;
            public int networkType;
            public string multiplayerName;

            public const int MaxNameLen = 16;

            public string GetMultiplayerName() => FormatName(multiplayerName);

            public static string FormatName(string name) 
                => name.Length <= MaxNameLen ? name : name.Substring(0, MaxNameLen) + "...";

            public GameSettings DeepCopy()
            {
                return new GameSettings {
                    language        = language,
                    networkType     = networkType, 
                    multiplayerName = multiplayerName
                };
            }
        }

        [Serializable]
        public class GraphicsSettings
        {
            public int textures;
            public int shadows;
            public int effects;
            public int ragdolls;
            public int vegetation;
            public int antialiasing;
            public int extraAntialiasing;
            public float dpiScale;
            public bool fog;
            public bool postProcessing;
            public bool hdrRendering;
            public bool highQualityMaterials;
            public bool highQualityMeshes;
            public bool highQualityDecals;
            public bool highQualityLighting;
            public bool anisotropicFiltering;
            public bool bloom;
            public bool chromaticAberration;
            public bool motionBlur;
            public bool vignette;
            public bool ssao;
            
            [HideInInspector] public bool vSync;
            [HideInInspector] public int resolutionX;
            [HideInInspector] public int resolutionY;
            [HideInInspector] public int displayMode;
            [HideInInspector] public int maxFps;
            
            public GraphicsSettings DeepCopy()
            {
                return new GraphicsSettings {
                    textures             = textures,
                    shadows              = shadows,
                    effects              = effects,
                    ragdolls             = ragdolls,
                    vegetation           = vegetation,
                    antialiasing         = antialiasing,
                    extraAntialiasing    = extraAntialiasing,
                    dpiScale             = dpiScale,
                    fog                  = fog,
                    postProcessing       = postProcessing,
                    hdrRendering         = hdrRendering,
                    highQualityMaterials = highQualityMaterials,
                    highQualityMeshes    = highQualityMeshes,
                    highQualityDecals    = highQualityDecals,
                    highQualityLighting  = highQualityLighting,
                    anisotropicFiltering = anisotropicFiltering,
                    bloom                = bloom,
                    chromaticAberration  = chromaticAberration,
                    motionBlur           = motionBlur,
                    vignette             = vignette,
                    ssao                 = ssao,
                    vSync                = vSync,
                    resolutionX          = resolutionX,
                    resolutionY          = resolutionY,
                    displayMode          = displayMode,
                    maxFps               = maxFps
                };
            }

            public void ApplyPreset(GraphicsSettings preset)
            {
                textures             = preset.textures;
                shadows              = preset.shadows;
                effects              = preset.effects;
                ragdolls             = preset.ragdolls;
                vegetation           = preset.vegetation;
                antialiasing         = preset.antialiasing;
                extraAntialiasing    = preset.extraAntialiasing;
                dpiScale             = preset.dpiScale;
                fog                  = preset.fog;
                postProcessing       = preset.postProcessing;
                hdrRendering         = preset.hdrRendering;
                highQualityMaterials = preset.highQualityMaterials;
                highQualityMeshes    = preset.highQualityMeshes;
                highQualityDecals    = preset.highQualityDecals;
                highQualityLighting  = preset.highQualityLighting;
                anisotropicFiltering = preset.anisotropicFiltering;
                bloom                = preset.bloom;
                chromaticAberration  = preset.chromaticAberration;
                motionBlur           = preset.motionBlur;
                vignette             = preset.vignette;
                ssao                 = preset.ssao;
            }

            public bool EqualTo(GraphicsSettings other, bool allParams)
            {
                // Resolution, vsync & max fps are ignored.
                return other.textures == textures 
                    && other.shadows == shadows 
                    && other.effects == effects 
                    && other.ragdolls == ragdolls
                    && other.vegetation == vegetation
                    && other.antialiasing == antialiasing 
                    && other.extraAntialiasing == extraAntialiasing
                    && Math.Abs(other.dpiScale - dpiScale) < 0.001f
                    && other.highQualityMaterials == highQualityMaterials
                    && other.highQualityMeshes == highQualityMeshes
                    && other.highQualityDecals == highQualityDecals
                    && other.highQualityLighting == highQualityLighting
                    && other.anisotropicFiltering == anisotropicFiltering
                    && other.bloom == bloom
                    && other.chromaticAberration == chromaticAberration
                    && other.motionBlur == motionBlur
                    && other.vignette == vignette
                    && other.ssao == ssao
                    && other.postProcessing == postProcessing
                    && other.fog == fog
                    && other.hdrRendering == hdrRendering
                    && (!allParams || other.maxFps == maxFps)
                    && (!allParams || other.resolutionX == resolutionX)
                    && (!allParams || other.resolutionY == resolutionY)
                    && (!allParams || other.vSync == vSync)
                    && (!allParams || other.displayMode == displayMode);
            }
        }
        
        [Serializable]
        public class AudioSettings
        {
            public float master;
            public float music;
            public float sfx;
            public float ambience;

            public AudioSettings DeepCopy()
            {
                return new AudioSettings { 
                    master   = master,
                    music    = music,
                    sfx      = sfx,
                    ambience = ambience
                };
            }
        }
    }
    
    public enum DesktopGraphicsPresets
    {
        VeryLow,
        Low,
        Medium,
        High,
        Ultra
    }

    public enum MobileGraphicsPresets
    {
        Low,
        High,
        Ultra
    }

}
