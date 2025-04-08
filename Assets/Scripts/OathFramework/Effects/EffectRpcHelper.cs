using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Utility;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Effects
{
    public class EffectRpcHelper : NetworkBehaviour
    {
        public static EffectRpcHelper Instance { get; private set; }

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

        private void ServerSpawnEffect(
            ulong senderID,
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Entity source,
            Vector3 position,
            HalfQuaternion? rotation   = null, 
            HalfVector3? scale         = null,
            ModelSocketHandler sockets = null,
            byte modelSpot             = 0)
        {
            Effect effect = EffectManager.Retrieve(
                effectID,
                source, 
                position, 
                rotation == null ? null : (Quaternion)rotation, 
                scale == null ? null : (Vector3)scale,
                sockets, 
                modelSpot,
                extraData == 0 ? null : extraData
            );
            effect.PassTime((float)(clientTime - NetworkManager.ServerTime.Time));
            NotifyEffectSpawnedRpc(effectID, index, RpcTarget.Single(senderID, RpcTargetUse.Temp));
        }
        
        // *******************************************************************************************************************************************
        // CLIENT -> SERVER RPC
        // *******************************************************************************************************************************************
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void SpawnEffectServerRpc(
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Vector3 position, 
            RpcParams rpcParams = default)
        {
            ServerSpawnEffect(rpcParams.Receive.SenderClientId, clientTime, effectID, extraData, index, null, position);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void SpawnEffectExtServerRpc(
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Vector3 position, 
            HalfQuaternion rotation, 
            HalfVector3 scale,
            RpcParams rpcParams = default)
        {
            ServerSpawnEffect(rpcParams.Receive.SenderClientId, clientTime, effectID, extraData, index, null, position, rotation, scale);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void SpawnEffectSourceServerRpc(
            NetworkBehaviourReference source,
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Vector3 position, 
            RpcParams rpcParams = default)
        {
            if(!source.TryGet(out Entity entity)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Failed to unpack the source entity at {nameof(SpawnEffectSourceServerRpc)}.");
                }
                return;
            }
            ServerSpawnEffect(rpcParams.Receive.SenderClientId, clientTime, effectID, extraData, index, entity, position);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void SpawnEffectSourceExtServerRpc(
            NetworkBehaviourReference source,
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Vector3 position, 
            HalfQuaternion rotation, 
            HalfVector3 scale,
            RpcParams rpcParams = default)
        {
            if(!source.TryGet(out Entity entity)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Failed to unpack the source entity at {nameof(SpawnEffectSourceExtServerRpc)}.");
                }
                return;
            }
            ServerSpawnEffect(rpcParams.Receive.SenderClientId, clientTime, effectID, extraData, index, entity, position, rotation, scale);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void SpawnEffectSocketsServerRpc(
            NetworkBehaviourReference sockets,
            byte modelSpot,
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Vector3 position, 
            RpcParams rpcParams = default)
        {
            if(!sockets.TryGet(out Entity socketsEntity) || socketsEntity.EntityModel == null || socketsEntity.EntityModel.Sockets == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Failed to unpack sockets source entity at {nameof(SpawnEffectSocketsServerRpc)}.");
                }
                return;
            }
            ModelSocketHandler handler = socketsEntity.EntityModel.Sockets;
            ServerSpawnEffect(rpcParams.Receive.SenderClientId, clientTime, effectID, extraData, index, null, position, sockets: handler, modelSpot: modelSpot);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void SpawnEffectSocketsExtServerRpc(
            NetworkBehaviourReference sockets,
            byte modelSpot,
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Vector3 position, 
            HalfQuaternion rotation,
            HalfVector3 scale,
            RpcParams rpcParams = default)
        {
            if(!sockets.TryGet(out Entity socketsEntity) || socketsEntity.EntityModel == null || socketsEntity.EntityModel.Sockets == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Failed to unpack sockets source entity at {nameof(SpawnEffectSocketsExtServerRpc)}.");
                }
                return;
            }
            ModelSocketHandler handler = socketsEntity.EntityModel.Sockets;
            ServerSpawnEffect(rpcParams.Receive.SenderClientId, clientTime, effectID, extraData, index, null, position, rotation, scale, handler, modelSpot);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void SpawnEffectSocketsAndSourceServerRpc(
            NetworkBehaviourReference source,
            NetworkBehaviourReference sockets,
            byte modelSpot,
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Vector3 position, 
            RpcParams rpcParams = default)
        {
            if(!source.TryGet(out Entity entity)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Failed to unpack the source entity at {nameof(SpawnEffectSocketsAndSourceServerRpc)}.");
                }
                return;
            }
            if(!sockets.TryGet(out Entity socketsEntity) || socketsEntity.EntityModel == null || socketsEntity.EntityModel.Sockets == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Failed to unpack the sockets source entity at {nameof(SpawnEffectSocketsAndSourceServerRpc)}.");
                }
                return;
            }
            ModelSocketHandler handler = socketsEntity.EntityModel.Sockets;
            ServerSpawnEffect(rpcParams.Receive.SenderClientId, clientTime, effectID, extraData, index, entity, position, sockets: handler, modelSpot: modelSpot);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        public void SpawnEffectSocketsAndSourceExtServerRpc(
            NetworkBehaviourReference source,
            NetworkBehaviourReference sockets,
            byte modelSpot,
            double clientTime,
            ushort effectID, 
            ushort extraData,
            byte index, 
            Vector3 position, 
            HalfQuaternion rotation,
            HalfVector3 scale,
            RpcParams rpcParams = default)
        {
            if(!source.TryGet(out Entity entity)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Failed to unpack the source entity at {nameof(SpawnEffectSocketsAndSourceExtServerRpc)}.");
                }
                return;
            }
            if(!sockets.TryGet(out Entity socketsEntity) || socketsEntity.EntityModel == null || socketsEntity.EntityModel.Sockets == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Failed to unpack the sockets source entity at {nameof(SpawnEffectSocketsAndSourceExtServerRpc)}.");
                }
                return;
            }
            ModelSocketHandler handler = socketsEntity.EntityModel.Sockets;
            ServerSpawnEffect(rpcParams.Receive.SenderClientId, clientTime, effectID, extraData, index, entity, position, rotation, scale, handler, modelSpot);
        }
        
        // *******************************************************************************************************************************************
        // SERVER -> CLIENT RPC
        // *******************************************************************************************************************************************

        [Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
        public void NotifyEffectSpawnedRpc(ushort effectID, byte index, RpcParams rpcParams = default)
        {
            EffectManager.NotifyEffectSpawnedOnServer(effectID, index);
        }
    }
}
