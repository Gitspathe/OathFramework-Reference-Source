using Cysharp.Threading.Tasks;
using OathFramework.EntitySystem.Attributes;
using OathFramework.Networking;
using OathFramework.Persistence;
using OathFramework.Progression;
using OathFramework.UI.Info;
using System;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.Players
{

    [RequireComponent(typeof(NetClient))]
    public class PlayerBuildDataHandler : NetworkBehaviour
    {
        [NonSerialized] public PlayerBuildData CurrentBuild;

        private NetworkVariable<PlayerBuildData> netBuildData = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        private NetworkVariable<bool> netIsOverride = new(
            value: false,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        private NetworkVariable<bool> netBuildSet = new(
            value: false,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        public bool IsOverride         => netIsOverride.Value;
        public bool IsBuildSet         => netBuildSet.Value; 
        public bool PendingInitialSync { get; private set; }
        public NetClient Client        { get; private set; }

        private void Awake()
        {
            Client       = GetComponent<NetClient>();
            CurrentBuild = PlayerBuildData.Default;
        }

        public override void OnNetworkSpawn()
        {
            _ = LoadTask();
        }

        private async UniTask LoadTask()
        {
            await GlobalNetInfo.AwaitInitialization();
            bool hasProxy = ProxyDatabase<PlayerProxyComponent>.TryGetProxy(Client.UniqueID, out PlayerProxyComponent proxy);
            // TODO: Handle this in a more robust way. I.e, notify player if they aren't in the loaded snapshot, etc.
            if(GlobalNetInfo.UsingSnapshot && hasProxy) {
                AssignOverrideBuild(proxy);
            } else {
                AssignPlayerBuild();
            }
        }

        private void AssignPlayerBuild()
        {
            if(IsOwner) {
                SetBuild(ProgressionManager.Profile.CurrentLoadout);
                SetBuildInternal(netBuildData.Value, false);
                PendingInitialSync = !IsServer;
            } else if(!IsServer) {
                SetBuildInternal(netBuildData.Value, false);
            }
        }

        private void AssignOverrideBuild(PlayerProxyComponent proxy)
        {
            if(IsOwner) {
                PendingInitialSync = !IsServer;
            }
            if(IsServer) {
                SetBuildInternal(proxy.BuildData, true);
                SyncBuildClientRpc(proxy.BuildData, true);
            } else {
                SetBuildInternal(netBuildData.Value, true);
            }
        }

        public void SetBuild(PlayerBuildData build, bool isOverride = false)
        {
            if(!IsOwner)
                return;

            if(!IsServer) {
                SyncBuildServerRpc(build, isOverride);
                return;
            }
            SetBuildInternal(build, isOverride);
            SyncBuildClientRpc(build, isOverride);
        }

        private void SetBuildInternal(PlayerBuildData build, bool isOverride)
        {
            CurrentBuild = build;
            if(IsServer) {
                netIsOverride.Value = isOverride;
                netBuildSet.Value   = true;
                netBuildData.Value  = build;
            }
            if(Client.PlayerController != null) {
                Apply();
            }
            UIInfoManager.TickPlayerInfo(Client);
        }

        public void Apply()
        {
            PlayerController controller = Client.PlayerController;
            Entity entity               = controller.Entity;

            // TODO:
            // Step 1: Apply weapon stats to weapons (need to write per-player weapon manager)

            // Step 2: Apply attributes.
            AttributeManager.Apply(in CurrentBuild, entity.States, true);

            // Step 3: Apply perks.
            controller.Perks.Assign(in CurrentBuild);

            // Step 4: Apply states.
            entity.States.ApplyStats(true);

            // Step 5: Equip weapons.
            controller.Equipment.UpdateHeldWeapons(in CurrentBuild);

            // Step 6: Equip abilities.
            controller.Abilities.Assign(in CurrentBuild);
            
            // Sync owner health and stamina.
            entity.SyncNetVars();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = true)]
        private void SyncBuildServerRpc(PlayerBuildData data, bool isOverride, RpcParams rpcParams = default)
        {
            SetBuildInternal(data, isOverride);
            SyncBuildClientRpc(data, isOverride);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        private void SyncBuildClientRpc(PlayerBuildData data, bool isOverride, RpcParams rpcParams = default)
        {
            if(IsServer)
                return;

            PendingInitialSync = false;
            SetBuildInternal(data, isOverride);
        }
    }

}
