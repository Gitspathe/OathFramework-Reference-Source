using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.UI.Builds;
using PrimeTween;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI.Info
{
    
    public class MiniPlayerInfoPanel : MonoBehaviour, 
        IResetGameStateCallback
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button startGameButton;

        private CanvasGroup panelCanvasGroup;
        
        [field: SerializeField] public PlayerInfoHolder InfoHolder { get; private set; }

        public static MiniPlayerInfoPanel Instance { get; private set; }

        public MiniPlayerInfoPanel Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(MenuUI)} singleton.");
                Destroy(gameObject);
                return null;
            }
            
            Instance = this;
            GameCallbacks.Register((IResetGameStateCallback)this);
            InfoHolder.Setup();
            panelCanvasGroup = panel.GetComponent<CanvasGroup>();
            return this;
        }

        public async UniTask Show()
        {
            startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
            startGameButton.interactable = !NetGame.ConnectionsArePending;
            await ShowAnim();
        }

        public async UniTask Hide()
        {
            await HideAnim();
            panel.SetActive(false);
        }
        
        public void ClickedStartGame()
        {
            BuildMenuScript.Instance.Hide();
            MenuUI.Instance.LobbyUI.ClickedStartGame();
        }
        
        private async UniTask ShowAnim()
        {
            panel.gameObject.SetActive(true);
            await Tween.Custom(this, 0.0f, 1.0f, AnimDuration.Medium, (t, val) => t.panelCanvasGroup.alpha = val);
        }

        private async UniTask HideAnim()
        {
            await Tween.Custom(this, 1.0f, 0.0f, AnimDuration.Fast, (t, val) => t.panelCanvasGroup.alpha = val);
            panel.gameObject.SetActive(false);
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            panel.gameObject.SetActive(false);
        }
    }

}
