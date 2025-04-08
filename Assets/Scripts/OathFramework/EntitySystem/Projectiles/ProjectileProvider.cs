using OathFramework.Core;
using OathFramework.Utility;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{
    [RequireComponent(typeof(Entity))]
    public class ProjectileProvider : NetworkBehaviour
    {
        private Dictionary<ushort, IProjectileDataProvider> dataProviders = new();
        private AccessToken callbacksToken;
        
        public Entity Entity                 { get; private set; }
        public ProjectileCallbacks Callbacks { get; } = new();
        
        [field: SerializeField] public EntityTeams[] Targets { get; private set; }

        private void Awake()
        {
            Entity         = GetComponent<Entity>();
            callbacksToken = Callbacks.Access.GenerateAccessToken();
        }

        private IProjectileDataProvider GetDataProvider(ushort projectileID)
        {
            dataProviders.TryGetValue(projectileID, out IProjectileDataProvider val);
            return val;
        }

        private bool TryGetDataProvider(ushort projectileID, out IProjectileDataProvider val)
        {
            val = GetDataProvider(projectileID);
            return val != null;
        }
        
        public void RegisterProvider(ushort projectileID, IProjectileDataProvider provider)
        {
            dataProviders[projectileID] = provider;
        }

        public void UnregisterProvider(ushort projectileID)
        {
            dataProviders.Remove(projectileID);
        }

        public void UnregisterAllProviders()
        {
            dataProviders.Clear();
        }
        
        public void CreateProjectile(ushort projectileID, Vector3 origin, Quaternion rotation, ushort extraData = 0)
        {
            ProjectileParams @params = new(projectileID, origin, rotation);
            CreateProjectileInternal(@params, extraData);
            if(IsServer) {
                if(extraData != 0) {
                    CreateProjectileWithDataClientRpc(@params, extraData);
                } else {
                    CreateProjectileClientRpc(@params);
                }
            } else {
                if(extraData != 0) {
                    CreateProjectileWithDataServerRpc(@params, extraData);
                } else {
                    CreateProjectileServerRpc(@params);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnProjectileDespawned(IProjectile projectile, bool missed)
        {
            Callbacks.Access.CallProjectileDespawned(callbacksToken, projectile, missed);
        }

        private void CreateProjectileInternal(ProjectileParams @params, ushort extraData)
        {
            if(!TryGetDataProvider(@params.ProjectileID, out IProjectileDataProvider provider)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Couldn't find ProjectileDataProvider under index {@params.ProjectileID}.");
                }
                return;
            }
            IProjectileData data = provider.GetProjectileData(Entity, extraData);
            CreateProjectileInternal(ref data, ref @params);
        }

        private void CreateProjectileInternal(ref IProjectileData data, ref ProjectileParams @params)
        {
            Callbacks.Access.CallProjectilePreSpawned(callbacksToken, ref @params, ref data);
            IProjectile projectile = ProjectileManager.CreateProjectile(in @params, Entity, IsOwner, Targets, data);
            Callbacks.Access.CallProjectileSpawned(callbacksToken, projectile);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void CreateProjectileWithDataServerRpc(ProjectileParams @params, ushort extraData, RpcParams rpcParams = default)
        {
            CreateProjectileInternal(@params, extraData);
            CreateProjectileWithDataClientRpc(@params, extraData);
        }
        
        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void CreateProjectileWithDataClientRpc(ProjectileParams @params, ushort extraData, RpcParams rpcParams = default)
        {
            if(IsOwner)
                return;

            CreateProjectileInternal(@params, extraData);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void CreateProjectileServerRpc(ProjectileParams @params, RpcParams rpcParams = default)
        {
            CreateProjectileInternal(@params, 0);
            CreateProjectileClientRpc(@params);
        }
        
        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void CreateProjectileClientRpc(ProjectileParams @params, RpcParams rpcParams = default)
        {
            if(IsOwner)
                return;

            CreateProjectileInternal(@params, 0);
        }
    }

    public struct ProjectileParams : INetworkSerializable
    {
        public ushort ProjectileID;
        
        // Use full precision for position, to mitigate lag.
        public Vector3 Origin;
        
        private HalfQuaternion halfRotation;
        public Quaternion Rotation => (Quaternion)halfRotation;

        public ProjectileParams(ushort type, Vector3 origin, Quaternion rotation)
        {
            ProjectileID = type;
            Origin       = origin;
            halfRotation = (HalfQuaternion)rotation;
        }

        public ProjectileTemplate ProjectileTemplate 
            => ProjectileManager.TryGetProjectileTemplate(ProjectileID, out ProjectileTemplate temp) ? temp : null;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ProjectileID);
            serializer.SerializeValue(ref Origin);
            serializer.SerializeValue(ref halfRotation);
        }
    }
}
