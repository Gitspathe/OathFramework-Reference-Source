using OathFramework.Core;
using OathFramework.Core.Service;
using OathFramework.Networking;
using OathFramework.UI.Builds;
using TMPro;
using UnityEngine;

namespace OathFramework.UI
{
    public class SpectateUIScript : LoopComponent,
        ILoopLateUpdate, IResetGameStateCallback
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        [SerializeField] private GameObject mainPanel;
        [SerializeField] private TextMeshProUGUI spectateText;
        [SerializeField] private TextMeshProUGUI respawnTimeText;
        [SerializeField] private Transform respawnBtn;
        [SerializeField] private Transform loadoutBtn;

        private float respawnTimeRemaining;
        private bool loadoutIsOpen;
        
        public static SpectateUIScript Instance { get; private set; }
        
        public SpectateUIScript Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(SpectateUIScript)} singleton.");
                Destroy(Instance);
                return null;
            }
            
            Instance = this;
            Hide();
            GameCallbacks.Register((IResetGameStateCallback)this);
            return this;
        }

        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(NetClient.SelfAlive)
                return;
            
            respawnTimeRemaining = Mathf.Clamp(respawnTimeRemaining - Time.deltaTime, 0.0f, 10.0f);
            respawnBtn.gameObject.SetActive(!loadoutIsOpen && respawnTimeRemaining <= 0.0f);
            respawnTimeText.text = respawnTimeRemaining.ToString("0.0");
        }

        public void RespawnPressed()
        {
            respawnBtn.gameObject.SetActive(false);
            loadoutBtn.gameObject.SetActive(false);
            PlayerSpawnService.Instance.SpawnPlayer(NetClient.Self);
            SpectateService.ExitSpectate();
        }

        public void ClickedLoadout()
        {
            mainPanel.SetActive(false);
            loadoutBtn.gameObject.SetActive(false);
            respawnBtn.gameObject.SetActive(false);
            loadoutIsOpen = true;
            _ = BuildMenuScript.Instance.Show(BuildMenuScript.BuildMenuMode.InGame);
        }

        public void ExitedLoadout()
        {
            loadoutBtn.gameObject.SetActive(true);
            mainPanel.SetActive(true);
            loadoutIsOpen = false;
        }

        public void SetPlayer(NetClient client)
        {
            spectateText.text = $"Spectating {client.Name}";
        }

        public void Hide()
        {
            loadoutBtn.gameObject.SetActive(true);
            mainPanel.SetActive(false);
            respawnTimeRemaining = 10.0f;
            loadoutIsOpen        = false;
        }

        public void Show()
        {
            mainPanel.SetActive(true);
            respawnTimeRemaining = 10.0f;
        }

        public void ClickedNext()
        {
            SpectateService.SwapNext();
        }

        public void ClickedPrevious()
        {
            SpectateService.SwapPrevious();
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            Hide();
        }
    }
}
