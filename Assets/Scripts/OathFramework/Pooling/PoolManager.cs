using Cysharp.Threading.Tasks;
using OathFramework.Attributes;
using OathFramework.Core;
using OathFramework.Settings;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace OathFramework.Pooling
{ 
    public sealed class PoolManager : Subsystem, 
        IMaterialPreloaderDataProvider, IResetGameStateCallback
    {
        [SerializeField] private PoolCollectionType defaultCollection;
        [SerializeField] private bool hideInHierarchy;
        [SerializeField] private bool parentPooledObjects = true;
        
        private Dictionary<GameObject, GameObjectPool> poolsDict;
        private List<GameObjectPool> networkPools;
        private Dictionary<PoolCollectionType, List<GameObjectPool>> pendingPools;
        private bool networkInit;
        private PoolConfig config = PoolConfig.Optimal;

        private static float amountMult = 1.0f;
        
        private static bool ParentPooledObjects => Instance.parentPooledObjects;
        
        public static bool IsLoading       { get; private set; }
        
        public static PoolManager Instance { get; private set; }
        public override string Name        => "Pool Manager";
        public override uint LoadOrder     => SubsystemLoadOrders.PoolManager;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple '{nameof(PoolManager)}' singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

#if !UNITY_EDITOR
            parentPooledObjects = false;
            hideInHierarchy = false;
#endif
            
            GameCallbacks.Register((IResetGameStateCallback)this);
            if(INISettings.GetString("Pooling/PoolConfig", out string val)) {
                switch(val) {
                    case "performance": {
                        config = PoolConfig.Performance;
                    } break;
                    case "optimal": {
                        config = PoolConfig.Optimal;
                    } break;
                    case "memory": {
                        config = PoolConfig.Memory;
                    } break;
                    case "off": {
                        config = PoolConfig.Off;
                    } break;

                    default: {
                        config = PoolConfig.Optimal;
                    } break;
                }
            }

            switch(config) {
                case PoolConfig.Performance: {
                    amountMult = 3.0f;
                } break;
                case PoolConfig.Optimal: {
                    amountMult = 1.0f;
                } break;
                case PoolConfig.Memory: {
                    amountMult = 0.33f;
                } break;
                case PoolConfig.Off: {
                    amountMult = 0.0f;
                } break;
                default: {
                    amountMult = 1.0f;
                } break;
            }

            const int size = 32;
            poolsDict      = new Dictionary<GameObject, GameObjectPool>(size);
            pendingPools   = new Dictionary<PoolCollectionType, List<GameObjectPool>>(size);
            networkPools   = new List<GameObjectPool>(size);
            return UniTask.CompletedTask;
        }

        public static async UniTask AwaitLoading()
        {
            while(IsLoading)
                await UniTask.Yield();
        }

        public static async UniTask InstantiatePendingAsync(Stopwatch timer, PoolCollectionType collection)
        {
            IsLoading = true;
            if(!timer.IsRunning) {
                timer.Start();
            }
            
            // If no collection is specified, load all pending.
            if(collection == null) {
                foreach(List<GameObjectPool> allPending in Instance.pendingPools.Values) {
                    foreach(GameObjectPool pool in allPending) {
                        await pool.InstantiateAsync(timer);
                    }
                }
                IsLoading = false;
                return;
            }
            
            // Else, load specified collection.
            if(Instance.pendingPools.TryGetValue(collection, out List<GameObjectPool> pending)) {
                foreach(GameObjectPool pool in pending) {
                    await pool.InstantiateAsync(timer);
                }
                pending.Clear();
            }
            IsLoading = false;
        }

        public static void InstantiatePending(PoolCollectionType collection)
        {
            IsLoading = true;
            
            // If no collection is specified, load all pending.
            if(collection == null) {
                foreach(List<GameObjectPool> allPending in Instance.pendingPools.Values) {
                    foreach(GameObjectPool pool in allPending) {
                        pool.InstantiatePool();
                    }
                    allPending.Clear();
                }
                IsLoading = false;
                return;
            }
            
            // Else, load specified collection.
            if(Instance.pendingPools.TryGetValue(collection, out List<GameObjectPool> pending)) {
                foreach(GameObjectPool pool in pending) {
                    pool.InstantiatePool();
                }
                pending.Clear();
            }
            IsLoading = false;
        }

        public static void RegisterPoolCollection(PoolableCollection collection)
        {
            foreach(GameObjectPool pool in collection.pools) {
                RegisterPool(pool);
            }
        }
        
        public static async UniTask RegisterPoolAsync(Stopwatch timer, GameObjectPool pool, bool instantiateImmediately = false)
        {
            if(!RegisterPoolInternal(pool))
                return;
            
            if(instantiateImmediately) {
                await pool.InstantiatePoolAsync(timer);
            }
        }

        public static void RegisterPool(GameObjectPool pool, bool instantiateImmediately = false)
        {
            if(!RegisterPoolInternal(pool))
                return;
            
            if(instantiateImmediately) {
                pool.InstantiatePool();
            }
        }
        
        private static bool RegisterPoolInternal(GameObjectPool pool)
        {
            if(Instance.poolsDict.ContainsKey(pool.prefab))
                return false;
            
            if(!pool.prefab.TryGetComponent<PoolableGameObject>(out _)) {
                Debug.LogError($"Prefab {pool.prefab.name} does not have a PoolableGameObject component!");
                return false;
            }
            
            GameObject temp   = pool.prefab;
            pool.IsNetworked  = temp.TryGetComponent<NetworkObject>(out _);
            GameObject parent = new(pool.prefab.name);
            parent.transform.SetParent(Instance.transform);
            pool.SetParams(Instance.hideInHierarchy, parent.transform);
            Instance.poolsDict.Add(pool.prefab, pool);
            if(pool.IsNetworked) {
                Instance.networkPools.Add(pool);
            }
            return true;
        }

        private static void RegisterPending(GameObjectPool pool)
        {
            PoolCollectionType collection = pool.Collection ?? Instance.defaultCollection;
            if(Instance.pendingPools.TryGetValue(collection, out List<GameObjectPool> pending)) {
                pending.Add(pool);
                return;
            }
            Instance.pendingPools.Add(collection, new List<GameObjectPool> { pool });
        }

        public static void NetworkInitialize()
        {
            foreach(GameObjectPool pool in Instance.networkPools) {
                pool.NetworkInitialized();
            }
        }

        public static void OnNetworkInitialized()
        {
            NetworkInitialize();
        }

        public static void OnNetworkClosed()
        {
            foreach(GameObjectPool pool in Instance.networkPools) {
                pool.NetworkClosed();
            }
        }

        public static PoolableGameObject Retrieve(GameObject prefab, Transform parent) 
            => Retrieve(prefab, null, null, null, parent);
        public static PoolableGameObject Retrieve(GameObject prefab, Vector3? position = null, Quaternion? rotation = null) 
            => Retrieve(prefab, position, rotation, null);

        public static PoolableGameObject Retrieve(
            GameObject prefab, 
            Vector3? position, 
            Quaternion? rotation, 
            Vector3? scale, 
            Transform parent   = null, 
            bool localRotation = true)
        {
            if(!Instance.poolsDict.TryGetValue(prefab, out GameObjectPool pool)) {
                Debug.LogError($"No ObjectPool for '{prefab.name}' found.");
                return null;
            }
            return pool.Retrieve(position, rotation, scale, parent, localRotation);
        }

        public static void Return(PoolableGameObject poolable) => poolable.Return();
        public static void Return(IPoolableComponent poolable) => poolable.PoolableGO.Return();

        public static void ReturnAll(PoolCollectionType collection = null)
        {
            foreach(GameObjectPool pair in Instance.poolsDict.Values) {
                if(collection == null || pair.Collection == collection) {
                    pair.ReturnAllObjects();
                }
            }
        }

        public static void Destroy(PoolCollectionType collection = null)
        {
            foreach(GameObjectPool pair in Instance.poolsDict.Values) {
                if(collection == null || pair.Collection == collection) {
                    pair.Destroy();
                }
            }
        }
        
        public static async UniTask DestroyAsync(Stopwatch timer, PoolCollectionType collection = null)
        {
            foreach(GameObjectPool pair in Instance.poolsDict.Values) {
                if(collection == null || pair.Collection == collection) {
                    await pair.DestroyAsync(timer);
                }
            }
        }
        
        public static bool IsPrefabPooled(GameObject prefab) => Instance.poolsDict.ContainsKey(prefab);
        
        Material[] IMaterialPreloaderDataProvider.GetMaterials()
        {
            List<Material> materials = new();
            return materials.ToArray();
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            foreach(GameObjectPool pool in poolsDict.Values) {
                if(!pool.ReturnOnReset || pool.IsNetworked)
                    continue; // If networked, Shutdown() deletes them.
                
                pool.ReturnAllObjects();
            }
        }

        [Serializable]
        public sealed class GameObjectPool : IArrayElementTitle
        {
            public GameObject prefab;
            [SerializeField] private bool returnOnReset;
            
            [NonSerialized] public bool IsNetworked;
            
            [Space(5)]
            
            [SerializeField] private PoolBehaviour behaviour = PoolBehaviour.Grow;
            
            [Space(5)]
            
            [SerializeField] private int size = 10;
            [SerializeField] private bool overrideForMobile;
            
            [SerializeField, ShowIf(nameof(overrideForMobile))]
            private int mobileSize = 10;

            public int Size {
                get {
#if !UNITY_IOS && !UNITY_ANDROID
                    return size;
#else
                    return overrideForMobile ? mobileSize : size;
#endif
                }
                private set {
                    size = value;
                }
            }

            private Transform parentTransform;
            private List<PoolableGameObject> all;
            private List<PoolableGameObject> pool;
            private Queue<PoolableGameObject> queuePool;
            private bool hideInHierarchy;
            private Vector3 startScale;
            private Quaternion startRotation;

            public PoolCollectionType Collection { get; private set; } 
            public PoolBehaviour Behaviour       { get; private set; }
            public bool IsInstantiated           { get; private set; }
            
            public bool PoolingDisabled => Instance.config == PoolConfig.Off && behaviour == PoolBehaviour.Grow;
            public bool ReturnOnReset => returnOnReset;
            
            string IArrayElementTitle.Name => prefab == null || string.IsNullOrEmpty(prefab.name) ? "NULL" : prefab.name;
            
            public GameObjectPool() { }
            
            public GameObjectPool(PoolParams @params)
            {
                prefab            = @params.Prefab;
                overrideForMobile = @params.OverrideForMobile;
                size              = @params.Size;
                mobileSize        = @params.MobileSize;
                behaviour         = @params.Behaviour;
                returnOnReset     = @params.ReturnOnReset;
                Collection        = @params.Collection;
            }

            public async UniTask InstantiateAsync(Stopwatch timer)
            {
                await InstantiatePoolAsync(timer);
            }

            public void SetParams(bool hideInHierarchy, Transform parentTransform)
            {
                this.hideInHierarchy = hideInHierarchy;
                Behaviour            = behaviour;
                all                  = new List<PoolableGameObject>(Size);
                switch(behaviour) {
                    case PoolBehaviour.Grow: {
                        pool = new List<PoolableGameObject>(Size);
                    } break;
                    case PoolBehaviour.Queue: {
                        queuePool = new Queue<PoolableGameObject>(Size);
                    } break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                startScale    = prefab.transform.localScale;
                startRotation = prefab.transform.localRotation;

                if(Game.ExtendedDebug) {
                    NetworkObject netObj = prefab.GetComponent<NetworkObject>();
                    if(IsNetworked && netObj.AutoObjectParentSync) {
                        Debug.LogError($"Pooled network object {prefab.name} must have AutoObjectParentSync set to false.");
                    }
                }
                this.parentTransform = parentTransform;
            }

            public void InstantiatePool()
            {
                if(PoolingDisabled || IsInstantiated)
                    return;

                int poolSize = behaviour == PoolBehaviour.Grow ? (int)(Size * amountMult) : Size;
                for(int i = 0; i < poolSize; i++) {
                    try {
                        CreateNew(true);
                    } catch(Exception e) {
                        Debug.LogError($"Exception occured when creating pool prefab instance: {e}");
                    }
                }
                IsInstantiated = true;
            }

            public async UniTask InstantiatePoolAsync(Stopwatch timer)
            {
                if(PoolingDisabled || IsInstantiated)
                    return;

                int poolSize = behaviour == PoolBehaviour.Grow ? (int)(Size * amountMult) : Size;
                for(int i = 0; i < poolSize; i++) {
                    try {
                        CreateNew(true);
                    } catch(Exception e) {
                        Debug.LogError($"Exception occured when creating pool prefab instance: {e}");
                    }
                    if(timer.Elapsed.Milliseconds < AsyncFrameBudgets.High)
                        continue;

                    await UniTask.Yield();
                    timer.Restart();
                }
                IsInstantiated = true;
            }

            public void Destroy()
            {
                foreach(PoolableGameObject go in all) {
                    try {
                        Object.Destroy(go.gameObject);
                    } catch(Exception e) {
                        Debug.LogError($"Exception occured when destroying pool prefab instance: {e}");
                    }
                }
                //Object.Destroy(parentTransform.gameObject);
                ClearCollections();
                RegisterPending(this);
            }

            public async UniTask DestroyAsync(Stopwatch timer)
            {
                for(int i = 0; i < all.Count; i++) {
                    if(all[i] == null)
                        continue;
                    
                    try {
                        Object.Destroy(all[i].gameObject);
                    } catch(Exception e) {
                        Debug.LogError($"Exception occured when destroying pool prefab instance: {e}");
                    }
                    if(timer.Elapsed.Milliseconds < AsyncFrameBudgets.High)
                        continue;

                    await UniTask.Yield();
                    timer.Restart();
                }
                ClearCollections();
                RegisterPending(this);
            }

            private void ClearCollections()
            {
                all?.Clear();
                pool?.Clear();
                queuePool?.Clear();
                IsInstantiated = false;
            }

            public void NetworkInitialized()
            {
                if(!IsNetworked)
                    return;
                
                NetworkManager.Singleton.PrefabHandler.AddHandler(prefab.GetComponent<NetworkObject>(), new InstanceHandler(this));
            }

            public void NetworkClosed()
            {
                if(!IsNetworked)
                    return;
                
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab.GetComponent<NetworkObject>());
            }

            private PoolableGameObject CreateNew(bool initialize)
            {
                GameObject go = Instantiate(prefab, ParentPooledObjects ? parentTransform : null);
                if(!go.TryGetComponent(out PoolableGameObject poolable)) {
                    poolable = go.AddComponent<PoolableGameObject>();
                }
                
                if(!PoolingDisabled) {
                    all.Add(poolable);
                    poolable.Initialize(this);
                    Return(poolable, true);
                } else {
                    poolable.Initialize(this);
                    poolable.OnReturn(true);
                }
                
                if(hideInHierarchy) {
                    poolable.gameObject.hideFlags = HideFlags.HideInHierarchy;
                }
                return poolable;
            }

            public PoolableGameObject Retrieve(
                Vector3? position,
                Quaternion? rotation,
                Vector3? scale,
                Transform parent, 
                bool localRotation = true)
            {
                PoolableGameObject poolable;
                try {
                    poolable = GetNext();
                } catch(Exception e) {
                    Debug.LogError($"Exception occured when retrieving pool prefab instance: {e}");
                    return null;
                }
                if(ReferenceEquals(poolable, null) || poolable.IsDestroyed) {
                    Debug.LogError($"Failed to retrieve pooled object instance '{prefab.name}'");
                    return null;
                }
                
                if(scale.HasValue) {
                    poolable.transform.localScale = scale.Value;
                }
                poolable.Attached = null;
                if(!ReferenceEquals(parent, null)) {
                    poolable.Attached           = parent;
                    poolable.OffsetPosition     = position;
                    poolable.OffsetRotation     = rotation;
                    poolable.transform.position = position.HasValue ? parent.position + position.Value : parent.position;
                    if(localRotation) {
                        poolable.transform.rotation = rotation.HasValue ? parent.rotation * rotation.Value : parent.rotation;
                    } else {
                        poolable.transform.rotation = rotation ?? Quaternion.identity;
                    }
                } else {
                    if(position.HasValue) {
                        poolable.transform.position = position.Value;
                    }
                    if(rotation.HasValue) {
                        poolable.transform.rotation = rotation.Value;
                    }
                }
                poolable.SetParams(true, true);
                poolable.OnRetrieve();
                return poolable;
            }

            public void Return(PoolableGameObject poolable, bool initialization)
            {
                if(poolable.IsInPool)
                    return;
                
                poolable.OnReturn(initialization);
                if(PoolingDisabled) {
                    Object.Destroy(poolable.gameObject);
                    return;
                }
                
                poolable.CTransform.localScale    = startScale;
                poolable.CTransform.localRotation = startRotation;
                if(behaviour != PoolBehaviour.Queue) {
                    pool.Add(poolable);
                } else if(initialization) {
                    queuePool.Enqueue(poolable);
                }
            }

            public void ReturnAllObjects()
            {
                foreach(PoolableGameObject go in all) {
                    try {
                        Return(go, false);
                    } catch(Exception e) {
                        Debug.LogError($"Failed to return object '{go.name}': {e.Message}");
                        Object.Destroy(go);
                    }
                }
            }

            private PoolableGameObject GetNext()
            {
                PoolableGameObject poolable;
                switch(Behaviour) {
                    case PoolBehaviour.Grow: {
                        if(pool.Count > 0) {
                            poolable = pool[pool.Count - 1];
                            pool.RemoveAt(pool.Count - 1);
                            return poolable;
                        }
                        
                        poolable = CreateNew(true);
                        if(!PoolingDisabled) {
                            pool.RemoveAt(pool.Count - 1);
                        }
                        return poolable;
                    }
                    case PoolBehaviour.Queue: {
                        poolable = queuePool.Dequeue();
                        try {
                            if(!poolable.IsInPool) {
                                poolable.OnReturn(false);
                            }
                        } catch(Exception e) {
                            Debug.LogError($"Failed to swap queued object '{poolable.name}': {e.Message}");
                        }
                        queuePool.Enqueue(poolable);
                        return poolable;
                    }
                    default: {
                        Debug.LogError("Invalid PoolBehaviour.");
                        return null;
                    }
                }
            }

            private class InstanceHandler : INetworkPrefabInstanceHandler
            {
                private readonly GameObjectPool pool;
                
                public InstanceHandler(GameObjectPool pool) => this.pool = pool;
                
                void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
                    => pool.Return(networkObject.GetComponent<PoolableGameObject>(), false);
                NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
                    => pool.Retrieve(position, rotation, null, null).GetComponent<NetworkObject>();
            }
        }
    }

    [Serializable]
    public class PoolParams
    {
        [field: SerializeField] public GameObject Prefab             { get; private set; }
        [field: SerializeField] public PoolCollectionType Collection { get; private set; }
        [field: SerializeField] public bool ReturnOnReset            { get; private set; }
        
        [field: Space(5)]
            
        [field: SerializeField] public PoolBehaviour Behaviour { get; private set; } = PoolBehaviour.Grow;
            
        [field: Space(5)]
            
        [field: SerializeField] public int Size                { get; private set; } = 10;
        [field: SerializeField] public bool OverrideForMobile  { get; private set; }
            
        [field: SerializeField, ShowIf(nameof(OverrideForMobile))]
        public int MobileSize                                  { get; private set; } = 10;
    }

    public interface IPoolableComponent
    {
        PoolableGameObject PoolableGO { get; set; }
        void OnRetrieve();
        void OnReturn(bool initialization);
    }

    public enum PoolBehaviour { Grow, Queue }
    public enum PoolConfig    { Performance, Optimal, Memory, Off }
}
