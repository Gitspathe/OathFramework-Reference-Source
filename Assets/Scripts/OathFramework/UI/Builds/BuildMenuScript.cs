using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.Progression;
using OathFramework.UI.Info;
using OathFramework.UI.Platform;
using OathFramework.Utility;
using PrimeTween;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace OathFramework.UI.Builds
{

    [RequireComponent(typeof(CharacterMenuScript), typeof(StatsMenuScript), typeof(EquipmentMenuScript))]
    public class BuildMenuScript : LoopComponent, 
        ILoopUpdate, IInitialized, ILoadLevelCompleted, 
        IResetGameStateCallback
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        private BuildMenuMode mode;
        private RectTransform panelTransform;
        private CanvasGroup panelCanvasGroup;
        private ModalConfig confirmExitModal;
        private bool initialized;
        
        [SerializeField] private GameObject panel;
        [SerializeField] private UINavPin uiNavPin;
        [SerializeField] private Button CharacterTabBtn;
        [SerializeField] private Button EquipmentTabBtn;
        [SerializeField] private TextMeshProUGUI rightPanelTitle;
        [SerializeField] private Transform statsView;
        [SerializeField] private DetailsView detailsView;
        [SerializeField] private Toggle statsToggle;
        [SerializeField] private Toggle detailsToggle;

        [Space(10)] 
        
        [SerializeField] private RectTransform defaultTransform;
        [SerializeField] private RectTransform miniTransform;

        [Space(10)]
        
        [SerializeField] private LocalizedString statsSubtitlesStr;
        [SerializeField] private LocalizedString detailsSubtitleStr;
        [SerializeField] private LocalizedString unsavedChangesStr;
        
        [field: Space(10)]
        
        [field: SerializeField] public CharacterMenuScript Character { get; private set; }
        [field: SerializeField] public EquipmentMenuScript Equipment { get; private set; }
        [field: SerializeField] public StatsMenuScript Stats         { get; private set; }

        public static PlayerBuildData CurBuildData;
        public static PlayerBuildData OldBuildData;
        public static PlayerProfile Profile            => ProgressionManager.Profile;
        public static bool BuildDataChanged            => !CurBuildData.Equals(OldBuildData);
        public static RightPanelMode CurRightPanelMode { get; private set; }
        public static BuildMenuScript Instance         { get; private set; }
        
        uint ILockableOrderedListElement.Order => 20;
        
        public BuildMenuScript Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(BuildMenuScript)} singleton.");
                Destroy(Instance);
                return null;
            }

            Instance         = this;
            panelTransform   = panel.GetComponent<RectTransform>();
            panelCanvasGroup = panel.GetComponent<CanvasGroup>();
            detailsView.Initialize();
            GameCallbacks.Register((IResetGameStateCallback)this);
            GameCallbacks.Register((IInitialized)this);
            GameCallbacks.Register((ILoadLevelCompleted)this);
            LocalizationSettings.SelectedLocaleChanged += OnLocalizationChanged;
            return Instance;
        }

        private void OnLocalizationChanged(Locale newLocale)
        {
            if(Instance == null)
                return;
            
            TickStats();
        }

        UniTask IInitialized.OnGameInitialized()
        {
            initialized  = true;
            CurBuildData = Profile.CurrentLoadout;
            OldBuildData = Profile.CurrentLoadout;
            Character    = GetComponent<CharacterMenuScript>().Initialize();
            Equipment    = GetComponent<EquipmentMenuScript>().Initialize();
            Stats        = GetComponent<StatsMenuScript>().Initialize();
            Character.Tick();
            ShowCharacter();
            return UniTask.CompletedTask;
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            if(!initialized || !uiNavPin.IsPinned)
                return;
            
            HandleControls();
        }

        private void HandleControls()
        {
            // if(BuildDataChanged && Character.IsVisible && UIControlsInputHandler.SubmitAction.WasPressedThisFrame()) {
            //     ApplyChanges();
            //     return;
            // }
            if(UIControlsInputHandler.ToggleInfoAction.WasPressedThisFrame()) {
                ToggleRightPanelMode();
                return;
            }
            if(UIControlsInputHandler.PrevTabAction.WasPressedThisFrame() || UIControlsInputHandler.NextTabAction.WasPressedThisFrame()) {
                if(Character.isActiveAndEnabled && BuildDataChanged) {
                    ShowDiscardChanges(DiscardType.Tab);
                    return;
                }
                ToggleTab();
                return;
            }
            if(UIControlsInputHandler.BackAction.WasPressedThisFrame()) {
                HandleExit();
            }
        }
        
        void ILoadLevelCompleted.OnLevelLoaded()
        {
            Hide();
        }

        public async UniTask Show(BuildMenuMode mode)
        {
            ProgressionManager.Profile.showLoadoutPopup = false;
            _ = ProgressionManager.Profile.Save();
            
            this.mode = mode;
            switch(mode) {
                case BuildMenuMode.MainMenu: {
                    panelTransform.position  = defaultTransform.position;
                    panelTransform.sizeDelta = defaultTransform.sizeDelta;
                    await ShowAnim();
                } break;
                case BuildMenuMode.Lobby: {
                    panelTransform.position  = miniTransform.position;
                    panelTransform.sizeDelta = miniTransform.sizeDelta;
                    await MenuUI.Instance.LobbyUI.HideLobbyAnim();
                    await UniTask.WhenAll(ShowAnim(), MiniPlayerInfoPanel.Instance.Show());
                } break;
                case BuildMenuMode.InGame: {
                    panelTransform.position  = defaultTransform.position;
                    panelTransform.sizeDelta = defaultTransform.sizeDelta;
                    await ShowAnim();
                } break;
                
                case BuildMenuMode.None:
                default:
                    break;
            }
        }

        public void Hide()
        {
            if(mode == BuildMenuMode.InGame) {
                SpectateUIScript.Instance.ExitedLoadout();
            }
            confirmExitModal?.Close();
            confirmExitModal = null;
            if(BuildDataChanged) {
                DiscardChanges();
            }
            switch(mode) {
                case BuildMenuMode.MainMenu:
                    _ = ExitToMainMenuTask();
                    break;
                case BuildMenuMode.Lobby:
                    _ = ExitToLobbyTask();
                    break;
                case BuildMenuMode.InGame:
                    _ = HideAnim();
                    break;

                case BuildMenuMode.None:
                default:
                    break;
            }
        }

        public static void SetRightPanelDetails(UIItemSlotBase slot)
        {
            UIEquipmentSlot selected = Instance.Equipment.CurSelected;
            string dataKey           = slot == null ? selected.ValueKey : slot.ValueKey;
            EquipSlotType slotType   = slot == null ? selected.Type : slot.SlotType;
            if(slot == null) {
                Instance.detailsView.Clear();
                if(selected == null)
                    return;
            }
            string comparisonKey = slotType == EquipSlotType.Equippable 
                && selected != null 
                && selected.Type == EquipSlotType.Equippable ? selected.ValueKey : null;
            
            Instance.detailsView.SetupDetailsEquipSlot(slotType, dataKey, comparisonKey);
        }
        
        public void ShowCharacter()
        {
            Equipment.Hide();
            Character.Show();
            CharacterTabBtn.interactable = false;
            EquipmentTabBtn.interactable = true;
            SetRightPanelMode(RightPanelMode.Stats);
        }

        public void ShowEquipment()
        {
            Character.Hide();
            Equipment.Show();
            CharacterTabBtn.interactable = true;
            EquipmentTabBtn.interactable = false;
            SetRightPanelMode(RightPanelMode.Details);
        }

        public void HandleExit()
        {
            if(BuildDataChanged) {
                ShowDiscardChanges(DiscardType.None);
                return;
            }
            Exit(DiscardType.None);
        }

        private void ShowDiscardChanges(DiscardType discardType)
        {
            confirmExitModal = ModalConfig.Retrieve()
                .WithText(unsavedChangesStr)
                .WithButtons(new List<(LocalizedString, Action)> { 
                    (UICommonMessages.Yes, () => {
                        DiscardChanges();
                        Exit(discardType);
                    }), 
                    (UICommonMessages.No,  () => confirmExitModal = null) 
                }).WithInitButton(1).WithSelectLast(true).Show();
        }

        private void Exit(DiscardType discardType)
        {
            switch(discardType) {
                case DiscardType.None:
                    if(Equipment.CurSelected != null && Equipment.IsVisible) {
                        Equipment.CloseSubPanel();
                        return;
                    }
                    Hide();
                    break;
                case DiscardType.Tab:
                    ToggleTab();
                    break;
            }
        }

        public void ToggleTab()
        {
            if(Character.IsVisible) {
                ShowEquipment();
            } else {
                ShowCharacter();
            }
        }

        public void ToggleRightPanelMode()
        {
            SetRightPanelMode(CurRightPanelMode == RightPanelMode.Details ? RightPanelMode.Stats : RightPanelMode.Details);
        }

        public void RightPanelToggleChanged()
        {
            if(statsToggle.isOn) {
                SetRightPanelMode(RightPanelMode.Stats);
                return;
            }
            if(detailsToggle.isOn) {
                SetRightPanelMode(RightPanelMode.Details);
            }
        }

        public static void SetRightPanelMode(RightPanelMode mode)
        {
            string title;
            CurRightPanelMode = mode;
            Instance.statsToggle.SetIsOnWithoutNotify(mode == RightPanelMode.Stats);
            Instance.detailsToggle.SetIsOnWithoutNotify(mode == RightPanelMode.Details);
            Instance.statsView.gameObject.SetActive(false);
            Instance.detailsView.gameObject.SetActive(false);
            switch(mode) {
                case RightPanelMode.Stats: {
                    Instance.statsView.gameObject.SetActive(true);
                    title = Instance.statsSubtitlesStr.GetLocalizedString();
                } break;
                case RightPanelMode.Details: {
                    Instance.detailsView.gameObject.SetActive(true);
                    title = Instance.detailsSubtitleStr.GetLocalizedString();
                } break;

                case RightPanelMode.None: 
                default: {
                    title = "";
                } break;
            }

            if(!string.IsNullOrEmpty(title)) {
                Instance.rightPanelTitle.text = title;
            }
        }

        public static void TickStats()
        {
            Instance.Stats.Tick(ref CurBuildData);
        }

        public static void ApplyChanges()
        {
            OldBuildData = CurBuildData;
            Profile.UpdateLoadout(Profile.loadoutIndex, CurBuildData);
            Instance.Character.Tick();
            Instance.Equipment.UpdateSlots();
            TickStats();
            if(NetClient.SelfExists) {
                NetClient.Self.Data.SetBuild(Profile.CurrentLoadout);
            }
            if(Instance.Character.IsVisible && GameControls.UsingController) {
                Instance.Character.Hide();
                Instance.Character.Show();
            }
        }

        public static void DiscardChanges()
        {
            CurBuildData = OldBuildData;
            Instance.Character.Tick();
            TickStats();
            if(GameControls.UsingController) {
                Instance.Character.Hide();
                Instance.Character.Show();
            }
        }
        
        public async UniTask ExitToMainMenuTask()
        {
            await UniTask.WhenAll(HideAnim(), MiniPlayerInfoPanel.Instance.Hide());
            if(MenuUI.Instance == null)
                return;
            
            await MenuUI.Instance.ShowMainPanelAnim();
        }

        public async UniTask ExitToLobbyTask()
        {
            await UniTask.WhenAll(HideAnim(), MiniPlayerInfoPanel.Instance.Hide());
            if(MenuUI.Instance == null)
                return;
            
            await MenuUI.Instance.LobbyUI.ShowLobbyAnim();
        }
        
        private async UniTask ShowAnim()
        {
            panel.gameObject.SetActive(true);
            await Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Medium, (t, val) => t.panelCanvasGroup.alpha = val);
        }

        public async UniTask HideAnim()
        {
            await Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Fast, (t, val) => t.panelCanvasGroup.alpha = val);
            panel.gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocalizationChanged;
        }
        
        void IResetGameStateCallback.OnResetGameState()
        {
            panel.gameObject.SetActive(false);
        }

        public enum RightPanelMode
        {
            None    = 0,
            Stats   = 1,
            Details = 2
        }

        public enum BuildMenuMode
        {
            None     = 0,
            MainMenu = 1,
            Lobby    = 2,
            InGame   = 3
        }

        private enum DiscardType
        {
            None = 0,
            Tab  = 1,
        }
    }

}
