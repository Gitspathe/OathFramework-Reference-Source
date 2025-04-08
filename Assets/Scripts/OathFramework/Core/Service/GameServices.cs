using Cysharp.Threading.Tasks;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.Core.Service
{ 

    public sealed class GameServices : Subsystem
    {
        public static ConnectionService Connection     { get; private set; }
        public static PlayerSpawnService PlayerSpawn   { get; private set; }
        public static PlayerCountService PlayerCount   { get; private set; }
        public static NotificationService Notification { get; private set; }
        public static SpectateService Spectate         { get; private set; }
        public static WaveService Waves                { get; private set; }

        public static GameServices Instance            { get; private set; }
        
        public override string Name    => "Game Services";
        public override uint LoadOrder => SubsystemLoadOrders.GameService;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(GameServices)} singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            Instance = this;
            DontDestroyOnLoad(this);

            Connection    = GetComponentInChildren<ConnectionService>().Initialize();
            PlayerSpawn   = GetComponentInChildren<PlayerSpawnService>().Initialize();
#if !UNITY_IOS && !UNITY_ANDROID
            PlayerCount   = GetComponentInChildren<PlayerCountService>().Initialize();
#endif
            Notification  = GetComponentInChildren<NotificationService>().Initialize();
            Spectate      = GetComponentInChildren<SpectateService>().Initialize();
            Waves         = GetComponentInChildren<WaveService>().Initialize();
            return UniTask.CompletedTask;
        }
    }

}
