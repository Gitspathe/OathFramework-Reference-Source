using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.EntitySystem.Players;
using OathFramework.ProcGen;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Networking
{
    public class NetScene : NetworkBehaviour, IPlayerDisconnectedCallback
    {
        [SerializeField] private float timeout = 60.0f;

        private NetworkVariable<bool> netEveryoneLoaded = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );
        
        private NetworkVariable<ushort> netMapConfig = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );
        
        private NetworkVariable<uint> netMapSeed = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        private ushort initMapConfig;
        private uint initMapSeed;

        private List<NetClient> pending = new();
        private float curTimeout;
        
        public ushort MapConfig => netMapConfig.Value;
        public uint MapSeed     => netMapSeed.Value;

        private void Awake()
        {
            GameCallbacks.Register((IPlayerDisconnectedCallback)this);
        }
        
        public override void OnDestroy()
        {
            GameCallbacks.Unregister((IPlayerDisconnectedCallback)this);
        }

        public override void OnNetworkSpawn()
        {
            if(IsServer) {
                netMapConfig.Value = initMapConfig;
                netMapSeed.Value   = initMapSeed;
                foreach(NetClient client in PlayerManager.Players) {
                    pending.Add(client);
                }
            }
            if(!(SceneScript.Main is ProcGenSceneScript procGen)) {
                Debug.LogError($"Scene is not a {nameof(ProcGenSceneScript)} scene.");
                return;
            }
            if(!ProcGenManager.TryGet(netMapConfig.Value, out MapConfig conf)) {
                Debug.LogError($"Failed to find {nameof(MapConfig)} with NetID {netMapConfig.Value}.");
                return;
            }
            procGen.NetSceneSpawned(this);
        }

        public void SetValues(MapConfig config, uint seed)
        {
            if(IsServer) {
                Debug.LogError($"Only the server can set {nameof(NetScene)} values.");
                return;
            }
            initMapConfig = config.ID;
            initMapSeed   = seed;
        }

        public void OnSelfIntegrated()
        {
            if(!IsServer) {
                NotifyIntegratedServerRpc();
            } else {
                pending.Remove(NetClient.Self);
            }
        }

        public async UniTask<bool> WaitForEveryone()
        {
            if(IsServer) {
                while(pending.Count > 0) {
                    await UniTask.Yield();
                    curTimeout += Time.unscaledDeltaTime;
                    if(curTimeout >= timeout) {
                        KickPending();
                        return false;
                    }
                }
                netEveryoneLoaded.Value = true;
                return true;
            }
            
            // Client only.
            while(!netEveryoneLoaded.Value) {
                await UniTask.Yield();
            }
            return true;
        }

        private void KickPending()
        {
            List<NetClient> copy = new();
            copy.AddRange(pending);
            foreach(NetClient client in copy) {
                NetGame.Instance.KickPlayer(client, "Timed out.");
            }
            pending.Clear();
        }
        
        void IPlayerDisconnectedCallback.OnPlayerDisconnected(NetClient client)
        {
            pending.Remove(client);
        }

        [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void NotifyIntegratedServerRpc(RpcParams rpcParams = default)
        {
            if(PlayerManager.TryGetPlayerFromNetID(rpcParams.Receive.SenderClientId, out NetClient client)) {
                bool existed = pending.Remove(client);
                NetGame.Instance.OnClientIntegrated(client, !existed);
            }
        }
    }
}
