using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using OathFramework.Core;
using OathFramework.EntitySystem.Players;
using OathFramework.Networking;
using OathFramework.Progression;
using OathFramework.UI.Info;
using OathFramework.UI.Platform;
using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace OathFramework.UI
{ 

    public class LobbyUIScript : MonoBehaviour, 
        IOnScreenKeyboardCallback, IControlSchemeChangedCallback
    {
        [SerializeField] private Transform lobbyTransform;
        [SerializeField] private PlayerInfoHolder infoHolder;

        [Space(5)]
        
        [SerializeField] private Transform quickJoinBtn;
        [SerializeField] private Transform joinLobbyPanel;
        [SerializeField] private TextMeshProUGUI gameCodeText;
        [SerializeField] private TMP_InputField codeInputField;
        [SerializeField] private Button startGameButton;

        [Space(5)]

        [SerializeField] private TMP_Dropdown mapDropDown;

        [Space(5)]

        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI statusTextButton;
        [SerializeField] private Button statusButton;

        [Space(5)]
        
        [SerializeField] private Transform lobbyPanel;
        
        [field: Space(5)]
        
        [field: SerializeField] public UINavigationGroup JoinNavGroup { get; private set; }

        [Header("Localization")]
        
        [SerializeField] private LocalizedString enterCodeMsg;
        [SerializeField] private LocalizedString enterIPMsg;
        [SerializeField] private LocalizedString gameCodeMsg;
        [SerializeField] private LocalizedString publicMsg;
        [SerializeField] private LocalizedString privateMsg;
        [SerializeField] private LocalizedString setPublicMsg;
        [SerializeField] private LocalizedString setPrivateMsg;
        [SerializeField] private LocalizedString showLoadoutMsg;

        public TMP_InputField CodeInputField => codeInputField;
        public TMP_Dropdown MapDropDown => mapDropDown;

        public static bool BlockStart { get; set; }

        public GameType GameType => NetGame.GameType;

        private ModalConfig showLoadoutModal;
        private CanvasGroup canvasGroup;
        private byte lastMaxPlayers;

        private void Awake()
        {
            lastMaxPlayers = PlayerManager.MaxPlayers;
            codeInputField.onSubmit.AddListener(JoinGame);
            GameControlsCallbacks.Register((IOnScreenKeyboardCallback)this);
            GameControlsCallbacks.Register((IControlSchemeChangedCallback)this);
            canvasGroup = lobbyPanel.GetComponent<CanvasGroup>();
        }
        
        private void LateUpdate()
        {
            TextMeshProUGUI placeholder = codeInputField.placeholder.GetComponent<TextMeshProUGUI>();
            gameCodeText.text = string.IsNullOrEmpty(NetGame.CurrentCode) ? "" : $"{gameCodeMsg.GetLocalizedString()}: {NetGame.CurrentCode}";
            placeholder.text  = NetGame.Instance.UseSteam ? enterCodeMsg.GetLocalizedString() : enterIPMsg.GetLocalizedString();
            startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
            startGameButton.interactable = !NetGame.ConnectionsArePending;
#if !DEBUG
            quickJoinBtn.gameObject.SetActive(NetGame.Instance.UseSteam);
#endif
        }
        
        private void Show()
        {
            lobbyTransform.gameObject.SetActive(true);
            statusText.gameObject.SetActive(GameType == GameType.Multiplayer && NetGame.Instance.UseSteam);
            statusTextButton.gameObject.SetActive(GameType == GameType.Multiplayer && NetGame.Instance.UseSteam);
            statusButton.gameObject.SetActive(GameType == GameType.Multiplayer && NetGame.Instance.UseSteam && NetGame.Manager.IsServer);
            gameCodeText.gameObject.SetActive(GameType == GameType.Multiplayer && NetGame.Instance.UseSteam);
#if !UNITY_IOS && !UNITY_ANDROID
            UpdateStatusText();
#endif
        }
        
        public static string GetMapName(int dropDownValue)
        {
            switch(dropDownValue) {
                case 0:
                    return Game.Instance.gameScene;
                case 1:
                    return Game.Instance.testScene;
                default:
                    return null;
            }
        }

        private void JoinGame(string s)
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected || codeInputField.wasCanceled)
                return;
            
            _ = JoinLobbyTask(s);
        }
        
        public void ClickedStartGame()
        {
            if(BlockStart)
                return;

            NetGame.Instance.StartGame(GetMapName(0));
            BlockStart = true;
        }

        public void ClickedNewMultiplayerGame()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;
            
            _ = MakeLobbyTask();
        }

        public void ClickedLoadMultiplayerGame()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;
            
            // TODO.
        }

        public void ClickedNewSinglePlayerGame()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;

            _ = StartNewGameTask();
        }

        public void ClickedLoadSinglePlayerGame()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;
            
            // TODO.
        }

        public void ClickedJoinLobbyCode()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;
            
            _ = JoinLobbyTask(codeInputField.text);
        }
        
        public void ClickedJoinCode()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;
            
            _ = ClickedEnterLobbyCodeTask();
        }

        public void ExitJoinCode()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;
            
            _ = ClickedExitLobbyCodeTask();
        }
        
        public void ClickedQuickMatch()
        {
            if(NetGame.ConnectionState != GameConnectionState.Disconnected)
                return;

            _ = ClickedQuickJoinTask();
        }

        public void RegisterInfoHolder()
        {
            infoHolder.Setup();
        }

        public void ShowSinglePlayer()
        {
            Show();
        }

        public void ShowMultiplayer()
        {
            _ = ShowDelayed();
        }

        private async UniTask StartNewGameTask()
        {
            if(ProgressionManager.Profile.showLoadoutPopup) {
                showLoadoutModal = ModalConfig.Retrieve()
                    .WithText(showLoadoutMsg)
                    .WithButtons(new List<(LocalizedString, Action)> { 
                        (UICommonMessages.Yes, () => _ = StartSinglePlayerNewGameLobbyTask()), 
                        (UICommonMessages.No,  () => {
                            ProgressionManager.Profile.showLoadoutPopup = false;
                            showLoadoutModal                            = null;
                            _ = ProgressionManager.Profile.Save();
                            _ = StartNewGameTask();
                        })
                    })
                    .WithInitButton(1)
                    .Show();
                return;
            }
            
            await UniTask.WhenAll(
                MenuUI.Instance.HideSinglePlayerPanelAnim(), 
                MenuUI.Instance.HideMainPanelAnim()
            );
            bool success = await NetGame.Instance.StartSinglePlayerHost();
            if(!success || BlockStart)
                return;

            NetGame.Instance.StartGame(GetMapName(0));
            BlockStart = true;
        }

        private async UniTask StartSinglePlayerNewGameLobbyTask()
        {
            await UniTask.WhenAll(
                MenuUI.Instance.HideSinglePlayerPanelAnim(), 
                MenuUI.Instance.HideMainPanelAnim()
            );
            
            ProgressionManager.Profile.showLoadoutPopup = false;
            _ = ProgressionManager.Profile.Save();
            await UniTask.WhenAll(
                MenuUI.Instance.HideMultiplayerPanelAnim(), 
                MenuUI.Instance.HideMainPanelAnim()
            );
            bool success = await NetGame.Instance.StartMultiplayerHost();
            if(!success)
                return;

            MenuUI.Instance.ClickedLoadoutFromLobby();
        }

        private async UniTask MakeLobbyTask()
        {
            await UniTask.WhenAll(
                MenuUI.Instance.HideMultiplayerPanelAnim(), 
                MenuUI.Instance.HideMainPanelAnim()
            );
            bool success = await NetGame.Instance.StartMultiplayerHost();
            if(!success)
                return;
            
            await ShowLobbyAnim();
        }

        private async UniTask JoinLobbyTask(string code)
        {
            await UniTask.WhenAll(
                MenuUI.Instance.HideLobbyCodePanelAnim(), 
                MenuUI.Instance.HideMainPanelAnim()
            );
            bool success = await NetGame.Instance.ConnectMultiplayer(code);
            if(!success)
                return;
            
            CodeInputField.text = "";
            await ShowLobbyAnim();
        }

        private async UniTask ClickedQuickJoinTask()
        {
            await UniTask.WhenAll(
                MenuUI.Instance.HideMultiplayerPanelAnim(), 
                MenuUI.Instance.HideMainPanelAnim()
            );
            bool success = await NetGame.Instance.ConnectQuickMultiplayer();
            if(!success)
                return;
            
            CodeInputField.text = "";
            await ShowLobbyAnim();
        }
        
        public async UniTask ShowLobbyAnim()
        {
            if(lobbyPanel == null)
                return;
            
            Show();
            await Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Slow, (t, val) => t.canvasGroup.alpha = val);
        }

        public async UniTask HideLobbyAnim()
        {
            await Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Medium, (t, val) => t.canvasGroup.alpha = val);
            if(lobbyPanel == null)
                return;
            
            lobbyPanel.gameObject.SetActive(false);
        }

        private async UniTask ShowDelayed()
        {
            await UniTask.Yield();
            Show();
        }
        
        private async UniTask ClickedEnterLobbyCodeTask()
        {
            await MenuUI.Instance.HideMultiplayerPanelAnim();
            await MenuUI.Instance.ShowLobbyCodePanelAnim();
        }

        private async UniTask ClickedExitLobbyCodeTask()
        {
            await MenuUI.Instance.HideLobbyCodePanelAnim();
            await MenuUI.Instance.ShowMultiplayerPanelAnim();
        }

#if !UNITY_IOS && !UNITY_ANDROID
        public void StatusButtonPressed()
        {
            if(NetGame.CurrentLobby == null)
                return;

            NetGame.CurrentLobby.SetPublic(!NetGame.CurrentLobby.IsPublic);
            UpdateStatusText();
        }

        public void UpdateStatusText()
        {
            if(NetGame.CurrentLobby == null)
                return;
            
            statusText.text       = NetGame.CurrentLobby.IsPublic ? publicMsg.GetLocalizedString() : privateMsg.GetLocalizedString();
            statusTextButton.text = NetGame.CurrentLobby.IsPublic ? setPrivateMsg.GetLocalizedString() : setPublicMsg.GetLocalizedString();
        }
#endif

        public void JoinCodeEditBegin()
        {
            if(!GameControls.UsingController)
                return;
            
            OnScreenKeyboard.Instance.SetActiveFocus(codeInputField, null, OnScreenKeyboard.KeyboardType.None);
        }

        public void JoinCodeEditEnd()
        {
            OnScreenKeyboard.Instance.SetActive(false);
        }
        
        void IOnScreenKeyboardCallback.OnOSKOpened(Selectable target)
        {
            if(!GameControls.UsingController)
                return;
            
            JoinNavGroup.SetNavigation(false);
        }

        public void OnOSKSubmit(Selectable target) {}

        void IOnScreenKeyboardCallback.OnOSKClosed()
        {
            if(!GameControls.UsingController)
                return;
            
            JoinNavGroup.SetNavigation(true);
        }

        void IControlSchemeChangedCallback.OnControlSchemeChanged(ControlSchemes controlScheme)
        {
            if(!codeInputField.gameObject.activeInHierarchy)
                return;
            
            JoinNavGroup.SetNavigation(!GameControls.UsingController);
            if(GameControls.UsingController) {
                codeInputField.OnSelect(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            }
        }
        
        private void OnDestroy()
        {
            codeInputField.onSubmit.RemoveListener(JoinGame);
            GameControlsCallbacks.Unregister((IOnScreenKeyboardCallback)this);
            GameControlsCallbacks.Unregister((IControlSchemeChangedCallback)this);
        }
    }

}
