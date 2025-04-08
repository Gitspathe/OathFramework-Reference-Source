using Cysharp.Threading.Tasks;
using OathFramework.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace OathFramework.Settings
{
    public class ShaderPreloader : Subsystem
    {
        [field: SerializeField] public bool DoPreloading     { get; private set; }
        [field: SerializeField] public MaterialDB MaterialDB { get; private set; }

        [Space(10)]
        
        [SerializeField] private int numLights = 4;
        [SerializeField] private GameObject preloadEnvironmentPrefab;
        
        private GameObject preloadEnvironment;
        private Color[] lightColors;
        private List<GameObject> lights = new();
        private MeshRenderer quadRenderer;
        
        public static ShaderPreloader Instance { get; private set; }

        public override string Name    => "Shader Preloader";
        public override uint LoadOrder => SubsystemLoadOrders.ShaderPreloader;

        public override async UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(ShaderPreloader)} singleton.");
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            if(numLights > 8) {
                numLights = 8;
            }
            lightColors = new[] {
                Color.clear,
                Color.white,
                Color.red,
                Color.green,
                Color.blue,
                Color.grey,
                Color.yellow,
                Color.magenta,
                Color.cyan
            };
            bool shouldPreload = INISettings.GetBool("Performance/PrecompileShaders") != false;
            try {
                GraphicsDeviceType gfxDevice = SystemInfo.graphicsDeviceType;
                if(gfxDevice != GraphicsDeviceType.Vulkan 
                   && gfxDevice != GraphicsDeviceType.Direct3D12 
                   && gfxDevice != GraphicsDeviceType.Metal) { 
                    shouldPreload = false;
                }
            } catch(Exception) { /* ignored */ }
            
            if(shouldPreload && DoPreloading) {
                preloadEnvironment = Instantiate(preloadEnvironmentPrefab);
                GameObject quad    = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quadRenderer       = quad.GetComponent<MeshRenderer>();
                await PreloadShaders(timer);
                Destroy(quad);
                Destroy(preloadEnvironment);
                foreach(GameObject go in lights) {
                    Destroy(go);
                }
                lights.Clear();
            }
        }

        private async UniTask PreloadShaders(Stopwatch timer)
        {
            if(!timer.IsRunning) {
                timer.Start();
            }
            for(int i = 0; i < numLights; i++) {
                await PreloadShaderChunk(i);
            }
        }

        private async UniTask PreloadShaderChunk(int chunkLights)
        {
            foreach(GameObject go in lights) {
                Destroy(go);
            }
            lights.Clear();
            if(chunkLights > 0) {
                for(int i = 1; i < chunkLights + 1; i++) {
                    if(i == 1) {
                        GameObject mainLight = new("Main Light") { transform = { position = new Vector3(0.0f, 0.0f, -1.0f) } };
                        Light mainL          = (Light)mainLight.AddComponent(typeof(Light));
                        mainL.shadows        = LightShadows.Soft;
                        mainL.color          = Color.white;
                        lights.Add(mainLight);
                    }
                    GameObject go = new($"Light {i}") { transform = { position = new Vector3(0.0f, 0.0f, -1.0f) } };
                    Light l       = (Light)go.AddComponent(typeof(Light));
                    l.color       = lightColors[i];
                    l.shadows     = chunkLights % 2 == 0 ? LightShadows.Soft : LightShadows.Hard;
                    lights.Add(go);
                }
            }
            foreach(Material mat in MaterialDB.GetAllMaterials()) {
                quadRenderer.sharedMaterial = mat;
                await UniTask.Yield();
            }
        }
    }
}
