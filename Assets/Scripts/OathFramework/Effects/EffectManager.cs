using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Networking;
using OathFramework.Pooling;
using OathFramework.Settings;
using OathFramework.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.Effects
{
    public class EffectManager : Subsystem, IMaterialPreloaderDataProvider
    {
        [SerializeField] private EffectCollection[] effects;
        
        public override string Name    => "Effects Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EffectsManager;

        private static Dictionary<ushort, Effect> networkPrefabDict          = new();
        private static Dictionary<(ushort, byte), Effect> localEffectsLookup = new();
        private static Dictionary<ushort, byte> curLocalIndexes              = new();
        private static Database database = new();
        
        public static EffectManager Instance { get; private set; }

        public override async UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(EffectManager)} singleton.");
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            foreach(EffectCollection effectCollection in effects) {
                foreach(PoolParams pair in effectCollection.pools) {
                    if(!pair.Prefab.TryGetComponent(out Effect effect)) {
                        Debug.LogError($"{pair.Prefab.name} is not an {nameof(Effect)}.");
                        continue;
                    }
                    if(!Register(effect, out ushort id))
                        continue;

                    if(effect.GetComponent<NetEffect>() != null) {
                        networkPrefabDict.Add(id, effect);
                    }
                    await PoolManager.RegisterPoolAsync(timer, new PoolManager.GameObjectPool(pair), true);
                }
            }
        }
        
        public static bool Register(Effect effect, out ushort id)
        {
            id = default;
            EffectParams @params = effect.Params;
            if(database.RegisterWithID(@params.Key, effect, @params.DefaultID)) {
                @params.ID = @params.DefaultID;
                id         = @params.ID;
                return true;
            }
            if(database.Register(@params.Key, effect, out ushort retID)) {
                @params.ID = retID;
                id         = @params.ID;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(Effect)} '{@params.Key}'.");
            return false;
        }

        public static Effect Retrieve(
            string key,
            IEntity source             = null,
            Vector3? pos               = null,
            Quaternion? rot            = null,
            Vector3? scale             = null,
            ModelSocketHandler sockets = null,
            byte modelSpot             = 0,
            ushort? extraData          = null,
            bool local                 = false,
            bool? networkedOverride    = null)
        {
            if(!database.TryGet(key, out Effect _, out ushort id)) {
                Debug.LogError($"No {nameof(Effect)} with key '{key}' found.");
                return null;
            }
            return Retrieve(id, source, pos, rot, scale, sockets, modelSpot, extraData, local, networkedOverride);
        }

        public static Effect Retrieve(
            ushort id, 
            IEntity source             = null,
            Vector3? pos               = null,
            Quaternion? rot            = null,
            Vector3? scale             = null, 
            ModelSocketHandler sockets = null, 
            byte modelSpot             = 0, 
            ushort? extraData          = null,
            bool local                 = false,
            bool? networkedOverride    = null)
        {
            if(!database.TryGet(id, out Effect prefab, out _)) {
                Debug.LogError($"No {nameof(Effect)} with ID {id} found.");
                return null;
            }

            ModelSpot parent = null;
            bool found       = networkPrefabDict.ContainsKey(id);
            bool isNetworked = networkedOverride ?? found;
            if(isNetworked && NetGameRpcHelper.Instance.IsServer) {
                // Server always spawns the 'true' effect.
                local = false;
            }
            if(!ReferenceEquals(sockets, null)) {
                parent = sockets.GetModelSpot(modelSpot);
            }
            
            Effect instance = ReferenceEquals(parent, null) 
                ? PoolManager.Retrieve(prefab.gameObject, pos, rot, scale).GetComponent<Effect>() 
                : PoolManager.Retrieve(prefab.gameObject, pos, rot, scale, parent.Transform, prefab.LocalRotation).GetComponent<Effect>();

            if(instance.HasDelayedTransform && !ReferenceEquals(parent, null)) {
                instance.DelayTransform.SetTarget(parent.Transform);
            }
            instance.Local     = local;
            instance.Source    = source;
            instance.ExtraData = extraData ?? 0;
            if(isNetworked) {
                // Defer setting source and/or sockets to netcode.
                HandleNetworkedEffect(local, instance);
            } else {
                if(!ReferenceEquals(parent, null)) {
                    sockets.AddPlug(parent.ID, instance);
                }
            }
            
            if(!isNetworked && found) {
                instance.NetworkEnabled                        = false;
                instance.GetComponent<NetworkObject>().enabled = false;
            }
            if(instance.Local) {
                instance.OnSpawned();
            }
            return instance;
        }

        private static void HandleNetworkedEffect(bool local, Effect effect)
        {
            effect.NetworkEnabled = true;
            if(!local && NetGameRpcHelper.Instance.IsServer) {
                NetworkObject obj = effect.GetComponent<NetworkObject>();
                NetEffect ne      = effect.GetComponent<NetEffect>();
                obj.enabled       = true;
                obj.Spawn();
            } else if(local && !NetGameRpcHelper.Instance.IsServer) {
                NetworkObject obj              = effect.GetComponent<NetworkObject>();
                EntityModelSocketHandler eSock = effect.Sockets as EntityModelSocketHandler;
                obj.enabled                    = false;
                NetEffect ne                   = effect.NetEffect;
                ne.Index                       = GetNextIndex(effect.ID);
                Transform t                    = effect.transform;
                if(!ne.HideNetworkedForSource) {
                    // Server copy IS visible to the client here when HideNetworkedForSource is false.
                    localEffectsLookup.Add((effect.ID, ne.Index.Value), effect);
                }
                if(!ReferenceEquals(eSock, null) && !ReferenceEquals(effect.Source, null)) {
                    EffectRpcHelper.Instance.SpawnEffectSocketsAndSourceExtServerRpc(
                        effect.Source as Entity, 
                        eSock.Entity, 
                        effect.CurrentSpot, 
                        NetworkManager.Singleton.LocalTime.Time, 
                        effect.ID, 
                        effect.ExtraData,
                        ne.Index.Value, 
                        t.position,
                        (HalfQuaternion)effect.transform.localRotation,
                        (HalfVector3)effect.transform.localScale
                    );
                    return;
                }
                if(!ReferenceEquals(eSock, null)) {
                    EffectRpcHelper.Instance.SpawnEffectSocketsExtServerRpc(
                        eSock.Entity, 
                        effect.CurrentSpot, 
                        NetworkManager.Singleton.LocalTime.Time, 
                        effect.ID, 
                        effect.ExtraData,
                        ne.Index.Value, 
                        t.position,
                        (HalfQuaternion)effect.transform.localRotation,
                        (HalfVector3)effect.transform.localScale
                    );
                    return;
                }
                if(!ReferenceEquals(effect.Source, null)) {
                    EffectRpcHelper.Instance.SpawnEffectSourceExtServerRpc(
                        effect.Source as Entity, 
                        NetworkManager.Singleton.LocalTime.Time, 
                        effect.ID, 
                        effect.ExtraData,
                        ne.Index.Value, 
                        t.position,
                        (HalfQuaternion)effect.transform.localRotation,
                        (HalfVector3)effect.transform.localScale
                    );
                    return;
                }
                EffectRpcHelper.Instance.SpawnEffectExtServerRpc(
                    NetworkManager.Singleton.LocalTime.Time, 
                    effect.ID, 
                    effect.ExtraData,
                    ne.Index.Value, 
                    t.position,
                    (HalfQuaternion)effect.transform.localRotation,
                    (HalfVector3)effect.transform.localScale
                );
            }
        }

        public static byte GetNextIndex(ushort id)
        {
            if(curLocalIndexes.TryGetValue(id, out byte i)) {
                unchecked {
                    curLocalIndexes[id] = ++i;
                }
                return i;
            }
            curLocalIndexes[id] = 0;
            return 0;
        }

        public static void ReturnImmediate(Effect effect)
        {
            if(!ReferenceEquals(effect.Sockets, null)) {
                effect.Sockets.RemovePlug(effect);
            }
            if(effect.IsNetworked) {
                ReturnNetworked(effect);
                return;
            }
            effect.PoolableGO.Return();
        }

        private static void ReturnNetworked(Effect effect)
        {
            if(NetworkManager.Singleton.IsServer) {
                effect.GetComponent<NetworkObject>().Despawn();
                return;
            }
            if(effect.Local && !NetworkManager.Singleton.IsServer) {
                effect.PoolableGO.Return();
            }
        }
        
        public static bool TryGetKey(ushort id, out string key) => database.TryGet(id, out _, out key);
        public static bool TryGetID(string key, out ushort id)  => database.TryGet(key, out _, out id);

        public static bool TryGetParams(string key, out EffectParams @params)
        {
            @params = null;
            if(!database.TryGet(key, out Effect e, out _))
                return false;

            @params = e.Params;
            return true;
        }
        
        public static bool TryGetParams(ushort id, out EffectParams @params)
        {
            @params = null;
            if(!database.TryGet(id, out Effect e, out _))
                return false;

            @params = e.Params;
            return true;
        }

        public static void NotifyEffectSpawnedOnServer(ushort id, byte index)
        {
            if(localEffectsLookup.TryGetValue((id, index), out Effect effect)) {
                effect.Return(true);
                localEffectsLookup.Remove((id, index));
            }
            // TODO: Might need to tell the new instance of the old local effect.
        }
        
        Material[] IMaterialPreloaderDataProvider.GetMaterials()
        {
            List<Material> materials = new();
            foreach(EffectCollection collection in effects) {
                foreach(PoolParams pair in collection.pools) {
                    foreach(Renderer render in pair.Prefab.GetComponentsInChildren<Renderer>(true)) {
                        materials.AddRange(render.sharedMaterials);
                    }
                    foreach(IMaterialPreloaderDataProvider prov in pair.Prefab.GetComponentsInChildren<IMaterialPreloaderDataProvider>(true)) {
                        materials.AddRange(prov.GetMaterials());
                    }
                }
            }
            return materials.ToArray();
        }
        
        private class Database : Database<string, ushort, Effect>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
