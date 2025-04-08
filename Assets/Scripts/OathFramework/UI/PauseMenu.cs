using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.UI.Platform;
using OathFramework.UI.Settings;
using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace OathFramework.UI
{
    public class PauseMenu : LoopComponent, 
        IResetGameStateCallback, ILoopUpdate
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private LocalizedString quitTitleStr;
        [SerializeField] private LocalizedString quitMessageStr;

        private Tween currentTween;
        private CanvasGroup mainCanvasGroup;
        private bool transitionActive;
        private ModalConfig exitModal;
        
        public static bool PauseBlocked  => Game.State != GameState.InGame || LeaderboardUIScript.IsOpen;
        public static bool IsPaused      { get; private set; }
        public static PauseMenu Instance { get; private set; }
        
        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(PauseMenu)} singletons.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            mainCanvasGroup = mainPanel.GetComponent<CanvasGroup>();
            GameCallbacks.Register((IResetGameStateCallback)this);
        }

        void ILoopUpdate.LoopUpdate()
        {
            if((IsPaused && !mainPanel.activeSelf) || PauseBlocked)
                return;

            if(IsPaused && (UIControlsInputHandler.BackAction.WasPressedThisFrame() || UIControlsInputHandler.ResumeAction.WasPressedThisFrame())) {
                Resume();
                return;
            } 
            if(!IsPaused && (UIControlsInputHandler.PauseAction.WasPressedThisFrame())) {
                Pause();
            }
        }

        public void ClickedSettings()
        {
            if(!IsPaused || SettingsUI.TransitionActive)
                return;
            
            _ = ClickedSettingsTask();
        }

        public void ClickedQuit()
        {
            if(!IsPaused)
                return;

            exitModal = ModalConfig.Retrieve()
                .WithTitle(quitTitleStr)
                .WithText(quitMessageStr)
                .WithPriority(ModalPriority.Critical)
                .WithButtons(new List<(LocalizedString, Action)> { 
                    (UICommonMessages.Yes, () => _ = QuitTask()), 
                    (UICommonMessages.No,  () => exitModal = null) 
                })
                .WithInitButton(1)
                .Show();
        }

        public void Pause()
        {
            if(transitionActive)
                return;
            
            IsPaused = true;
            _ = ShowMainPanelAnim();
        }

        public void Resume()
        {
            if(transitionActive)
                return;
            
            IsPaused = false;
            _ = HideMainPanelAnim();
        }

        private async UniTask ClickedSettingsTask()
        {
            transitionActive = true;
            await HideMainPanelAnim();
            await SettingsUI.Instance.Show(SettingsUI.SettingsMenuMode.InGame);
            transitionActive = false;
        }

        private async UniTask QuitTask()
        {
            transitionActive = true;
            await HideMainPanelAnim();
            NetGame.OnQuit();
            transitionActive = false;
            IsPaused         = false;
        }
        
        public async UniTask ShowMainPanelAnim()
        {
            if(currentTween.isAlive) {
                currentTween.Stop();
            }

            transitionActive = true;
            mainPanel.gameObject.SetActive(true);
            mainPanel.GetComponent<UITweenCallbackController>().Show();
            currentTween = Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Medium, (t, val) => t.mainCanvasGroup.alpha = val);
            await currentTween;
            transitionActive = false;
        }

        public async UniTask HideMainPanelAnim()
        {
            if(currentTween.isAlive) {
                currentTween.Stop();
            }

            transitionActive = true;
            mainPanel.GetComponent<UITweenCallbackController>().Hide();
            currentTween = Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Medium, (t, val) => t.mainCanvasGroup.alpha = val);
            await currentTween;
            mainPanel.gameObject.SetActive(false);
            transitionActive = false;
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            exitModal?.Close();
            mainPanel.gameObject.SetActive(false);
            transitionActive = false;
            IsPaused         = false;
        }
    }
}
