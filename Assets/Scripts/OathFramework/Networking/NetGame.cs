using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text;
using OathFramework.Core;
using OathFramework.Core.Service;
using OathFramework.EntitySystem.Players;
using OathFramework.Persistence;
using OathFramework.Platform.Unity;
using OathFramework.Pooling;
using OathFramework.ProcGen;
using OathFramework.Settings;
using OathFramework.UI;
using OathFramework.Utility;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UnityEngine.Localization;
using Debug = UnityEngine.Debug;

#if !UNITY_IOS && !UNITY_ANDROID
using Netcode.Transports.Facepunch;
using Steamworks;
using OathFramework.Platform.Steam;
#endif

namespace OathFramework.Networking
{ 

    public sealed class NetGame : Subsystem, IInitialized, IJitOptimizerTask
    {
        [SerializeField] private GameObject globalNetPrefab;
        [SerializeField] private GameObject netProcGenScenePrefab;
        [SerializeField] private PoolCollectionType poolCollection;
        [SerializeField] private float connectionSpawnTimeout = 30.0f;
        [SerializeField] private float initialSyncTimeout     = 15.0f;

        [Space(10)]

        [SerializeField] private GameObject networkClientPrefab;

        public static NetGameMsg Msg { get; private set; }

        private static Dictionary<ulong, ConnectionData> connectionDatas = new();
        private static HashSet<ulong> connecting                             = new();
        private static bool gameInProgressCheck                              = true;
        private static bool expectedDisconnect;
        private static float initialSyncTime;
        private static CancellationTokenSource netCts;

        [field: SerializeField] public bool UseSteam                       { get; set; } = true;
        [field: SerializeField] public GameObject UnityTransportPrefab     { get; private set; }
        [field: SerializeField] public GameObject FacepunchTransportPrefab { get; private set; }

        public static NetGameCallbacks Callbacks            { get; } = new();
        public static Transports TransportType              { get; set; }
        public static GameConnectionState ConnectionState   { get; set; }
        public static GameType GameType                     { get; private set; }
        public static bool IsLoadingScene                   { get; set; }

        public static string UDPMultiplayerName             => SettingsManager.Instance.CurrentSettings.game.GetMultiplayerName();
        public static string CurrentCode                    { get; set; }
        private static NetHandler CurrentHandler            { get; set; }
        public static GameObject CurrentTransport           { get; set; }
        public static NetScene NetScene       { get; private set; }
        public static NetLobbyBase CurrentLobby             => CurrentHandler.NetLobby;
        public static NetHandlers NetHandlerType            => CurrentHandler?.Type ?? NetHandlers.None;

        public static bool IsServer                         => NetworkManager.Singleton.IsServer;
        public static bool IsClient                         => NetworkManager.Singleton.IsClient;
        
        public static bool ConnectionsArePending            => connecting.Count > 0;
        public static NetworkManager Manager                => NetworkManager.Singleton;
        public static NetworkConfig NetworkConfig           => NetworkManager.Singleton.NetworkConfig;
        public static UnityTransport UnityTransport         => CurrentTransport.GetComponent<UnityTransport>();
#if !UNITY_IOS && !UNITY_ANDROID
        public static FacepunchTransport FacepunchTransport => CurrentTransport.GetComponent<FacepunchTransport>();
#endif

        private static AccessToken callbackToken;
        
        public static NetGame Instance { get; private set; }
        
        public override string Name    => "Net Game";
        public override uint LoadOrder => SubsystemLoadOrders.NetGame;
        
        uint ILockableOrderedListElement.Order => 0;

        protected override void Awake()
        {
            base.Awake();
            Msg           = GetComponent<NetGameMsg>();
            callbackToken = Callbacks.Access.GenerateAccessToken();
            GameCallbacks.Register((IInitialized)this);
        }
        
        private void Start()
        {
            NetworkManager.Singleton.OnConnectionEvent         += OnConnectionEventCallback;
            NetworkManager.Singleton.OnServerStarted           += OnServerStarted;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(NetGame)} singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }
            Instance = this;
            DontDestroyOnLoad(this);
            
            ConnectionState = GameConnectionState.Disconnected;
            return UniTask.CompletedTask;
        }
        
        UniTask IInitialized.OnGameInitialized()
        {
            // Start transport immediately for the player count.
            if(UseSteam) {
                StartHandler(true);
            }
            return UniTask.CompletedTask;
        }

        private void OnConnectionEventCallback(NetworkManager manager, ConnectionEventData data)
        {
            if(Game.State == GameState.Quitting || ConnectionState == GameConnectionState.Disconnected)
                return;
            
            ulong clientNetID  = data.ClientId;
            bool fromSelf      = NetworkManager.Singleton.LocalClientId == clientNetID;
            bool fromServer    = NetworkManager.ServerClientId == clientNetID;
            bool inLobbyOrMenu = Game.State == GameState.MainMenu || Game.State == GameState.Lobby;
            switch(data.EventType) {
                case ConnectionEvent.ClientConnected: {
                    if(!Manager.IsServer)
                        break;

                    string uid = connectionDatas[clientNetID].Name;
#if !UNITY_IOS && !UNITY_ANDROID
                    if(TransportType == Transports.Facepunch) {
                        uid = fromSelf ? SteamClient.SteamId.ToString() : data.ClientId.ToString();
                    }
#endif

                    if(fromSelf) {
                        InitGlobalNetObject();
                    } else {
                        connecting.Add(clientNetID);
                    }
                    if(!TryGetNextPlayerIndex(uid, out byte nextIndex)) {
                        Manager.DisconnectClient(clientNetID);
                        break;
                    }
                    if(Game.State == GameState.MainMenu) {
                        Debug.LogError("GameState is 'MainMenu' while network is open.");
                        break;
                    }
                    if(Game.State == GameState.Lobby) {
                        NetGameRpcHelper.NotifyInLobby(clientNetID);
                    }

                    NetClient player = CreateNetClientGameObject(connectionDatas[clientNetID], nextIndex, clientNetID, uid);
                } break;
                
                case ConnectionEvent.PeerConnected: {
                    if(!fromSelf) {
                        connecting.Add(clientNetID);
                    }
                } break;
                
                case ConnectionEvent.ClientDisconnected: {
                    connecting.Remove(clientNetID);
                    if(fromServer) {
                        Disconnected(true, !inLobbyOrMenu);
                        break;
                    }
                    if(fromSelf) {
                        Disconnected(!expectedDisconnect, !inLobbyOrMenu);
                        break;
                    }
                    if(!Manager.IsServer)
                        break;
                    
                    if(!PlayerManager.TryGetPlayerFromNetID(clientNetID, out NetClient client)) {
                        Debug.LogWarning($"No NetPlayer for client ID '{clientNetID}' found on disconnect.");
                        break;
                    }
                    client.GetComponent<NetworkObject>().Despawn();
                } break;
                
                case ConnectionEvent.PeerDisconnected: {
                    if(ConnectionState == GameConnectionState.Disconnected)
                        break;
                    
                    connecting.Remove(clientNetID);
                } break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            if(fromSelf) {
                expectedDisconnect = false;
            }
        }

        private static bool TryGetNextPlayerIndex(string uid, out byte index)
        {
            index = 0;
            if(GlobalNetInfo.UsingSnapshot) {
                if(PlayerManager.TryGetPlayerIndex(uid, out index) || PlayerManager.GetFreeClientIndex(out index))
                    return true;
                
                Debug.LogError("No free player index found.");
                return false;
            }
            if(PlayerManager.GetFreeClientIndex(out index))
                return true;

            Debug.LogError("No free player index found.");
            return false;
        }

        public static void RegisterCallbacks()
        {
            gameInProgressCheck = true;
            if(NetworkManager.Singleton.SceneManager == null)
                return;

            NetworkManager.Singleton.SceneManager.OnSceneEvent += Instance.OnSceneEvent;
        }

        public static void UnregisterCallbacks()
        {
            if(NetworkManager.Singleton.SceneManager != null) { 
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= Instance.OnSceneEvent;
            }
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            ulong clientId              = request.ClientNetworkId;
            byte[] connectionData       = request.Payload;
            response.Approved           = true;
            response.CreatePlayerObject = false;
            response.PlayerPrefabHash   = null;
            response.Position           = Vector3.zero;
            response.Rotation           = Quaternion.identity;
            response.Reason             = "Some reason for not approving the client";
            response.Pending            = IsLoadingScene;
            ConnectionData conData      = JsonUtility.FromJson<ConnectionData>(new string(Encoding.UTF8.GetChars(connectionData)));
            connectionDatas.Remove(clientId);
            connectionDatas.Add(clientId, conData);
            connecting.Add(request.ClientNetworkId);
        }

        private void StartHandler(bool? useSteamOverride = null)
        {
#if !UNITY_IOS && !UNITY_ANDROID
            if(useSteamOverride ?? UseSteam) {
                CurrentHandler = new SteamNetHandler().Initialize();
            } else {
                CurrentHandler = new UnityNetHandler().Initialize();
            }
#else
            CurrentHandler = new UnityNetHandler().Initialize();
#endif
            
            CurrentHandler.StartTransport();
        }

        public async UniTask<bool> StartSinglePlayerHost(string snapshotName = null)
        {
            try {
                // Single-player - always use Unity Transport.
                StartHandler(false);

                ConnectionData conData = new(
                    UDPMultiplayerName, 
                    SupporterDLCUtil.HasSupporterDLC,
                    SupporterDLCUtil.Secret
                );
                
                NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(conData));
                ConnectionState              = GameConnectionState.Loading;
                GameType                     = GameType.SinglePlayer;
                bool usingSnapshot           = !string.IsNullOrEmpty(snapshotName);
                GlobalNetInfo.UsingSnapshot  = usingSnapshot;
                Game.SetState(GameState.Lobby);
                if(usingSnapshot) {
                    await PersistenceManager.LoadSnapshot(snapshotName);
                    await PersistenceManager.ApplySceneData("_GLOBAL");
                }
                netCts?.Cancel();
                netCts?.Dispose();
                netCts = new CancellationTokenSource();
                return await CurrentHandler.StartMultiplayerHost(netCts.Token);
            } catch(Exception e) {
                string s = $"{Msg.SinglePlayerHostFailedStr.GetLocalizedString()}.\n{e.Message}";
                Debug.LogError(s);
                ErrorReset();
                ModalUIScript.ShowGeneric(Msg.SinglePlayerHostFailedStr);
                return false;
            } finally {
                LobbyUIScript.BlockStart = false;
            }
        }

        public async UniTask<bool> StartMultiplayerHost(string snapshotName = null)
        {
            try {
                StartHandler();
                ConnectionState             = GameConnectionState.Loading;
                GameType                    = GameType.Multiplayer;
                bool usingSnapshot          = !string.IsNullOrEmpty(snapshotName);
                GlobalNetInfo.UsingSnapshot = usingSnapshot;
                if(usingSnapshot) {
                    await PersistenceManager.LoadSnapshot(snapshotName);
                    await PersistenceManager.ApplySceneData("_GLOBAL");
                }
                netCts?.Cancel();
                netCts?.Dispose();
                netCts = new CancellationTokenSource();
                if(!await CurrentHandler.StartMultiplayerHost(netCts.Token))
                    return false;
                
                await PoolManager.AwaitLoading();
                return true;
            } catch(Exception e) {
                Debug.LogError(e);
                ErrorReset();
                ModalUIScript.ShowGeneric(Instance.UseSteam ? Msg.MultiPlayerStartGameFailedSteamStr : Msg.MultiPlayerHostFailedStr);
                LobbyUIScript.BlockStart = false;
                return false;
            }
        }

        public void StartGame(string map)
        {
            if(!Manager.IsServer)
                return;

            LoadScene(map);
        }

        public void LoadScene(string scene)
        {
            if(!Manager.IsServer)
                return;

            try {
                ConnectionState  = GameConnectionState.Loading;
                LoadSequence seq = new LoadSequence().WithScene(scene);
                if(GlobalNetInfo.UsingSnapshot) {
                    seq.WithSnapshotScene(scene);
                }
                _ = seq.Execute();
            } catch(Exception e) {
                Debug.LogError(e);
                ErrorReset();
                ModalUIScript.ShowGeneric(Msg.MultiPlayerStartGameFailedStr);
                LobbyUIScript.BlockStart = false;
            }
        }

        public async UniTask<bool> ConnectQuickMultiplayer()
        {
#if UNITY_IOS || UNITY_ANDROID
            return false;
#else
            await MenuUI.Instance.AwaitPlayerCountTask();
            try {
                StartHandler();
                ConnectionState = GameConnectionState.Loading;
                GameType        = GameType.Multiplayer;
                netCts?.Cancel();
                netCts?.Dispose();
                netCts = new CancellationTokenSource();
                return await CurrentHandler.ConnectQuickMultiplayer(netCts.Token);
            } catch(Exception e) {
                Debug.LogError(e);
                ErrorReset();
                ModalUIScript.ShowGeneric(Instance.UseSteam ? Msg.ConnectFailedSteamStr : Msg.ConnectFailedStr);
                LobbyUIScript.BlockStart = false;
                return false;
            }
#endif
        }

        public async UniTask<bool> ConnectMultiplayer(string code)
        {
            try { 
                StartHandler();
                ConnectionState = GameConnectionState.Loading;
                GameType        = GameType.Multiplayer;
                netCts?.Cancel();
                netCts?.Dispose();
                netCts = new CancellationTokenSource();
                return await CurrentHandler.ConnectMultiplayer(code, netCts.Token);
            } catch(Exception e) {
                Debug.LogError(e);
                ErrorReset();
                ModalUIScript.ShowGeneric(Instance.UseSteam ? Msg.ConnectFailedSteamStr : Msg.ConnectFailedStr);
                LobbyUIScript.BlockStart = false;
                return false;
            }
        }

        private static void OnServerStarted() => ConnectionState = GameConnectionState.Ready;

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch(sceneEvent.SceneEventType) {
                case SceneEventType.Load: {
                    IsLoadingScene = true;
                    _ = LoadingUIScript.Show();
                    LoadingUIScript.SetProgress(Msg.LoadingSceneStr, 0.0f);
                    LoadingUIScript.StartSceneProgressTask(sceneEvent.AsyncOperation, true);
                    Callbacks.Access.OnBeganLoadingScene(callbackToken, sceneEvent);
                } break;
                case SceneEventType.Unload: {

                } break;
                case SceneEventType.LoadComplete: {
                    if(Manager.IsServer) {
                        if(sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId) {
                            LoadingUIScript.SetProgress(Msg.WaitingForClientsStr, 1.0f);
                        }
                        Callbacks.Access.OnClientLoadedScene(callbackToken, sceneEvent);
                        return;
                    }
                    
                    // If the game is in progress, the new client doesn't receive LoadEventCompleted.
                    // So complete it here instead.
                    if(gameInProgressCheck) {
                        ClientLoadCompleted();
                        Callbacks.Access.OnClientLoadedScene(callbackToken, sceneEvent);
                        
                        // This call is very weird. Maybe try coming up with an alternative?
                        // It is here because LoadEventCompleted is NOT called when the game is already in-progress.
                        Callbacks.Access.OnLoadSceneEventCompleted(callbackToken, sceneEvent);
                        return;
                    }
                    LoadingUIScript.SetProgress(Msg.WaitingForOthersStr, 1.0f);
                    Callbacks.Access.OnClientLoadedScene(callbackToken, sceneEvent);
                } break;
                case SceneEventType.UnloadComplete: {

                } break;
                case SceneEventType.LoadEventCompleted: {
                    ClientLoadCompleted();
                    if(Manager.IsServer) {
                        GameServices.PlayerSpawn.FindSpawnAreas();
                        foreach(ulong clientID in sceneEvent.ClientsThatTimedOut) {
                            if(!PlayerManager.TryGetPlayerFromNetID(clientID, out NetClient timedOut))
                                continue;

                            KickPlayer(timedOut, "Timed out.");
                        }
                    }
                    Callbacks.Access.OnLoadSceneEventCompleted(callbackToken, sceneEvent);
                } break;
                case SceneEventType.UnloadEventCompleted: {

                } break;
                case SceneEventType.Synchronize: {
                    
                } break;
                case SceneEventType.ReSynchronize: {
                    
                } break;
                case SceneEventType.SynchronizeComplete: {
                    
                } break;
                case SceneEventType.ActiveSceneChanged: {
                    
                } break;
                case SceneEventType.ObjectSceneChanged: {
                    
                } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return;

            void ClientLoadCompleted()
            {
                ConnectionState = GameConnectionState.Ready;
                IsLoadingScene  = false;
                GameCallbacks.Access.OnLevelLoaded(Game.AccessToken);
            }
        }

        private NetClient CreateNetClientGameObject(ConnectionData data, byte playerIndex, ulong clientID, string uid)
        {
            GameObject clientGO = Instantiate(networkClientPrefab);
            NetClient player    = clientGO.GetComponent<NetClient>();
            player.Initialize(data, playerIndex, uid);
            clientGO.GetComponent<NetworkObject>().SpawnWithOwnership(clientID);
            return player;
        }

        public void CreateClientGameObject(NetClient client) => _ = CreateClientGameObjectAsync(client);

        private async UniTask CreateClientGameObjectAsync(NetClient client)
        {
            while(!client.IsSpawned || !client.Data.IsBuildSet) {
                await UniTask.Yield();
                client.ConnectionTimeOut += Time.deltaTime;
                if(client.ConnectionTimeOut < connectionSpawnTimeout)
                    continue;
                
                Debug.LogError($"Failed to initialize client '{client.Index}' - timed out.");
                KickPlayer(client, "Client initialization timed out.");
                return;
            }
            client.ConnectionTimeOut = 0.0f;
            GameServices.PlayerSpawn.SpawnPlayer(client);
        }

        public void KickPlayer(NetClient client, string reason = null)
        {
            if(!NetworkManager.Singleton.IsServer)
                return;

            // Workaround - with Facepunch transport, delete player objects manually on kick.
            if(TransportType == Transports.Facepunch) {
                if(client.PlayerController != null) { 
                    client.PlayerController.GetComponent<NetworkObject>().Despawn();
                }
                client.GetComponent<NetworkObject>().Despawn();
            }
            ConnectionService.OnKickPlayer(client, reason);
        }

        public NetScene CreateNetScene(MapConfig config, int seed)
        {
            if(!IsServer) {
                Debug.LogError($"Only the server can instantiate {nameof(NetScene)}.");
                return null;
            }
            if(NetScene != null) {
                NetScene.GetComponent<NetworkObject>().Despawn();
                NetScene = null;
            }
            if(config == null) {
                Debug.LogException(new ArgumentNullException(nameof(config)));
                return null;
            }
            NetScene netScene = Instantiate(netProcGenScenePrefab).GetComponent<NetScene>();
            NetScene          = netScene;
            netScene.SetValues(config, seed);
            netScene.GetComponent<NetworkObject>().Spawn();
            return netScene;
        }

        private void InitGlobalNetObject() => Instantiate(globalNetPrefab).GetComponent<NetworkObject>().Spawn();

        private static void CloseTransport()
        {
            TransportType    = Transports.None;
            CurrentTransport = null;
            CurrentHandler   = null;
        }

        public static void ResetState()
        {
            Game.FireResetCancellation();
            GameCallbacks.Access.OnResetGameState(Game.AccessToken);
            if(!Instance.UseSteam) {
                // Steam transport is always active, to show player count.
                CloseTransport();
            }
            ConnectionState          = GameConnectionState.Disconnected;
            LobbyUIScript.BlockStart = false;
            PersistenceManager.UnloadSnapshot();
            if(NetworkManager.Singleton.IsListening) {
                NetworkManager.Singleton.Shutdown();
            }
            UnregisterCallbacks();
            ModalUIScript.CloseAll();
            try { 
                CurrentLobby?.Leave();
            } catch(Exception) { /* Ignore error. */ }
            Game.SetState(GameState.MainMenu);
        }

        public static void ErrorReset(bool openMenuScene = true)
        {
            Instance.Disconnected(loadMainMenu: Game.State == GameState.InGame);
        }

        public static void OnQuit()
        {
            _ = ReturnToMenu();
        }

        public void Disconnected(bool showMsg = false, bool loadMainMenu = true, bool animated = true)
        {
            if(Game.State == GameState.Quitting)
                return;
            
            if(loadMainMenu) {
                _ = ReturnToMenu(showMsg ? Msg.DisconnectedStr : null);
                return;
            }
            
            ResetState();
            if(showMsg) {
                ModalUIScript.ShowGeneric(Msg.DisconnectedStr);
            }
            if(UseSteam) {
                StartHandler(true);
            }
        }

        public void OnClientIntegrated(NetClient client, bool spawnImmediately = false)
        {
            Callbacks.Access.OnClientSceneIntegrateCompleted(callbackToken);
            if(IsServer && Game.State == GameState.InGame && spawnImmediately) {
                CreateClientGameObject(client);
            }
        }

        public void OnAllPeersIntegrated()
        {
            Callbacks.Access.OnSceneIntegrateEventCompleted(callbackToken);
        }

        public static async UniTask ReturnToMenu(LocalizedString message = null)
        {
            bool showLoading = Game.State != GameState.Lobby || Game.State != GameState.MainMenu;
            Game.SetState(GameState.Quitting);
            if(showLoading) {
                await LoadingUIScript.Show();
            }

            GameCallbacks.Access.OnGameQuit(Game.AccessToken);
            ResetState();
            await new LoadSequence()
                  .WithScene(Game.Instance.mainMenuScene, true)
                  .WithPoolDestruction(Instance.poolCollection)
                  .WithPoolInstantiation(Instance.poolCollection)
                  .Execute();
            
            ConnectionState = GameConnectionState.Disconnected;
            if(message != null) {
                ModalUIScript.ShowGeneric(message);
            }
            if(Instance.UseSteam) {
                Instance.StartHandler(true);
            }
            Game.SetState(GameState.MainMenu);
            if(showLoading) {
                await LoadingUIScript.Hide();
            }
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            if(NetworkManager.Singleton == null)
                return;
            
            NetworkManager.Singleton.Shutdown();
            while(NetworkManager.Singleton.ShutdownInProgress) { /* ignore */ }
        }
#endif

        public static void NotifyExpectingDisconnect()        => expectedDisconnect = true;
        public static bool IsClientConnecting(ulong clientID) => connecting.Contains(clientID);
        public static void NotifyConnectionFinished(ulong id) => connecting.Remove(id);
        public static void NotifyInLobby()                    => gameInProgressCheck = false;

        public static void NotifyServerStartedLoading()
        {
            if(Manager.IsServer)
                return;
            
            _ = LoadingUIScript.Show();
            LoadingUIScript.SetProgress(Msg.WaitingForServerStr, 0.0f);
        }

        public static async UniTask<AwaitSelfReturn> AwaitSelfConnection(CancellationToken ct)
        {
            initialSyncTime = 0.0f;
            while(!NetClient.SelfInitialSync) {
                await UniTask.Yield();
                if(ct.IsCancellationRequested)
                    return AwaitSelfReturn.Cancelled;
                
                initialSyncTime += Time.deltaTime;
                if(initialSyncTime > Instance.initialSyncTimeout)
                    return AwaitSelfReturn.Timeout;
            }
            return AwaitSelfReturn.Success;
        }

        async UniTask IJitOptimizerTask.Run()
        {
            StartHandler(false);
            NetworkManager.Singleton.StartHost();
            await UniTask.Yield();
            NetworkManager.Singleton.Shutdown(true);
            ResetState();
        }
    }
    
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")] // Setting to readonly breaks it for some reason.
    public class ConnectionData
    {
        public string Name;
        public bool ShowSupporterBadge;
        public ulong SupporterSecret;
        
        // ReSharper disable once UnusedMember.Global
        public ConnectionData() {}

        public ConnectionData(string name, bool showSupporterBadge, ulong supporterSecret)
        {
            Name               = name;
            ShowSupporterBadge = showSupporterBadge;
            SupporterSecret    = supporterSecret;
        }
    }

    public enum AwaitSelfReturn     { Success, Timeout, Cancelled }
    public enum GameConnectionState { Disconnected, Loading, Ready }
    public enum NetHandlers         { None, Unity, Steam }
    public enum Transports          { None, Unity, Facepunch }
    public enum GameType            { None, SinglePlayer, Multiplayer }
}
