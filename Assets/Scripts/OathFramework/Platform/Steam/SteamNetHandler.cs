#if !UNITY_IOS && !UNITY_ANDROID
using Cysharp.Threading.Tasks;
using Netcode.Transports.Facepunch;
using OathFramework.Core;
using OathFramework.EntitySystem.Players;
using OathFramework.Networking;
using OathFramework.UI;
using Steamworks;
using Steamworks.Data;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OathFramework.Platform.Steam
{
    public class SteamNetHandler : NetHandler
    {
        public override NetHandlers Type => NetHandlers.Steam;
        public override NetLobbyBase NetLobby { get; protected set; }
        
        private static readonly System.Random Random = new();
        
        public override NetHandler Initialize()
        {
            return this;
        }

        public override void StartTransport()
        {
            if(NetGame.TransportType == Transports.Facepunch)
                return;
            if(NetGame.CurrentTransport != null) 
                Object.Destroy(NetGame.CurrentTransport);
            
            GameObject transportGO                 = Object.Instantiate(NetGame.Instance.FacepunchTransportPrefab, NetworkManager.Singleton.transform);
            FacepunchTransport transport           = transportGO.GetComponent<FacepunchTransport>();
            NetGame.NetworkConfig.NetworkTransport = transport;
            NetGame.CurrentTransport               = transportGO;
            NetGame.TransportType                  = Transports.Facepunch;
        }

        public override void ResetState()
        {
            
        }

        public override async UniTask<bool> StartMultiplayerHost(CancellationToken ct)
        {
            CancellationTokenSource cancelBtnCts = new();
            CancellationTokenSource cts          = CancellationTokenSource.CreateLinkedTokenSource(ct, cancelBtnCts.Token);
            ConnectionData conData = new(
                SteamClient.Name, 
                SupporterDLCUtil.HasSupporterDLC,
                SupporterDLCUtil.Secret
            );
            NetGame.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(conData));
            Game.SetState(GameState.Lobby);
            ModalConfig modal = ModalUIScript.ShowGeneric(NetGame.Msg.CreatingLobbyStr, NetGame.Msg.CancelStr, onButtonClicked: () => CancelPressed(cts));

            try {
                if(!NetworkManager.Singleton.StartHost())
                    throw new Exception("Failed to start host.");

                NetGame.RegisterCallbacks();
                (CreateLobbyResult result, SteamNetLobby lobby) = await CreateLobby(cts.Token);
                switch(result) {
                    case CreateLobbyResult.Success: {
                        NetGame.ConnectionState  = GameConnectionState.Ready;
                        NetGame.CurrentCode      = lobby.Code;
                        NetLobby                 = lobby;
                        AwaitSelfReturn awaitRet = await NetGame.AwaitSelfConnection(cts.Token);
                        modal.Close();
                        switch(awaitRet) {
                            case AwaitSelfReturn.Timeout:
                                throw new Exception("timed out.");
                            case AwaitSelfReturn.Cancelled:
                                NetGame.ErrorReset();
                                return false;

                            case AwaitSelfReturn.Success:
                            default:
                                return true;
                        }
                    }
                    case CreateLobbyResult.Fail: {
                        NetGame.CurrentCode = null;
                        ModalUIScript.ShowGeneric(NetGame.Msg.FailedToCreateLobbyStr);
                        return false;
                    }
                    case CreateLobbyResult.Cancelled: {
                        NetGame.CurrentCode = null;
                        NetGame.ErrorReset();
                        return false;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } finally {
                modal.Close();
            }
        }

        public override async UniTask<bool> ConnectQuickMultiplayer(CancellationToken ct)
        {
            CancellationTokenSource cancelBtnCts = new();
            CancellationTokenSource cts          = CancellationTokenSource.CreateLinkedTokenSource(ct, cancelBtnCts.Token);
            ModalConfig modal = ModalUIScript.ShowGeneric(NetGame.Msg.JoiningLobbyStr, NetGame.Msg.CancelStr, onButtonClicked: () => CancelPressed(cts));

            try {
                (JoinLobbyResult result, SteamNetLobby lobby) = await JoinRandomLobby(cts.Token);
                switch(result) {
                    case JoinLobbyResult.Success: {
                        NetGame.FacepunchTransport.targetSteamId = lobby.OwnerID;
                        ConnectionData conData = new(
                            SteamClient.Name, 
                            SupporterDLCUtil.HasSupporterDLC,
                            SupporterDLCUtil.Secret
                        );
                        NetGame.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(conData));
                        if(!NetworkManager.Singleton.StartClient())
                            throw new Exception("Failed to start client.");

                        NetGame.RegisterCallbacks();
                        NetGame.CurrentCode      = null;
                        NetLobby                 = lobby;
                        AwaitSelfReturn awaitRet = await NetGame.AwaitSelfConnection(cts.Token);
                        modal.Close();
                        switch(awaitRet) {
                            case AwaitSelfReturn.Timeout:
                                throw new Exception("timed out.");
                            case AwaitSelfReturn.Cancelled:
                                NetGame.ErrorReset();
                                return false;

                            case AwaitSelfReturn.Success:
                            default:
                                return true;
                        }
                    }

                    case JoinLobbyResult.Timeout:
                    case JoinLobbyResult.NotFound:
                    case JoinLobbyResult.Full: {
                        NetGame.CurrentCode = null;
                        NetGame.ErrorReset();
                        PrintConnectionError(result, true);
                        return false;
                    }
                    case JoinLobbyResult.Cancelled: {
                        NetGame.CurrentCode = null;
                        NetGame.ErrorReset();
                        return false;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } finally {
                modal?.Close();
            }
        }

        public override async UniTask<bool> ConnectMultiplayer(string code, CancellationToken ct)
        {
            CancellationTokenSource cancelBtnCts = new();
            CancellationTokenSource cts          = CancellationTokenSource.CreateLinkedTokenSource(ct, cancelBtnCts.Token);
            ModalConfig modal = ModalUIScript.ShowGeneric(NetGame.Msg.JoiningLobbyStr, NetGame.Msg.CancelStr, onButtonClicked: () => CancelPressed(cts));

            try {
                (JoinLobbyResult result, SteamNetLobby lobby) = await JoinLobby(code, ct);
                switch(result) {
                    case JoinLobbyResult.Success: {
                        NetGame.FacepunchTransport.targetSteamId = lobby.OwnerID;
                        ConnectionData conData = new(
                            SteamClient.Name, 
                            SupporterDLCUtil.HasSupporterDLC,
                            SupporterDLCUtil.Secret
                        );
                        NetGame.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(conData));
                        if(!NetworkManager.Singleton.StartClient())
                            throw new Exception("Failed to start client.");

                        NetGame.RegisterCallbacks();
                        NetGame.CurrentCode      = code;
                        NetLobby                 = lobby;
                        AwaitSelfReturn awaitRet = await NetGame.AwaitSelfConnection(cts.Token);
                        modal.Close();
                        switch(awaitRet) {
                            case AwaitSelfReturn.Timeout:
                                throw new Exception("timed out.");
                            case AwaitSelfReturn.Cancelled:
                                NetGame.ErrorReset();
                                return false;

                            case AwaitSelfReturn.Success:
                            default:
                                return true;
                        }
                    }
                        
                    case JoinLobbyResult.Timeout:
                    case JoinLobbyResult.NotFound:
                    case JoinLobbyResult.Full: {
                        NetGame.CurrentCode = null;
                        NetGame.ErrorReset();
                        PrintConnectionError(result, false);
                        return false;
                    }
                    case JoinLobbyResult.Cancelled: {
                        NetGame.CurrentCode = null;
                        NetGame.ErrorReset();
                        return false;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } finally {
                modal?.Close();
            }
        }

        public override void OnKickPlayer(NetClient client, string reason = null)
        {
            
        }
        
        public static async UniTask<(int, int)> GetPlayerCount(CancellationToken ct)
        {
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.RequestAsync();
            if(ct.IsCancellationRequested || lobbies == null)
                return (0, 0);

            int totalPlayers    = 0;
            int joinableLobbies = 0;
            foreach(Lobby i in lobbies) {
                totalPlayers += i.MemberCount;
                if(i.GetData("isPublic") == "false")
                    continue;
                if(i.MemberCount == i.MaxMembers)
                    continue;
                
                joinableLobbies += 1;
            }
            return (joinableLobbies, totalPlayers);
        }

        private static async UniTask<SteamNetLobby> GetRandomLobby(CancellationToken ct)
        {
            if(NetGame.TransportType != Transports.Facepunch) {
                Debug.LogError("Transport type is not Facepunch Steamworks.");
                return null;
            }

            LobbyQuery query = SteamMatchmaking.LobbyList.WithKeyValue("isPublic", "true");
            query.WithSlotsAvailable(1);
            Lobby[] lobbies = await query.RequestAsync();
            if(ct.IsCancellationRequested)
                return null;
            
            return lobbies != null && lobbies.Length > 0 ? new SteamNetLobby(lobbies[UnityEngine.Random.Range(0, lobbies.Length)]) : null;
        }

        private static async UniTask<SteamNetLobby> GetLobby(string code, CancellationToken ct)
        {
            if(NetGame.TransportType != Transports.Facepunch) {
                Debug.LogError("Transport type is not Facepunch Steamworks.");
                return null;
            }
            
            LobbyQuery query = SteamMatchmaking.LobbyList.WithKeyValue("code", code);
            Lobby[] lobbies  = await query.RequestAsync();
            if(ct.IsCancellationRequested) {
                return null;
            }
            return lobbies != null && lobbies.Length > 0 ? new SteamNetLobby(lobbies[0]) : null;
        }

        public static async UniTask<(JoinLobbyResult, SteamNetLobby)> JoinLobby(string code, CancellationToken ct)
        {
            if(NetGame.TransportType != Transports.Facepunch) { 
                Debug.LogError("Transport type is not Facepunch Steamworks.");
                return (JoinLobbyResult.NotFound, null);
            }

            SteamNetLobby lobby = await GetLobby(code, ct);
            if(lobby == null) 
                return (JoinLobbyResult.NotFound, null);
            if(lobby.SteamLobby.MemberCount == lobby.SteamLobby.MaxMembers)
                return (JoinLobbyResult.Full, null);
            
            try {
                Lobby? joinedLobby = await SteamMatchmaking.JoinLobbyAsync(lobby.LobbyID);
                if(!ct.IsCancellationRequested)
                    return (JoinLobbyResult.Success, new SteamNetLobby(joinedLobby.Value));

                joinedLobby?.Leave();
                return (JoinLobbyResult.Cancelled, null);
            } catch(TaskCanceledException) {
                return (JoinLobbyResult.NotFound, null);
            }
        }

        public static async UniTask<(JoinLobbyResult, SteamNetLobby)> JoinRandomLobby(CancellationToken ct)
        {
            if(NetGame.TransportType != Transports.Facepunch) {
                Debug.LogError("Transport type is not Facepunch Steamworks.");
                return (JoinLobbyResult.NotFound, null);
            }

            SteamNetLobby lobby = await GetRandomLobby(ct);
            if(lobby == null)
                return (JoinLobbyResult.NotFound, null);
            if(lobby.SteamLobby.MemberCount == lobby.SteamLobby.MaxMembers)
                return (JoinLobbyResult.Full, null);

            try {
                Lobby? joinedLobby = await SteamMatchmaking.JoinLobbyAsync(lobby.LobbyID);
                if(!ct.IsCancellationRequested)
                    return (JoinLobbyResult.Success, new SteamNetLobby(joinedLobby.Value));
                
                joinedLobby?.Leave();
                return (JoinLobbyResult.Cancelled, null);
            } catch(TaskCanceledException) {
                return (JoinLobbyResult.NotFound, null);
            }
        }

        public static async UniTask<(CreateLobbyResult, SteamNetLobby)> CreateLobby(CancellationToken ct)
        {
            const int maxAttempts = 5;
            int attempts          = 1;
            while(true) {
                if(attempts == maxAttempts)
                    return (CreateLobbyResult.Fail, null);
                
                string randCode        = GenerateRandomCode();
                SteamNetLobby theLobby = await GetLobby(randCode, ct);
                if(ct.IsCancellationRequested)
                    return (CreateLobbyResult.Cancelled, null);
                
                if(theLobby != null) {
                    attempts += 1;
                    continue;
                }

                Lobby? steamLobby = await SteamMatchmaking.CreateLobbyAsync(PlayerManager.MaxPlayers);
                if(ct.IsCancellationRequested) {
                    steamLobby?.Leave();
                    return (CreateLobbyResult.Cancelled, null);
                }
                if(!steamLobby.HasValue)
                    continue;

                SteamNetLobby newLobby = new(steamLobby.Value);
                newLobby.SetCode(randCode);
                newLobby.SetPublic(true);
                newLobby.MakeListing();
                theLobby = newLobby;
                return (CreateLobbyResult.Success, theLobby);
            }
        }

        private static void CancelPressed(CancellationTokenSource cts)
        {
            cts.Cancel();
            cts.Dispose();
            NetGame.ErrorReset();
        }

        private static string GenerateRandomCode()
        {
            string s = "";
            lock(Random) {
                for (int i = 0; i < 4; i++) {
                    s += Random.Next(0, 9).ToString();
                }
            }
            return s;
        }
    }
}
#endif
