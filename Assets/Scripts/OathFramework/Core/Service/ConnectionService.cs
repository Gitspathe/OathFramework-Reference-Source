using UnityEngine;
using Unity.Netcode;
using OathFramework.EntitySystem.Players;
using OathFramework.Networking;

namespace OathFramework.Core.Service 
{ 

    public class ConnectionService : MonoBehaviour
    {
        public static ConnectionService Instance { get; private set; }

        public ConnectionService Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(ConnectionService)} singletons.");
                return null;
            }

            Instance = this;
            return Instance;
        }

        public static void OnKickPlayer(NetClient client, string reason = null)
        { 
            if(NetGame.NetHandlerType == NetHandlers.Steam) {
                // Workaround - with Facepunch transport, call specific disconnect and delete player GOs manually.
#if !UNITY_IOS && !UNITY_ANDROID
                NetGame.FacepunchTransport.DisconnectRemoteClient(client.OwnerClientId);
#endif
            } else {
                NetworkManager.Singleton.DisconnectClient(client.OwnerClientId, !string.IsNullOrEmpty(reason) ? "Kicked" : $"Kicked (Reason: {reason})");
            }

            GameServices.Notification.PlayerKicked(client.Name);
        }

        public static void OnPlayerClientSpawned(NetClient client)
        {
            PlayerManager.ClientConnected(client);
        }

        public static void OnPlayerClientDespawned(NetClient client)
        {
            PlayerManager.ClientDisconnected(client);
        }
    }

}
