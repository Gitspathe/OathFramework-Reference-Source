using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Threading;

#if !UNITY_IOS && !UNITY_ANDROID
using OathFramework.Platform.Steam;
#endif

namespace OathFramework.Core.Service
{ 

    public class PlayerCountService : MonoBehaviour
    {
        [SerializeField] private float updateInterval = 20.0f;

        private CancellationTokenSource countCts;
        
        public float UpdateInterval => updateInterval;

        public int LobbyCount  { get; private set; }
        public int PlayerCount { get; private set; }
        
        public static PlayerCountService Instance { get; private set; }

#if !UNITY_IOS && !UNITY_ANDROID
        public PlayerCountService Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(PlayerCountService)} singletons.");
                return null;
            }

            Instance = this;
            return Instance;
        }

        public async Task UpdatePlayerCount(Action onComplete)
        {
            countCts?.Dispose();
            countCts = new CancellationTokenSource();
            (int lobbies, int players) = await SteamNetHandler.GetPlayerCount(countCts.Token);
            LobbyCount = lobbies; 
            PlayerCount = players;
            onComplete.Invoke();
        }
#endif

    }

}
