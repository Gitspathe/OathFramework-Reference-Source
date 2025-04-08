using OathFramework.Core.Service;
using OathFramework.EntitySystem.Players;
using OathFramework.Pooling;
using System;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Networking
{
    public class NetGameRpcHelper : NetworkBehaviour
    {
        public static NetGameRpcHelper Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Instance = null;
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            PoolManager.OnNetworkInitialized();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PoolManager.OnNetworkClosed();
        }

        public static void NotifyInLobby(ulong id)
        {
            Instance.NotifyInLobbyClientRpc(Instance.RpcTarget.Single(id, RpcTargetUse.Temp));
        }

        public static void RequestPlayerSpawn()
        {
            Instance.RequestRespawnServerRpc();
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        public void NotifyConnectionFinishedClientRpc(ulong id, RpcParams rpcParams = default)
        {
            NetGame.NotifyConnectionFinished(id);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void NotifyConnectionFinishedServerRpc(RpcParams rpcParams = default)
        {
            NetGame.NotifyConnectionFinished(rpcParams.Receive.SenderClientId);
            NotifyConnectionFinishedClientRpc(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
        public void NotifyInLobbyClientRpc(RpcParams rpcParams = default)
        {
            NetGame.NotifyInLobby();
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        public void NotifyServerStartedLoadingClientRpc()
        {
            NetGame.NotifyServerStartedLoading();
        }
        
        [Rpc(SendTo.Server)]
        private void RequestRespawnServerRpc(RpcParams rpcParams = default)
        {
            ulong id = rpcParams.Receive.SenderClientId;
            if(PlayerManager.TryGetPlayerFromNetID(id, out NetClient client)) {
                PlayerSpawnService.Instance.SpawnPlayer(client);
            }
        }
    }
}
