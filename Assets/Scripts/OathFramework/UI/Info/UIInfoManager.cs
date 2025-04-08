using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Networking;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.UI.Info
{

    public sealed class UIInfoManager : Subsystem, IPlayerConnectedCallback, IPlayerDisconnectedCallback
    {
        [SerializeField] private List<AbilityInfo> abilityInfos               = new();
        [SerializeField] private List<UIPerkInfo> perkInfos                     = new();
        [SerializeField] private List<UIStatParamInfoCollection> statParamInfos = new();
        
        private HashSet<PlayerInfoHolder> infoHolders                   = new();
        private Dictionary<string, UIEquippableInfo> equippableInfoDict = new();
        private Dictionary<string, AbilityInfo> abilityInfoDict       = new();
        private Dictionary<string, UIPerkInfo> perkInfoDict             = new();
        private Dictionary<string, UIStatParamInfo> statParamInfoDict   = new();
        private HashSet<NetClient> registeredPlayers                    = new();
        
        [field: SerializeField] public GameObject UIInfoStatBarPrefab       { get; private set; }
        [field: SerializeField] public GameObject UILevelDetailsGroupPrefab { get; private set; }

        public static UIInfoManager Instance { get; private set; }
        
        public override string Name    => "UI Info Manager";
        public override uint LoadOrder => SubsystemLoadOrders.UIInfoManager;

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(UIInfoManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }
            
            DontDestroyOnLoad(gameObject);
            Instance = this;
            GameCallbacks.Register((IPlayerConnectedCallback)this);
            GameCallbacks.Register((IPlayerDisconnectedCallback)this);
            foreach(AbilityInfo info in abilityInfos) {
                RegisterAbilityInfo(info.AbilityKey, info.DeepCopy());
            }
            foreach(UIPerkInfo info in perkInfos) {
                RegisterPerkInfo(info.PerkKey, info.DeepCopy());
            }
            foreach(UIStatParamInfoCollection collection in statParamInfos) {
                foreach(UIStatParamInfo info in collection.Collection) {
                    RegisterStatParamInfo(info.StatParamKey, info.DeepCopy());
                }
            }
            return UniTask.CompletedTask;
        }
        
        void IPlayerConnectedCallback.OnPlayerConnected(NetClient client)
        {
            TickPlayerInfo(client);
        }

        void IPlayerDisconnectedCallback.OnPlayerDisconnected(NetClient client)
        {
            ClearPlayerInfo(client);
        }

        public static void RegisterPlayerInfoHolder(PlayerInfoHolder holder)
        {
            if(!Instance.infoHolders.Add(holder))
                return;

            foreach(NetClient client in Instance.registeredPlayers) {
                holder.UpdateInfo(client);
            }
        }

        public static void UnregisterPlayerInfoHolder(PlayerInfoHolder holder)
        {
            if(Instance.infoHolders.Remove(holder)) {
                holder.ClearInfo();
            }
        }

        public static void TickPlayerInfo(NetClient client)
        {
            Instance.registeredPlayers.Add(client);
            foreach(PlayerInfoHolder infoHolder in Instance.infoHolders) {
                infoHolder.UpdateInfo(client);
            }
        }

        public static void ClearPlayerInfo(NetClient client)
        {
            Instance.registeredPlayers.Remove(client);
            foreach(PlayerInfoHolder infoHolder in Instance.infoHolders) {
                infoHolder.ClearInfo(client);
            }
        }

        public static UIEquippableInfo GetEquippableInfo(string equippableKey)
        {
            if(!Instance.equippableInfoDict.TryGetValue(equippableKey, out UIEquippableInfo info)) {
                Debug.LogError($"No {nameof(UIEquippableInfo)} for '{equippableKey}' found.");
                return null;
            }
            return info;
        }

        public static void RegisterEquippableInfo(string equippableKey, UIEquippableInfo info)
        {
            info.Setup();
            if(!Instance.equippableInfoDict.TryAdd(equippableKey, info)) {
                Debug.LogError($"Attempted to register duplicate {nameof(UIEquippableInfo)} for '{equippableKey}'");
            }
        }

        public static AbilityInfo GetAbilityInfo(string abilityKey)
        {
            if(!Instance.abilityInfoDict.TryGetValue(abilityKey, out AbilityInfo info)) {
                Debug.LogError($"No {nameof(AbilityInfo)} for '{abilityKey}' found.");
                return null;
            }
            return info;
        }
        
        public static void RegisterAbilityInfo(string abilityKey, AbilityInfo info)
        {
            if(!Instance.abilityInfoDict.TryAdd(abilityKey, info)) {
                Debug.LogError($"Attempted to register duplicate {nameof(AbilityInfo)} for '{abilityKey}'");
            }
        }
        
        public static UIPerkInfo GetPerkInfo(string perkKey)
        {
            if(!Instance.perkInfoDict.TryGetValue(perkKey, out UIPerkInfo info)) {
                Debug.LogError($"No {nameof(UIPerkInfo)} for '{perkKey}' found.");
                return null;
            }
            return info;
        }
        
        public static void RegisterPerkInfo(string perkKey, UIPerkInfo info)
        {
            if(!Instance.perkInfoDict.TryAdd(perkKey, info)) {
                Debug.LogError($"Attempted to register duplicate {nameof(UIPerkInfo)} for '{perkKey}'");
            }
        }
        
        public static UIStatParamInfo GetStatParamInfo(string paramKey)
        {
            if(!Instance.statParamInfoDict.TryGetValue(paramKey, out UIStatParamInfo info)) {
                Debug.LogError($"No {nameof(UIStatParamInfo)} info for '{paramKey}' found.");
                return null;
            }
            return info;
        }
        
        public static void RegisterStatParamInfo(string paramKey, UIStatParamInfo info)
        {
            if(!Instance.statParamInfoDict.TryAdd(paramKey, info)) {
                Debug.LogError($"Attempted to register duplicate {nameof(UIStatParamInfo)} for '{paramKey}'");
            }
        }
    }

}
