using OathFramework.EntitySystem;
using OathFramework.Pooling;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Effects
{
    [RequireComponent(typeof(Effect))]
    public class NetEffect : NetworkBehaviour, IPoolableComponent
    {
        [field: SerializeField] public bool HideNetworkedForSource { get; private set; }
        
        private NetworkVariable<byte> netCurrentModelSpot = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server,
            value: 0
        );

        private NetworkVariable<NetworkBehaviourReference> netModelSockets = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server,
            value: default
        );
        
        private NetworkVariable<NetworkBehaviourReference> netSource = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server,
            value: default
        );

        private NetworkVariable<ushort> netExtraData = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server,
            value: default
        );
        
        public Effect Effect                 { get; private set; }
        public byte? Index                   { get; set; }
        public PoolableGameObject PoolableGO { get; set; }
        
        private void Awake()
        {
            Effect = GetComponent<Effect>(); 
        }

        public override void OnNetworkSpawn()
        {
            Effect.Local = false;
            if(IsServer && HideNetworkedForSource) {
                HandleNetworkHide();
            }

            EntityModelSocketHandler eSockets = Effect.Sockets as EntityModelSocketHandler;
            if(IsServer) {
                netSource.Value           = Effect.Source as Entity;
                netModelSockets.Value     = eSockets?.Entity;
                netCurrentModelSpot.Value = Effect.CurrentSpot;
                netExtraData.Value        = Effect.ExtraData;
                Effect.OnSpawned();
                return;
            }
            
            if(netSource.Value.TryGet(out Entity entity)) {
                SetSource(entity);
            }
            if(netModelSockets.Value.TryGet(out Entity socketsEntity) && socketsEntity.EntityModel != null && socketsEntity.Sockets != null) {
                SetSockets(socketsEntity.Sockets as EntityModelSocketHandler, netCurrentModelSpot.Value);
            }
            SetExtraData(netExtraData.Value);
            Effect.OnSpawned();
        }

        public override void OnNetworkDespawn()
        {
            if(!ReferenceEquals(Effect.Sockets, null)) {
                Effect.Sockets.RemovePlug(Effect, ModelPlugRemoveBehavior.Dissipate);
            }
        }

        private void HandleNetworkHide()
        {
            if(Effect.Source == null || !(Effect.Source is Entity dEntity) || dEntity.IsOwnedByServer || !dEntity.IsPlayer)
                return;
            
            GetComponent<NetworkObject>().NetworkHide(dEntity.OwnerClientId);
        }

        public void SetSockets(EntityModelSocketHandler sockets, byte modelSpot)
        {
            if(IsServer) {
                netModelSockets.Value     = sockets.Entity;
                netCurrentModelSpot.Value = modelSpot;
            }
            if(!ReferenceEquals(Effect.Sockets, null)) {
                Effect.Sockets.RemovePlug(Effect, ModelPlugRemoveBehavior.Instant);
            }
            if(!ReferenceEquals(sockets, null)) {
                sockets.AddPlug(modelSpot, Effect);
            }
            Effect.CurrentSpot = modelSpot;
            Effect.Sockets     = sockets;
        }

        public void SetSource(Entity source)
        {
            if(IsServer) {
                netSource.Value = source;
            }
            Effect.Source = source;
        }

        public void SetExtraData(ushort extraData)
        {
            if(IsServer) {
                netExtraData.Value = extraData;
            }
            Effect.ExtraData = extraData;
        }
        
        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Reliable)]
        public void SyncPersistenceNotOwnerRpc(Effect.Data data)
        {
            Effect.SetParams(data);
        }

        void IPoolableComponent.OnRetrieve()
        {
            Index = null;
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            if(IsServer) {
                netSource.Value           = null;
                netModelSockets.Value     = null;
                netCurrentModelSpot.Value = 0;
            }
        }
    }
}
