using Cysharp.Threading.Tasks;
using OathFramework.Achievements;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using OathFramework.Core.GameEvents;
using OathFramework.Settings;
using OathFramework.Utility;
using System.Threading;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using Event = OathFramework.Core.GameEvents.Event;
using Image = UnityEngine.UI.Image;

#if !UNITY_IOS && !UNITY_ANDROID
using Steamworks;
using OathFramework.Platform.Steam;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

// ReSharper disable once RedundantUsingDirective
using OathFramework.Extensions;

namespace OathFramework.Core
{ 

    public class Game : MonoBehaviour
    {
        [SerializeField] private Image transitionImage;
        
        public string mainMenuScene;
        public string gameScene;
        public string testScene;
        
        [Header("Performance")]
        public int pathfindingIterations = 50;
        public int mobilePhysicsRate     = 30;
        public int desktopPhysicsRate    = 60;

        [Header("Build")]
        public bool supportSteam = true;
        public uint steamAppID;

        private int gcUpdate;

        public static CancellationTokenSource ResetCancellation { get; private set; } = new();
        
        public static bool IsQuitting         => State == GameState.Quitting;
        public static bool ExtendedDebug      => true;
        public static bool DebugGizmos        => true;
        public static bool Initialized        { get; private set; }
        public static bool ConsoleEnabled     { get; private set; }
        public static string[] LaunchArgs     { get; private set; }
        public static AccessToken AccessToken { get; private set; }
        public static Platforms Platform      { get; private set; }
        public static GameState State         { get; private set; }
        public static GameGCMode GCMode       { get; private set; } = GameGCMode.Optimal;
        public static Game Instance           { get; private set; }

        private async void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(Game)} singleton.");
                Destroy(Instance);
                return;
            }

            Instance = this;
            
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnEditorApplicationOnplayModeStateChanged;
#endif
            
#if UNITY_IOS || UNITY_ANDROID
            Platform = Platforms.Mobile;
            Time.fixedDeltaTime = 1.0f / mobilePhysicsRate;
#else
            Platform = Platforms.Desktop;
            Time.fixedDeltaTime = 1.0f / desktopPhysicsRate;
#endif
            
            transitionImage.gameObject.SetActive(true);
            transitionImage.color = Color.black;
            
            DontDestroyOnLoad(gameObject);
            FileIO.Initialize();
            await LoadAsync();
            GetComponent<IGame>()?.Initialize();
        }

        public static void FireResetCancellation()
        {
            ResetCancellation?.Cancel();
            ResetCancellation?.Dispose();
            ResetCancellation = new CancellationTokenSource();
        }

        private void OnApplicationQuit()
        {
            FireResetCancellation();
            State = GameState.Quitting;
            if(NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnEditorApplicationOnplayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private static void OnEditorApplicationOnplayModeStateChanged(PlayModeStateChange change)
        {
            if(change == PlayModeStateChange.ExitingPlayMode) {
                FireResetCancellation();
                State = GameState.Quitting;
            }
            if(change == PlayModeStateChange.ExitingPlayMode) {
                Debug.Log("Forcing network port release...");
                NetworkManager networkManager = FindObjectOfType<NetworkManager>();
                if(networkManager != null && networkManager.IsServer) {
                    networkManager.Shutdown();
                }
            }
        }
#endif

        public static void LoadLaunchArgs()
        {
            LaunchArgs = Environment.GetCommandLineArgs();
            
#if DEBUG
            ConsoleEnabled = true;
#else
            ConsoleEnabled = LaunchArgs.Contains("-enable-console");
#endif
        }

        private async UniTask LoadAsync()
        {
            AccessToken = GameCallbacks.Access.GenerateAccessToken();

            INISettings.Load();
            Debug.Log("Initializing game.");
            SetState(GameState.Preload);
            SetInitialINIOptions();
            await UniTask.Yield();
            await SubsystemManager.PreInitialize();
            await SubsystemManager.Initialize();
            await SubsystemManager.PostInitialize();
            Debug.Log("initialization complete.");
            
#if !UNITY_IOS && !UNITY_ANDROID
            if(supportSteam) {
                Debug.Log("Initializing Steam.");
                try {
                    SteamClient.Init(steamAppID, false);
                    SteamFriends.OnGameOverlayActivated += SteamOnOnGameOverlayActivated;
                } catch(Exception e) {
                    Debug.LogError($"Exception during Steam init: {e}");
                }
                // Callbacks are run by FacepunchTransport.
            }
            await SupporterDLCUtil.Init();
#endif
            
            await SettingsManager.Instance.ApplyInitialSettings();
            NavMesh.pathfindingIterationsPerFrame = pathfindingIterations;
            Preloader.SetProgress("Loading menu...", "", val: 0.75f);
            await UniTask.Yield();

            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(mainMenuScene, LoadSceneMode.Single);
            if(asyncOperation == null) {
                Debug.LogError("FATAL ERROR: Could not load main menu scene.");
                Application.Quit();
                return;
            }
            
            while(true) {
                if(asyncOperation.isDone)
                    break;
            
                Preloader.SetProgress(val: 0.75f + (asyncOperation.progress * 0.25f));
                await UniTask.Yield();
            }

            Preloader.SetProgress("Running GC...", "", 1.0f);
            await UniTask.Yield();
            if(Preloader.Instance != null) {
                Destroy(Preloader.Instance.gameObject);
            }
            Resources.UnloadUnusedAssets();
            GC.Collect();
            await UniTask.Yield();
            Initialized = true;
            
            SetState(GameState.MainMenu);
            await GameCallbacks.Access.OnGameInitialized(AccessToken);
            EventManager.Activate(
                Event.Type.ScreenTransition, 
                new ScreenTransitionParams(ScreenTransitionOrder.FadeIn, 0.0f, 0.5f, 2.5f)
            );
        }

#if !UNITY_IOS && !UNITY_ANDROID
        private void SteamOnOnGameOverlayActivated(bool state)
        {
            InputSystemUIInputModule iMod = FindObjectOfType<InputSystemUIInputModule>();
            if(iMod != null) {
                iMod.enabled = !state;
            }
            PlayerInput pInput = FindObjectOfType<PlayerInput>();
            if(pInput != null) {
                pInput.enabled = !state;
            }
        }
#endif

        private static void SetInitialINIOptions()
        {
            if(INISettings.GetNumeric("Physics/PhysicsTickRate", out int val)) {
                Time.fixedDeltaTime = 1.0f / Mathf.Clamp(val, 0, 120);
            }
            if(INISettings.GetString("Performance/GCMode", out string sVal)) {
                switch(sVal) {
                    case "aggressive": {
                        GCMode = GameGCMode.Aggressive;
                    } break;
                    case "moderate": {
                        GCMode = GameGCMode.Moderate;
                    } break;
                    
                    case "optimal":
                    default: {
                        GCMode = GameGCMode.Optimal;
                    } break;
                }
            }
            if(INISettings.GetNumeric("Performance/JobWorkerThreadCount", out val)) {
                JobsUtility.JobWorkerCount = val;
            }
        }

        private void Update()
        {
            // if(Keyboard.current.pKey.wasPressedThisFrame) {
            //     Time.timeScale                                                        = 0.0f;
            //     NetClient.Self.PlayerController.enabled                               = false;
            //     NetClient.Self.PlayerController.GetComponent<PlayerHandler>().enabled = false;
            //     FindObjectOfType<CameraController>().enabled                          = false;
            // }
            
            ProcessGC();
            if(Initialized)
                return;

            Preloader.SetProgress(subTaskText: SubsystemManager.CurrentSubTask, val: 0.25f + (0.5f * SubsystemManager.Progress));
        }

        private void ProcessGC()
        {
            switch(GCMode) {
                case GameGCMode.Aggressive: {
                    if(gcUpdate++ > 10) {
                        GC.Collect();
                        gcUpdate = 0;
                    }
                } break;
                case GameGCMode.Moderate: {
                    if(gcUpdate++ > 300) {
                        GC.Collect();
                        gcUpdate = 0;
                    }
                } break;

                case GameGCMode.Optimal: 
                default: { 
                    break;
                }
            }
        }

        public static void SetState(GameState state)
        {
            State = state;
        }

        public static void Quit()
        {
            Application.Quit();
        }
    }

    public enum GameGCMode
    {
        Aggressive,
        Moderate,
        Optimal
    }

    public enum GameState
    {
        Preload,
        MainMenu,
        Lobby,
        InGame,
        Quitting
    }

    public enum Platforms
    {
        Desktop = 0,
        Mobile  = 1
    }

    public interface IGame
    {
        void Initialize();
    }

}
