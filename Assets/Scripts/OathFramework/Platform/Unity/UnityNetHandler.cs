using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.UI;
using System;
using System.Text;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OathFramework.Platform.Unity
{
    public class UnityNetHandler : NetHandler
    {
        public override NetHandlers Type => NetHandlers.Unity;
        public override NetLobbyBase NetLobby { get; protected set; }

        public override NetHandler Initialize()
        {
            return this;
        }

        public override void StartTransport()
        {
            if(NetGame.TransportType == Transports.Unity)
                return;
            if(NetGame.CurrentTransport != null) 
                Object.Destroy(NetGame.CurrentTransport);

            GameObject transportGO                 = Object.Instantiate(NetGame.Instance.UnityTransportPrefab, NetworkManager.Singleton.transform);
            UnityTransport transport               = transportGO.GetComponent<UnityTransport>();
            NetGame.NetworkConfig.NetworkTransport = transport;
            NetGame.CurrentTransport               = transportGO;
            NetGame.TransportType                  = Transports.Unity;
        }

        public override void ResetState()
        {
            
        }

        public override async UniTask<bool> StartMultiplayerHost(CancellationToken ct)
        {
            CancellationTokenSource cancelBtnCts = new();
            CancellationTokenSource cts          = CancellationTokenSource.CreateLinkedTokenSource(ct, cancelBtnCts.Token);
            ModalConfig modal = ModalUIScript.ShowGeneric(NetGame.Msg.CreatingLobbyStr, NetGame.Msg.CancelStr, onButtonClicked: () => CancelPressed(cts));

            try {
                ConnectionData conData = new(
                    NetGame.UDPMultiplayerName, 
                    SupporterDLCUtil.HasSupporterDLC,
                    SupporterDLCUtil.Secret
                );
                NetGame.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(conData));
                
                // Ironically 'StartMultiplayerHost' is also called when the player starts a single-player game.
                // So if they did select single-player, bind to localhost instead of the listen address.
                if(NetGame.GameType == GameType.SinglePlayer) {
                    NetGame.UnityTransport.SetConnectionData("127.0.0.1", 8500, "127.0.0.1");
                } else {
                    NetGame.UnityTransport.SetConnectionData("0.0.0.0", 8500, "0.0.0.0");
                }
                Game.SetState(GameState.Lobby);
                if(!NetworkManager.Singleton.StartHost())
                    throw new Exception("Failed to start host.");
                
                NetGame.RegisterCallbacks();
                NetGame.ConnectionState = GameConnectionState.Ready;
                AwaitSelfReturn awaitRet = await NetGame.AwaitSelfConnection(cts.Token);
                modal?.Close();
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
            } finally {
                modal?.Close();
            }
        }

        public override async UniTask<bool> ConnectQuickMultiplayer(CancellationToken ct)
        {
            CancellationTokenSource cancelBtnCts = new();
            CancellationTokenSource cts          = CancellationTokenSource.CreateLinkedTokenSource(ct, cancelBtnCts.Token);
            ModalConfig modal = ModalUIScript.ShowGeneric(NetGame.Msg.JoiningLobbyStr, NetGame.Msg.CancelStr, onButtonClicked: () => CancelPressed(cts));
            NetGame.UnityTransport.SetConnectionData("127.0.0.1", 8500);

            try {
                ConnectionData conData = new(
                    NetGame.UDPMultiplayerName, 
                    SupporterDLCUtil.HasSupporterDLC,
                    SupporterDLCUtil.Secret
                );
                NetGame.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(conData));
                if(!NetworkManager.Singleton.StartClient())
                    throw new Exception("Failed to start client.");

                NetGame.RegisterCallbacks();
                AwaitSelfReturn awaitRet = await NetGame.AwaitSelfConnection(cts.Token);
                modal?.Close();
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
                ConnectionData conData = new(
                    NetGame.UDPMultiplayerName, 
                    SupporterDLCUtil.HasSupporterDLC,
                    SupporterDLCUtil.Secret
                );
                NetGame.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(conData));
                NetGame.UnityTransport.SetConnectionData(code, 8500);
                if(!NetworkManager.Singleton.StartClient())
                    throw new Exception("Failed to start client.");

                NetGame.RegisterCallbacks();
                AwaitSelfReturn awaitRet = await NetGame.AwaitSelfConnection(cts.Token);
                modal?.Close();
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
            } finally {
                modal?.Close();
            }
        }

        public override void OnKickPlayer(NetClient client, string reason = null)
        {
            
        }
        
        private static void CancelPressed(CancellationTokenSource cts)
        {
            cts.Cancel();
            cts.Dispose();
            NetGame.ErrorReset(false);
        }
    }
}
