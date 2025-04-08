using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.UI.Platform;
using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace OathFramework.UI.Settings
{

    public class SettingsUI : LoopComponent, 
        ILoopLateUpdate, IControlSchemeChangedCallback, IResetGameStateCallback
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        [SerializeField] private Button gameButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button audioButton;
        [SerializeField] private Button graphicsButton;

        [Space(10)] 
        
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject graphicsPanel;

        [Space(10)]
        
        [SerializeField] private LocalizedString unsavedChangesStr;
        
        private bool init;
        private CanvasGroup mainCanvasGroup;
        private UINavigationGroup tabNavGroup;
        private UIDropdownParent dropdownParent;
        private ModalConfig confirmExitModal;
        private UINavPin uiNavPin;
        private SettingsMenuMode mode;

        public static bool IsOpen => Instance.mainPanel.activeInHierarchy;
        public static bool TransitionActive                 { get; private set; }
        public static SettingsMenuSubPanel CurrentSubPanel  { get; private set; }
        public static GameSettingsUI GameSettingsUI         { get; private set; }
        public static ControlsSettingsUI ControlsSettingsUI { get; private set; }
        public static AudioSettingsUI AudioSettingsUI       { get; private set; }
        public static GfxSettingsUI GfxSettingsUI           { get; private set; }
        public static SettingsUI Instance                   { get; private set; }

        public SettingsUI Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(SettingsUI)} singleton.");
                Destroy(Instance);
                return null;
            }

            GameSettingsUI     = GetComponent<GameSettingsUI>().Initialize();
            ControlsSettingsUI = GetComponent<ControlsSettingsUI>().Initialize();
            AudioSettingsUI    = GetComponent<AudioSettingsUI>().Initialize();
            GfxSettingsUI      = GetComponent<GfxSettingsUI>().Initialize();
            tabNavGroup        = GetComponent<UINavigationGroup>();
            dropdownParent     = GetComponent<UIDropdownParent>();
            uiNavPin           = mainPanel.GetComponent<UINavPin>();
            mainCanvasGroup    = mainPanel.GetComponent<CanvasGroup>();
            GameControlsCallbacks.Register((IControlSchemeChangedCallback)this);
            GameCallbacks.Register((IResetGameStateCallback)this);
            ShowGame();
            Instance = this;
            init     = true;
            return this;
        }

        public void RebindManagerSettings()
        {
            GameSettingsUI.Instance.RebindManagerSettings();
            GfxSettingsUI.Instance.RebindManagerSettings();
        }
        
        void IControlSchemeChangedCallback.OnControlSchemeChanged(ControlSchemes controlScheme)
        {
            tabNavGroup.SetNavigation(!GameControls.UsingController);
        }

        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(!init || !uiNavPin.IsPinned || ControlsSettingsUI.IsRebinding)
                return;
            
            HandleControls();
        }

        private void HandleControls()
        {
            if(UIControlsInputHandler.NextTabAction.WasPressedThisFrame()) {
                HandleTabChange(NavDirection.Forward);
                return;
            }
            if(UIControlsInputHandler.PrevTabAction.WasPressedThisFrame()) { 
                HandleTabChange(NavDirection.Back);
                return;
            }
            if(UIControlsInputHandler.BackAction.WasPressedThisFrame()) {
                HandleExit();
            }
        }

        private void HandleTabChange(NavDirection dir)
        {
            SettingsMenuSubPanel next;
            if(dir == NavDirection.Forward && CurrentSubPanel == SettingsMenuSubPanel.Graphics) {
                next = SettingsMenuSubPanel.Game;
            } else if(dir == NavDirection.Back && CurrentSubPanel == SettingsMenuSubPanel.Game) {
                next = SettingsMenuSubPanel.Graphics;
            } else {
                int curInt = (int)CurrentSubPanel;
                next = dir == NavDirection.Back ? (SettingsMenuSubPanel)(curInt - 1) : (SettingsMenuSubPanel)(curInt + 1);
            }

            if(CurrentSubPanel == SettingsMenuSubPanel.Graphics && GfxSettingsUI.PendingChanges) {
                ShowDiscardGfxChanges(dir == NavDirection.Forward ? GfxDiscardType.TabGame : GfxDiscardType.TabAudio);
                return;
            }
            ShowSubPanel(next);
        }

        private void HandleExit()
        {
            if(dropdownParent.CurrentDropdown != null) {
                dropdownParent.Close();
                return;
            }
            if(CurrentSubPanel == SettingsMenuSubPanel.Graphics && GfxSettingsUI.PendingChanges) {
                ShowDiscardGfxChanges(GfxDiscardType.None);
                return;
            }
            Hide();
        }

        public async UniTask Show(SettingsMenuMode mode)
        {
            if(TransitionActive)
                return;
            
            this.mode = mode;
            await ShowAnim();
        }

        public void Hide()
        {
            if(TransitionActive)
                return;
            
            switch(mode) { 
                case SettingsMenuMode.MainMenu:
                    _ = ExitToMainMenuTask();
                    break;
                case SettingsMenuMode.InGame:
                    _ = ExitToPauseMenuTask();
                    break;
            }
        }
        
        public void ShowSubPanel(SettingsMenuSubPanel panel)
        {
            switch(panel) {
                case SettingsMenuSubPanel.Game:
                    ShowGame();
                    return;
                case SettingsMenuSubPanel.Controls:
                    ShowControls();
                    return;
                case SettingsMenuSubPanel.Audio:
                    ShowAudio();
                    return;
                case SettingsMenuSubPanel.Graphics:
                    ShowGraphics();
                    return;
                
                case SettingsMenuSubPanel.None:
                default:
                    CurrentSubPanel = SettingsMenuSubPanel.None;
                    Hide();
                    return;
            }
        }

        public void ShowGame()
        {
            if(CurrentSubPanel == SettingsMenuSubPanel.Graphics && GfxSettingsUI.PendingChanges) {
                ShowDiscardGfxChanges(GfxDiscardType.TabGame);
                return;
            }
            CurrentSubPanel             = SettingsMenuSubPanel.Game;
            gameButton.interactable     = false;
            controlsButton.interactable = true;
            audioButton.interactable    = true;
            graphicsButton.interactable = true;
            gamePanel.SetActive(true);
            controlsPanel.SetActive(false);
            audioPanel.SetActive(false);
            graphicsPanel.SetActive(false);
            GameSettingsUI.UpdateGameSettingsUI();
        }

        public void ShowControls()
        {
            if(CurrentSubPanel == SettingsMenuSubPanel.Graphics && GfxSettingsUI.PendingChanges) {
                ShowDiscardGfxChanges(GfxDiscardType.TabControls);
                return;
            }
            CurrentSubPanel             = SettingsMenuSubPanel.Controls;
            gameButton.interactable     = true;
            controlsButton.interactable = false;
            audioButton.interactable    = true;
            graphicsButton.interactable = true;
            gamePanel.SetActive(false);
            controlsPanel.SetActive(true);
            audioPanel.SetActive(false);
            graphicsPanel.SetActive(false);
            ControlsSettingsUI.Tick();
        }

        public void ShowAudio()
        {
            if(CurrentSubPanel == SettingsMenuSubPanel.Graphics && GfxSettingsUI.PendingChanges) {
                ShowDiscardGfxChanges(GfxDiscardType.TabAudio);
                return;
            }
            CurrentSubPanel             = SettingsMenuSubPanel.Audio;
            gameButton.interactable     = true;
            controlsButton.interactable = true;
            audioButton.interactable    = false;
            graphicsButton.interactable = true;
            gamePanel.SetActive(false);
            controlsPanel.SetActive(false);
            audioPanel.SetActive(true);
            graphicsPanel.SetActive(false);
            AudioSettingsUI.Tick();
        }

        public void ShowGraphics()
        {
            CurrentSubPanel             = SettingsMenuSubPanel.Graphics;
            gameButton.interactable     = true;
            controlsButton.interactable = true;
            audioButton.interactable    = true;
            graphicsButton.interactable = false;
            gamePanel.SetActive(false);
            controlsPanel.SetActive(false);
            audioPanel.SetActive(false);
            graphicsPanel.SetActive(true);
            GfxSettingsUI.UpdateGraphicsUI();
        }
        
        private void ShowDiscardGfxChanges(GfxDiscardType discardType)
        {
            confirmExitModal = ModalConfig.Retrieve()
                .WithText(unsavedChangesStr)
                .WithButtons(new List<(LocalizedString, Action)> { 
                    (UICommonMessages.Yes, () => {
                        GfxSettingsUI.Instance.RevertChanges();
                        ExitGfx(discardType);
                    }), 
                    (UICommonMessages.No,  () => confirmExitModal = null) 
                }).WithInitButton(1).WithSelectLast(true).Show();
        }
        
        private void ExitGfx(GfxDiscardType discardType)
        {
            switch(discardType) {
                case GfxDiscardType.None:
                    Hide();
                    break;
                case GfxDiscardType.TabAudio:
                    ShowSubPanel(SettingsMenuSubPanel.Audio);
                    break;
                case GfxDiscardType.TabGame:
                    ShowSubPanel(SettingsMenuSubPanel.Game);
                    break;
                case GfxDiscardType.TabControls:
                    ShowSubPanel(SettingsMenuSubPanel.Controls);
                    break;
            }
        }

        public async UniTask ExitToMainMenuTask()
        {
            await HideAnim();
            await MenuUI.Instance.ShowMainPanelAnim();
        }

        public async UniTask ExitToPauseMenuTask()
        {
            await HideAnim();
            await PauseMenu.Instance.ShowMainPanelAnim();
        }
        
        public async UniTask ShowAnim()
        {
            TransitionActive = true;
            mainPanel.gameObject.SetActive(true);
            await Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Medium, (t, val) => t.mainCanvasGroup.alpha = val);
            TransitionActive = false;
        }

        public async UniTask HideAnim()
        {
            TransitionActive = true;
            await Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Fast, (t, val) => t.mainCanvasGroup.alpha = val);
            mainPanel.gameObject.SetActive(false);
            TransitionActive = false;
        }
        
        void IResetGameStateCallback.OnResetGameState()
        {
            mainPanel.gameObject.SetActive(false);
            confirmExitModal?.Close();
            confirmExitModal = null;
        }

        public enum SettingsMenuMode
        {
            None     = 0,
            MainMenu = 1,
            InGame   = 2
        }

        public enum SettingsMenuSubPanel
        {
            None     = 0,
            Game     = 1,
            Controls = 2,
            Audio    = 3,
            Graphics = 4
        }

        private enum NavDirection
        {
            Forward, 
            Back
        }
        
        private enum GfxDiscardType
        {
            None        = 0,
            TabGame     = 1,
            TabControls = 2,
            TabAudio    = 3
        }
    }

}
