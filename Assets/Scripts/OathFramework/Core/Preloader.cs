using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace OathFramework.Core
{ 

    public class Preloader : MonoBehaviour
    {
        [SerializeField] private string globalScene;
        [SerializeField] private TextMeshProUGUI taskText;
        [SerializeField] private TextMeshProUGUI subTaskText;
        [SerializeField] private ShaderVariantCollection preloadShaders;
        [SerializeField] private bool doShaderVariantCollectionWarmup = true;
        [SerializeField] private Slider progressSlider;

        public float Progress { get; set; }
        
        public static Preloader Instance;

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError("Attempted to create multiple preloaders.");
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        private void Start()
        {
            INISettings.Load();
            _ = LoadSceneAsync();
        }

        private void Update()
        {
            progressSlider.value = Progress;
        }
        
        public static void SetProgress(string text = null, string subTaskText = null, float? val = null)
        {
            if(Instance == null)
                return;
            
            if(!string.IsNullOrEmpty(text)) {
                Instance.taskText.text = text;
            }
            if(subTaskText != null) {
                Instance.subTaskText.text = subTaskText;
            }
            if(val.HasValue) {
                Instance.Progress = val.Value;
            }
        }

        private async UniTask LoadSceneAsync()
        {
            bool shouldPreloadCollection = INISettings.GetBool("Performance/PrecompileShaders") != false;
            try {
                GraphicsDeviceType gfxDevice = SystemInfo.graphicsDeviceType;
                if(gfxDevice == GraphicsDeviceType.Vulkan 
                   || gfxDevice == GraphicsDeviceType.Direct3D12
                   || gfxDevice == GraphicsDeviceType.Metal) {
                    // Vulkan, DX12, and Metal are loaded via the ShaderPreloader.
                    shouldPreloadCollection = false;
                    SetProgress("");
                }
            } catch(Exception _) { /* ignored */ }
            
            if(shouldPreloadCollection && doShaderVariantCollectionWarmup && preloadShaders != null) {
                SetProgress("Preloading shaders...");
                await UniTask.Yield();
                preloadShaders.WarmUp();
                await UniTask.Yield();
            }

            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(globalScene, LoadSceneMode.Single);
            while(true) {
                if(asyncOperation.isDone) {
                    await UniTask.Yield();
                    SetProgress("Loading subsystems...", "", val: 0.25f);
                    break;
                }
                await UniTask.Yield();
                SetProgress("Loading global scene...", val: asyncOperation.progress * 0.25f);
            }
        }
    }

}
