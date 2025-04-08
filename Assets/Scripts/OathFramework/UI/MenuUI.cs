using PrimeTween;
using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Core.Service;
using OathFramework.Networking;
using OathFramework.UI.Builds;
using OathFramework.UI.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Video;

namespace OathFramework.UI
{ 

    public class MenuUI : MonoBehaviour, 
        IResetGameStateCallback
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Transform playPanel;
        [SerializeField] private Transform multiplayerJoinPanel;

        [Space(10)] 
        
        [SerializeField] private Transform singlePlayerPanel;
        [SerializeField] private Transform multiplayerPanel;
        [SerializeField] private Transform lobbyCodePanel;

        [Space(10)]
        
        [SerializeField] private LocalizedString leaveLobbyConfirmationMsg;
        [SerializeField] private LocalizedString leaveLobbyYesMsg;
        [SerializeField] private LocalizedString leaveLobbyNoMsg;

        public Transform onlineTransform;
        public TextMeshProUGUI lobbiesText;
        public TextMeshProUGUI playersText;

        public string lobbyCountString  = "MULTIPLAYER LOBBIES";
        public string playerCountString = "ONLINE PLAYERS";

        private bool enableFlag;
        private float timeUntilCountUpdate;
        private Task playerCountTask;

        private ModalConfig leaveLobbyModal;
        private CanvasGroup mainCanvasGroup;
        private CanvasGroup singlePlayerCanvasGroup;
        private CanvasGroup multiplayerCanvasGroup;
        private CanvasGroup lobbyCanvasGroup;
        
        [field: Space(10)]
        [field: SerializeField] public LobbyUIScript LobbyUI { get; private set; }
        [field: SerializeField] public GameObject LobbyPanel { get; private set; }
        [field: SerializeField] public GameObject MainPanel  { get; private set; }

        public static MenuUI Instance { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(MenuUI)} singleton.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            mainCanvasGroup         = MainPanel.GetComponent<CanvasGroup>();
            singlePlayerCanvasGroup = singlePlayerPanel.GetComponent<CanvasGroup>();
            multiplayerCanvasGroup  = multiplayerPanel.GetComponent<CanvasGroup>();
            lobbyCanvasGroup        = lobbyCodePanel.GetComponent<CanvasGroup>();
            GameCallbacks.Register((IResetGameStateCallback)this);
        }
        
        private void OnEnable()
        {
            onlineTransform.gameObject.SetActive(false);
            LobbyUI.RegisterInfoHolder();
#if !UNITY_IOS && !UNITY_ANDROID
            if(NetGame.Instance.UseSteam) {
                _ = UpdatePlayersCoroutine();
            }
            timeUntilCountUpdate = GameServices.PlayerCount.UpdateInterval;
#endif
        }

        private void LateUpdate()
        {
#if !UNITY_IOS && !UNITY_ANDROID
            timeUntilCountUpdate -= Time.unscaledDeltaTime;
            if(timeUntilCountUpdate <= 0.0f && NetGame.Instance.UseSteam) {
                _ = UpdatePlayersCoroutine();
                timeUntilCountUpdate = GameServices.PlayerCount.UpdateInterval;
            }
#endif

            if(enableFlag) {
                onlineTransform.gameObject.SetActive(true);
                lobbiesText.text = $"{GameServices.PlayerCount.LobbyCount} {lobbyCountString}";
                playersText.text = $"{GameServices.PlayerCount.PlayerCount} {playerCountString}";
            }
        }

        private void OnDestroy()
        {
            GameCallbacks.Unregister((IResetGameStateCallback)this);
            if(videoPlayer != null) {
                videoPlayer.time = 0.0;
            }
            leaveLobbyModal?.Close();
            leaveLobbyModal = null;
            Instance        = null;
        }

#if !UNITY_IOS && !UNITY_ANDROID
        public async UniTask AwaitPlayerCountTask()
        {
            if(playerCountTask == null || playerCountTask.IsCompleted)
                await UniTask.Yield();

            while(playerCountTask != null && !playerCountTask.IsCompleted)
                await UniTask.Yield();
        }

        private async UniTask UpdatePlayersCoroutine()
        {
            await UniTask.WaitForSeconds(1.0f);
            playerCountTask = Task.Run(async () => await GameServices.PlayerCount.UpdatePlayerCount(() => {
                enableFlag = true;
            }));
        }
#endif

        public void ClickedMakeGameSnapshotTest()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;

            _ = NetGame.Instance.StartMultiplayerHost("snapshot");
        }

        public void ClickedLeaveLobby()
        {
            leaveLobbyModal = ModalConfig.Retrieve()
                .WithText(leaveLobbyConfirmationMsg)
                .WithButtons(new List<(LocalizedString, Action)> {
                    (leaveLobbyYesMsg, () => _ = LeaveLobbyTask()), 
                    (leaveLobbyNoMsg,  () => leaveLobbyModal = null)
                })
                .WithInitButton(1)
                .Show();
        }

        public void ClickedLoadoutFromMainMenu()
        {
            _ = BuildMenuScript.Instance.Show(BuildMenuScript.BuildMenuMode.MainMenu);
        }

        public void ClickedLoadoutFromLobby()
        {
            _ = BuildMenuScript.Instance.Show(BuildMenuScript.BuildMenuMode.Lobby);
        }

        public void ClickedSettingsFromMainMenu()
        {
            _ = ShowSettingsTask();
        }

        public void ClickedSinglePlayer()
        {
            _ = ShowSinglePlayerPanelAnim();
        }

        public void ClickedMultiplayer()
        {
            _ = ShowMultiplayerPanelAnim();
        }
        
        public void ExitMultiplayerPanel()
        {
            _ = HideMultiplayerPanelAnim();
        }

        public void ExitSinglePlayerPanel()
        {
            _ = HideSinglePlayerPanelAnim();
        }

        public void ClickedCredits()
        {
            CreditsScript.Instance.Show();
        }

        public void ClickedQuit()
        {
            Game.Quit();
        }

        public void Show()
        {
            MainPanel.gameObject.SetActive(true);
        }

        private async UniTask ShowSettingsTask()
        {
            await HideMainPanelAnim();
            await SettingsUI.Instance.Show(SettingsUI.SettingsMenuMode.MainMenu);
        }

        private async UniTask LeaveLobbyTask()
        {
            LobbyUIScript.BlockStart = false;
            NetGame.NotifyExpectingDisconnect();
            NetGame.Instance.Disconnected(loadMainMenu: false);
            await LobbyUI.HideLobbyAnim();
            if(NetGame.GameType == GameType.SinglePlayer) {
                // TODO.
                // playPanel.gameObject.SetActive(NetGame.GameType == GameType.SinglePlayer);
            } else {
                await UniTask.WhenAll(ShowMainPanelAnim(), ShowMultiplayerPanelAnim());
            }
        }
        
        public async UniTask ShowLobbyCodePanelAnim()
        {
            if(lobbyCodePanel == null)
                return;
            
            lobbyCodePanel.GetComponent<UITweenCallbackController>().Show();
            if(await Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Fast, (t, val) => t.lobbyCanvasGroup.alpha = val)
                          .ToYieldInstruction().WithCancellation(destroyCancellationToken).SuppressCancellationThrow()) return;
        }

        public async UniTask HideLobbyCodePanelAnim()
        {
            if(lobbyCodePanel == null)
                return;
            
            lobbyCodePanel.GetComponent<UITweenCallbackController>().Hide();
            if(await Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Fast, (t, val) => t.lobbyCanvasGroup.alpha = val)
                          .ToYieldInstruction().WithCancellation(destroyCancellationToken).SuppressCancellationThrow()) return;
            
            lobbyCodePanel.gameObject.SetActive(false);
        }
        
        public async UniTask ShowMultiplayerPanelAnim()
        {
            if(multiplayerPanel == null)
                return;
            
            multiplayerPanel.GetComponent<UITweenCallbackController>().Show();
            if(await Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Fast, (t, val) => t.multiplayerCanvasGroup.alpha = val)
                          .ToYieldInstruction().WithCancellation(destroyCancellationToken).SuppressCancellationThrow()) return;
        }

        public async UniTask HideMultiplayerPanelAnim()
        {
            if(multiplayerPanel == null)
                return;
            
            multiplayerPanel.GetComponent<UITweenCallbackController>().Hide();
            if(await Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Fast, (t, val) => t.multiplayerCanvasGroup.alpha = val)
                          .ToYieldInstruction().WithCancellation(destroyCancellationToken).SuppressCancellationThrow()) return;
            
            multiplayerPanel.gameObject.SetActive(false);
        }
        
        public async UniTask ShowSinglePlayerPanelAnim()
        {
            if(singlePlayerPanel == null)
                return;
            
            singlePlayerPanel.GetComponent<UITweenCallbackController>().Show();
            if(await Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Fast, (t, val) => t.singlePlayerCanvasGroup.alpha = val)
                          .ToYieldInstruction().WithCancellation(destroyCancellationToken).SuppressCancellationThrow()) return;
        }

        public async UniTask HideSinglePlayerPanelAnim()
        {
            if(singlePlayerPanel == null)
                return;
            
            singlePlayerPanel.GetComponent<UITweenCallbackController>().Hide();
            if(await Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Fast, (t, val) => t.singlePlayerCanvasGroup.alpha = val)
                          .ToYieldInstruction().WithCancellation(destroyCancellationToken).SuppressCancellationThrow()) return;
            
            singlePlayerPanel.gameObject.SetActive(false);
        }
        
        public async UniTask ShowMainPanelAnim()
        {
            if(MainPanel == null)
                return;
            
            MainPanel.gameObject.SetActive(true);
            if(await Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Fast, (t, val) => t.mainCanvasGroup.alpha = val)
                          .ToYieldInstruction().WithCancellation(destroyCancellationToken).SuppressCancellationThrow()) return;
        }

        public async UniTask HideMainPanelAnim()
        {
            if(await Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Fast, (t, val) => t.mainCanvasGroup.alpha = val)
                          .ToYieldInstruction().WithCancellation(destroyCancellationToken).SuppressCancellationThrow()) return;
            
            MainPanel.gameObject.SetActive(false);
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            if(LobbyPanel.activeSelf) {
                _ = ResetGameStateFromLobbyTask();
                return;
            }
            if(!MainPanel.activeSelf) {
                _ = UniTask.WhenAll(ShowMainPanelAnim(), ShowMultiplayerPanelAnim());
            }
        }

        private async UniTask ResetGameStateFromLobbyTask()
        {
            await LobbyUI.HideLobbyAnim();
            await UniTask.WhenAll(ShowMainPanelAnim(), ShowMultiplayerPanelAnim());
        }
    }

}
