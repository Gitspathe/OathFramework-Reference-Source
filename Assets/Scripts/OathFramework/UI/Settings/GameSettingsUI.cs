using OathFramework.Core;
using OathFramework.Settings;
using TMPro;
using UnityEngine;

namespace OathFramework.UI.Settings
{
    public class GameSettingsUI : LoopComponent, ILoopUpdate
    {
        [SerializeField] private TMP_Dropdown languageSetting;
        [SerializeField] private TMP_Dropdown networkTypeSetting;
        [SerializeField] private TMP_InputField multiplayerNameField;

        [Space(10)]
        
        [SerializeField] private Transform networkSettingTransform;
        [SerializeField] private Transform multiplayerNameTransform;
        [SerializeField] private Transform steamNetworkTypeMsg;
        [SerializeField] private Transform udpNetworkTypeMsg;

        public static GameSettingsUI Instance { get; private set; }

        private static SettingsManager.GameSettings ManagerSettings {
            get => SettingsManager.Instance.CurrentSettings.game;
            set => SettingsManager.Instance.CurrentSettings.game = value;
        }
        
        private bool init;
        private SettingsManager.GameSettings curSettings;
        
        public GameSettingsUI Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(GameSettingsUI)} singleton.");
                Destroy(Instance);
                return null;
            }
            Instance = this;
            init     = true;
            
            curSettings = ManagerSettings.DeepCopy();
            return this;
        }

        void ILoopUpdate.LoopUpdate()
        {
            networkSettingTransform.gameObject.SetActive(Game.State == GameState.MainMenu);
            multiplayerNameTransform.gameObject.SetActive(Game.State == GameState.MainMenu);
        }
        
        public void RebindManagerSettings()
        {
            curSettings = ManagerSettings.DeepCopy();
            UpdateGameSettingsUI();
        }

        public void UpdateCurrentSettings()
        {
            curSettings.language        = languageSetting.value;
            curSettings.networkType     = networkTypeSetting.value;
            curSettings.multiplayerName = multiplayerNameField.text;
            ManagerSettings             = curSettings.DeepCopy();
            SettingsManager.Instance.ApplyGame();
            UpdateGameSettingsUI();
        }

        public void UpdateGameSettingsUI()
        {
            languageSetting.SetValueWithoutNotify(curSettings.language);
            steamNetworkTypeMsg.gameObject.SetActive(curSettings.networkType == 0);
            udpNetworkTypeMsg.gameObject.SetActive(curSettings.networkType == 1);
            networkTypeSetting.SetValueWithoutNotify(curSettings.networkType);
            multiplayerNameField.SetTextWithoutNotify(curSettings.multiplayerName);
        }
    }
}
