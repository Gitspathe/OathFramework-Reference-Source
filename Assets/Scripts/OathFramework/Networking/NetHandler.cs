using Cysharp.Threading.Tasks;
using OathFramework.Platform;
using OathFramework.UI;
using System;
using System.Threading;

namespace OathFramework.Networking
{
    public abstract class NetHandler
    {
        public abstract NetHandlers Type { get; }
        public abstract NetLobbyBase NetLobby { get; protected set; }
        
        public abstract NetHandler Initialize();
        public abstract void StartTransport();
        public abstract void ResetState();
        public abstract UniTask<bool> StartMultiplayerHost(CancellationToken ct);
        public abstract UniTask<bool> ConnectQuickMultiplayer(CancellationToken ct);
        public abstract UniTask<bool> ConnectMultiplayer(string code, CancellationToken ct);
        public abstract void OnKickPlayer(NetClient client, string reason = null);
        
        public static void PrintConnectionError(JoinLobbyResult result, bool randomLobby)
        {
            switch(result) {
                case JoinLobbyResult.Full:
                    ModalUIScript.ShowGeneric(NetGame.Msg.LobbyFullStr);
                    break;
                case JoinLobbyResult.NotFound:
                    ModalUIScript.ShowGeneric(randomLobby ? NetGame.Msg.NoRandomLobbyStr : NetGame.Msg.EmptyLobbyCodeStr);
                    break;
                case JoinLobbyResult.Timeout:
                    ModalUIScript.ShowGeneric(NetGame.Msg.LobbyTimedOutStr);
                    break;
                
                case JoinLobbyResult.Success:
                case JoinLobbyResult.Cancelled:
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }
    }
}
