using UnityEngine;
using TMPro;
using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.UI.Info;
using Debug = UnityEngine.Debug;

namespace OathFramework.UI
{ 

    public class LeaderboardUIScript : LoopComponent, 
        ILoopLateUpdate, IResetGameStateCallback
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI gameCodeText;
        
        private PlayerInfoHolder playerInfoHolder;
        
        public static bool OpeningBlocked => Game.State != GameState.InGame || PauseMenu.IsPaused;
        public static bool IsOpen       { get; private set; }

        public static LeaderboardUIScript Instance { get; private set; }
        
        public LeaderboardUIScript Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(LeaderboardUIScript)} singletons.");
                Destroy(gameObject);
                return this;
            }

            panel.SetActive(false);
            Instance = this;
            
            playerInfoHolder = GetComponent<PlayerInfoHolder>();
            playerInfoHolder.Setup();
            GameCallbacks.Register((IResetGameStateCallback)this);
            return this;
        }
        
        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(!panel.gameObject.activeSelf)
                return;
            
            //gameCodeText.text = string.IsNullOrEmpty(NetGame.CurrentCode) ? "" : $"GAME CODE: {NetGame.CurrentCode}";
        }
        
        public void ClickedLeave()
        {
            NetGame.Instance.Disconnected();
        }

        public static void Open()
        {
            if(IsOpen)
                return;

            Instance.panel.SetActive(true);
            IsOpen = true;
        }

        public static void Close()
        {
            if(!IsOpen)
                return;

            Instance.panel.SetActive(false);
            IsOpen = false;
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            Close();
        }
    }

}
