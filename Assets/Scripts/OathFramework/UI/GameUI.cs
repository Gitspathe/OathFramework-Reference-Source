using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Networking;
using OathFramework.UI.Builds;
using OathFramework.UI.Info;
using OathFramework.UI.Platform;
using OathFramework.UI.Settings;
using OathFramework.Utility;
using QFSW.QC;
using System.Diagnostics;
using UnityEngine.InputSystem.UI;
using Debug = UnityEngine.Debug;

namespace OathFramework.UI
{ 

    [RequireComponent(typeof(UICommonMessages))]
    public sealed class GameUI : Subsystem, 
        IInitialized, ILoopUpdate, ILoopLateUpdate,
        ISceneIntegrateEventCompleted
    {
        public GameObject bossHealthBarPanel;
        public Slider bossHealthBar;

        [Space(10)]

        public GameObject playerUI;
        public GameObject settingsButton;
        public GameObject miniChatPanel;
        public GameObject mobileControlsPanel;
        public GameObject leftJoystick;
        public GameObject rightJoystick;
        public InputActionReference openLeaderboardAction;

        [Space(10)]

        public BuildMenuScript playerBuildUI;
        public GameObject consolePrefab;

        [Space(10)]
        
        [SerializeField] private InputActionReference[] alwaysEnabledActions;
        
        private QuantumConsole console;
        private bool consoleOpen;

        private Coroutine comboCoroutine;
        private bool initialized;

        public Entity AttachedBoss             { get; set; }
        public DeathUIScript DeathUI           { get; private set; }
        public SettingsUI SettingsUI           { get; private set; }
        public DamagePopupManager DamagePopups { get; private set; }
        public SpectateUIScript Spectate       { get; private set; }
        public CreditsScript Credits           { get; private set; }
        public LeaderboardUIScript Leaderboard { get; private set; }
        public static bool IsCursorLocked      { get; private set; }
        public bool PlayerControlBlocked => consoleOpen || PauseMenu.IsPaused;

        public override int UpdateOrder => GameUpdateOrder.Finalize;

        public static GameUI Instance { get; private set; }
        
        public override string Name    => "Game UI";
        public override uint LoadOrder => SubsystemLoadOrders.GameUI;
        
        uint ILockableOrderedListElement.Order => 0;

        protected override void Awake()
        {
            base.Awake();
            GameCallbacks.Register((IInitialized)this);
        }

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(GameUI)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);

            GetComponent<UICommonMessages>().Initialize();
            mobileControlsPanel.SetActive(true);
            leftJoystick.GetComponent<Image>().enabled  = false;
            rightJoystick.GetComponent<Image>().enabled = false;
            GetComponentInChildren<MiniPlayerInfoPanel>(true).Initialize();
            GetComponentInChildren<AllyStatusHolder>(true).Initialize();
            GetComponentInChildren<InfoPopup>(true).Initialize();
            GetComponent<UIControlsDatabase>().Initialize();
            GetComponent<UIControlsInputHandler>().Initialize();
            GetComponentInChildren<OnScreenKeyboard>(true).Initialize();
            DeathUI      = GetComponentInChildren<DeathUIScript>(true).Initialize();
            SettingsUI   = GetComponentInChildren<SettingsUI>(true).Initialize();
            Spectate     = GetComponentInChildren<SpectateUIScript>(true).Initialize();
            Credits      = GetComponentInChildren<CreditsScript>(true).Initialize();
            Leaderboard  = GetComponentInChildren<LeaderboardUIScript>(true).Initialize();
            DamagePopups = GetComponent<DamagePopupManager>().Initialize();
            DeathUI.gameObject.SetActive(false);
            SettingsUI.Hide();
            playerBuildUI.Initialize();
            Instance = this;
            return UniTask.CompletedTask;
        }

        public void CreateConsole()
        {
            if(console != null)
                return;
            
            console               = Instantiate(consolePrefab).GetComponent<QuantumConsole>();
            console.OnActivate   += ConsoleOnActivate;
            console.OnDeactivate += ConsoleOnDeactivate;
        }
        
        UniTask IInitialized.OnGameInitialized()
        {
            initialized = true;
            NetGame.Callbacks.Register((ISceneIntegrateEventCompleted)this);
            return UniTask.CompletedTask;
        }

        private void ConsoleOnActivate()
        {
            GameControls.PlayerInput.DeactivateInput();
            FindObjectOfType<InputSystemUIInputModule>().enabled = false; // Have to toggle this to fix a bug with input.
            FindObjectOfType<InputSystemUIInputModule>().enabled = true;
            consoleOpen = true;
        }

        private void ConsoleOnDeactivate()
        {
            if(Game.State == GameState.Quitting)
                return;
            
            GameControls.PlayerInput.ActivateInput();
            consoleOpen = false;
        }

        public void LoopUpdate()
        {
            if(!initialized)
                return;
            
            bossHealthBarPanel.SetActive(AttachedBoss != null);
            if(AttachedBoss != null) {
                bossHealthBar.value = (float)AttachedBoss.CurStats.health / AttachedBoss.CurStats.maxHealth;
            }
        }

        public void LoopLateUpdate()
        {
            if(!initialized || Instance == null)
                return;
            
            if(Game.State == GameState.InGame && !LeaderboardUIScript.OpeningBlocked && openLeaderboardAction.action.WasPerformedThisFrame()) {
                if(LeaderboardUIScript.IsOpen) {
                    LeaderboardUIScript.Close();
                } else {
                    LeaderboardUIScript.Open();
                }
            }
            UpdateCursorState();
        }

        private void OnDestroy()
        {
            Instance = null;
            console.OnActivate   -= ConsoleOnActivate;
            console.OnDeactivate -= ConsoleOnDeactivate;
            NetGame.Callbacks.Unregister((ISceneIntegrateEventCompleted)this);
        }

        public void ShowDeathUI(float timeUntilRespawn)
        {
            DeathUI.Show(timeUntilRespawn);
        }

        public void HideDeathUI()
        {
            DeathUI.Hide();
        }

        private void UpdateCursorState()
        {
            //bool attachedToOwner =
            //    HUDScript.Instance.AttachedPlayer != null
            //    && HUDScript.Instance.AttachedPlayer.IsOwner;

            //bool isDead =
            //    HUDScript.Instance.AttachedPlayer == null
            //    && NetClient.SelfAlive;

            //bool lockCursor =
            //    Game.Instance.State == GameState.InGame
            //    && NetClient.SelfExists
            //    && !LeaderboardUIScript.IsOpen
            //    && (attachedToOwner || isDead);
            
            bool lockCursor = GameControls.UsingController;
            if(lockCursor) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                IsCursorLocked = true;
            } else {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                IsCursorLocked = false;
            }
        }

        public void TickAlwaysEnabledActions()
        {
            foreach(InputActionReference action in alwaysEnabledActions) {
                action.action.Enable();
            }
        }
        
        void ISceneIntegrateEventCompleted.OnSceneIntegrateEventCompleted()
        {
            StopAllCoroutines();
            comboCoroutine = null;
            if(Game.State != GameState.InGame) {
                playerUI.SetActive(false);
                settingsButton.SetActive(false);
                leftJoystick.GetComponent<Image>().enabled  = false;
                rightJoystick.GetComponent<Image>().enabled = false;
                return;
            }

            playerUI.SetActive(true);
            settingsButton.SetActive(true);
            leftJoystick.GetComponent<Image>().enabled  = true;
            rightJoystick.GetComponent<Image>().enabled = true;
        }
    }

}
