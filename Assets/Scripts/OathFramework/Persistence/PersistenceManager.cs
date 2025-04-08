using OathFramework.AbilitySystem;
using Cysharp.Threading.Tasks;
using OathFramework.Achievements;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.EntitySystem.Projectiles;
using OathFramework.EntitySystem.States;
using OathFramework.EquipmentSystem;
using OathFramework.Networking;
using OathFramework.PerkSystem;
using OathFramework.Pooling;
using OathFramework.Progression;
using OathFramework.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Netcode;
using Unity.Serialization.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using Debug = UnityEngine.Debug;

namespace OathFramework.Persistence
{
    public partial class PersistenceManager : Subsystem
    {
        [SerializeField] private PrefabCollection[] prefabCollections;
        [SerializeField] private ProxyPrefabCollection[] proxyPrefabCollections;
        [SerializeField] private LocalizedString loadFailedMsg;
        
        private const int RandGenAttempts = 128;
        private static string tmpString;
        
#if UNITY_EDITOR
        private static bool compressSnapshots = false;
#else
        private static bool compressSnapshots = true;
#endif

        private static JsonSerializationParameters serializerParams;
        private static Dictionary<string, ComponentAdapter> componentAdapters = new();
        private static Dictionary<string, PersistentScene> persistentScenes   = new();
        private static Dictionary<string, GameObject> prefabs                 = new();
        private static Dictionary<string, GameObject> proxyPrefabs            = new();
        private static Dictionary<string, PersistentObject> objectLookup      = new();
        private static Dictionary<string, PersistentProxy> proxyLookup        = new();
        private static HashSet<string> takenIDs                               = new();
        
        public override string Name    => "Persistence Manager";
        public override uint LoadOrder => SubsystemLoadOrders.PersistenceManager;
        
        public static States State                { get; private set; }
        public static Data? CurrentData           { get; private set; }
        public static PersistentScene GlobalScene { get; private set; } = new("_GLOBAL");
        public static PersistenceManager Instance { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(PersistenceManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }
            
            DontDestroyOnLoad(gameObject);
            Instance = this;
            
            // Non-interned string.
            tmpString = new string(new char[8]);

#if UNITY_EDITOR
            serializerParams = new JsonSerializationParameters {
                Indent = 2,
                Minified = false
            };
#else
            if(INISettings.GetString("FileIO/PersistenceFormat", out string val)) {
                switch(val) {
                    case "debug": {
                        serializerParams = new JsonSerializationParameters {
                            Indent   = 2,
                            Minified = false
                        };
                    } break;
                    
                    default:
                    case "performance": {
                        serializerParams = new JsonSerializationParameters {
                            Indent   = 0,
                            Minified = true
                        };
                    } break;
                }
            }
#endif
            
            foreach(PrefabCollection prefab in prefabCollections) {
                foreach(PrefabCollection.Node node in prefab.Prefabs) {
                    RegisterPrefab(node.ID, node.Prefab);
                }
            }
            foreach(ProxyPrefabCollection proxies in proxyPrefabCollections) {
                foreach(ProxyPrefabCollection.Node node in proxies.Proxies) {
                    RegisterProxyPrefab(node.ID, node.ProxyPrefab);
                }
            }
            // Adapters.
            JsonSerialization.AddGlobalAdapter(new DataAdapter());
            JsonSerialization.AddGlobalAdapter(new PersistentObject.DataAdapter());
            JsonSerialization.AddGlobalAdapter(new TransformDataAdapter());
            JsonSerialization.AddGlobalAdapter(new PersistentScene.DataAdapter());
            JsonSerialization.AddGlobalAdapter(new PersistentProxy.Adapter());
            JsonSerialization.AddGlobalAdapter(new PlayerBuildData.JsonAdapter());
            JsonSerialization.AddGlobalAdapter(new UniqueID.JsonAdapter());
            JsonSerialization.AddGlobalAdapter(new StdBulletData.Adapter());
            JsonSerialization.AddGlobalAdapter(new HitEffectInfo.JsonAdapter());
            JsonSerialization.AddGlobalAdapter(new AnimationCurveAdapter());
            JsonSerialization.AddGlobalAdapter(new ColorAdapter());
            JsonSerialization.AddGlobalAdapter(new GradientAdapter());
            JsonSerialization.AddGlobalAdapter(new GradientColorKeyAdapter());
            JsonSerialization.AddGlobalAdapter(new GradientAlphaKeyAdapter());
            JsonSerialization.AddGlobalAdapter(new MinMaxGradientAdapter());
            JsonSerialization.AddGlobalAdapter(new PersistentProxy.Adapter());
            JsonSerialization.AddGlobalAdapter(new PlayerProfile.JsonAdapter());
            JsonSerialization.AddGlobalAdapter(new SaveData.JsonAdapter());

            // Migrations.
            JsonSerialization.AddGlobalMigration(new PlayerProfile.JsonAdapter());
            JsonSerialization.AddGlobalMigration(new PlayerBuildData.JsonAdapter());

            // Component adapters.
            RegisterComponentAdapter<Entity.Adapter>();
            RegisterComponentAdapter<StaggerHandler.Adapter>();
            RegisterComponentAdapter<StateHandler.Adapter>();
            RegisterComponentAdapter<FlagHandler.Adapter>();
            RegisterComponentAdapter<EntityEquipment.Adapter>();
            RegisterComponentAdapter<EntityTargeting.Adapter>();
            RegisterComponentAdapter<PlayerPersistenceBinder.Adapter>();
            RegisterComponentAdapter<RaycastProjectile.Adapter>();
            RegisterComponentAdapter<AbilityHandler.Adapter>();
            RegisterComponentAdapter<PerkHandler.Adapter>();
            RegisterComponentAdapter<Effect.Adapter>();
            return UniTask.CompletedTask;
        }

        private static void RegisterPrefab(string id, GameObject prefab)
        {
            if(!prefabs.TryAdd(id, prefab)) {
                Debug.LogError($"Attempted to register duplicate persistent prefab '{id}'");
            }
        }

        private static void RegisterProxyPrefab(string id, GameObject proxy)
        {
            if(!proxyPrefabs.TryAdd(id, proxy)) {
                Debug.LogError($"Attempted to register duplicate persistent proxy prefab '{id}'");
            }
        }

        private void Update()
        {
            // TODO: TESTING!!
            if(Keyboard.current.iKey.wasPressedThisFrame) {
                _ = Save("snapshot");
            }
        }
        
        private static void RegisterComponentAdapter(ComponentAdapter adapter)
        {
            if(!componentAdapters.TryAdd(adapter.ID, adapter)) {
                Debug.LogError($"Attempted to register duplicate {nameof(ComponentAdapter)} '{adapter.ID}'");
                return;
            }
            adapter.OnInitialize();
        }

        public static void RegisterComponentAdapter<T>() where T : ComponentAdapter, new() 
            => RegisterComponentAdapter(new T());

        public static bool TryGetComponentAdapter(string id, out ComponentAdapter adapter) 
            => componentAdapters.TryGetValue(id, out adapter);

        public static string GenerateRandomID(int attempts = RandGenAttempts)
        {
            // This technically violates a rule of C# that strings are immutable, and might cause issues.
            // (specifically Assign(tmpString) - ptr manipulation). If some weird shit happens this is likely a culprit.
            int curAttempt = 0;
            while(curAttempt < attempts) {
                UniqueID.Generate().Assign(tmpString);
                if(!takenIDs.Contains(tmpString))
                    return new string(tmpString);

                curAttempt++;
            }
            Debug.LogError("Failed to generate a unique ID.");
            return "WHAT THE FUCK!?";
        }

        public static void RegisterScene(PersistentScene scene) => persistentScenes.Add(scene.ID, scene);
        public static bool TryGetObject(string id, out PersistentObject obj) => objectLookup.TryGetValue(id, out obj);

        public static bool RegisterObject(string id, PersistentObject obj)
        {
            takenIDs.Add(id);
            return objectLookup.TryAdd(id, obj);
        }

        public static bool UnregisterObject(string id)
        {
            takenIDs.Remove(id);
            return objectLookup.Remove(id);
        }

        public static bool RegisterProxy(string id, PersistentProxy obj)
        {
            takenIDs.Add(id);
            return proxyLookup.TryAdd(id, obj);
        }

        public static bool UnregisterProxy(string id)
        {
            takenIDs.Remove(id);
            return proxyLookup.Remove(id);
        }
        
        public static bool TryGetObjectBehaviour<T>(string id, out T obj) where T : MonoBehaviour
        {
            bool b = objectLookup.TryGetValue(id, out PersistentObject persistentObj);
            obj    = b ? persistentObj.GetComponent<T>() : null;
            return b;
        }

        public static bool TryGetObjectID(IPersistableComponent persistable, out string id)
        {
            id = null;
            if(persistable == null || !(persistable is MonoBehaviour behaviour))
                return false;

            return TryGetObjectID(behaviour.GetComponent<PersistentObject>(), out id);
        }
        
        public static bool TryGetObjectID(PersistentObject obj, out string id)
        {
            id = null;
            if(obj == null)
                return false;

            id = obj.ID;
            return true;
        }

        public static void UnloadSnapshot()
        {
            // TODO: Clean up all snapshot objects. A callback system may be needed to clean everything.
            CurrentData = null;
            State       = States.Idle;
            persistentScenes.Clear();
            objectLookup.Clear();
            proxyLookup.Clear();
            takenIDs.Clear();
            GlobalScene.Clear();
        }
        
        public static async UniTask<bool> LoadSnapshot(string snapshotName)
        {
            // Only the server can load snapshots.
            if(NetGame.ConnectionState == GameConnectionState.Ready && !NetworkManager.Singleton.IsServer)
                return false;

            if(CurrentData != null) {
                Debug.LogError("Data is not null. Snapshot must be cleared first.");
                return false;
            }
            
            State = States.Loading;
            try {
                (LoadResult loadResult, Data data) = await Data.LoadFromFile(snapshotName);
                if(loadResult == LoadResult.Fail) {
                    State = States.Idle;
                    return false;
                }
                
                // Record all IDs so that duplicates don't generate.
                foreach(KeyValuePair<string, PersistentScene.Data> scene in data.Scenes) {
                    foreach(KeyValuePair<string, PersistentObject.Data> obj in scene.Value.ObjectData) {
                        takenIDs.Add(obj.Key);
                    }
                }
                
                CurrentData = data;
                State       = States.Idle;
            } catch(FileNotFoundException) {
                Debug.LogError($"Snapshot '{snapshotName}' not found.");
                State = States.Idle;
                return false;
            } catch(Exception e) {
                Debug.LogError(e);
                State = States.Idle;
                return false;
            }
            return true;
        }
        
        public static async UniTask<bool> ApplySceneData(string scene)
        {
            // Only the server can load snapshots.
            if(NetGame.ConnectionState == GameConnectionState.Ready && !NetworkManager.Singleton.IsServer)
                return false;
            
            if(CurrentData == null) {
                Debug.LogError("Data is null.");
                return false;
            }
            
            State = States.Loading;
            try {
                Data dataCopy = CurrentData.Value;
                
                // Step 1 - Instantiate prefabs and assign IDs.
                if(!InitializeSceneObjects(in dataCopy, scene))
                    throw new Exception("Failed to initialize objects in persistent snapshot.");

                // Step 2 - Load data.
                if(!LoadSceneData(in dataCopy, scene))
                    throw new Exception("Failed to load persistent snapshot data.");
                
                // Step 3 - Run finished callbacks.
                State = States.Idle;
                LoadCompleted(scene);
            } catch(Exception e) {
                Debug.LogError(e);
                _ = AbortLoading();
                State = States.Idle;
                return false;
            }
            return true;
        }

        public static async UniTask<bool> Save(string snapshotName)
        {
            State = States.Saving;
            try {
                await Data.Construct().SaveToFile(snapshotName);
            } catch(Exception e) {
                Debug.LogError(e);
                State = States.Idle;
                return false;
            }
            State = States.Idle;
            return true;
        }

        private static bool TryGetPersistentScene(string sceneID, out PersistentScene scene)
        {
            if(sceneID == "_GLOBAL") {
                scene = GlobalScene;
                return true;
            }
            if(persistentScenes.TryGetValue(sceneID, out scene))
                return true;

            Debug.LogError($"No persistent scene with ID '{sceneID}' found.");
            return false;
        }

        private static bool InitializeSceneObjects(in Data data, string scene)
        {
            PersistentScene.Data sceneData = data.Scenes[scene];
            if(!TryGetPersistentScene(scene, out PersistentScene persistScene))
                return false;
            
            foreach(KeyValuePair<string, PersistentProxy.Data> proxyData in sceneData.ProxyData) {
                if(!InitializeProxy(proxyData.Value, persistScene))
                    return false;
            }
            foreach(KeyValuePair<string, PersistentObject.Data> objData in sceneData.ObjectData) {
                if(!InitializeObject(objData.Value, persistScene))
                    return false;
            }
            return true;
        }

        private static bool LoadSceneData(in Data data, string scene)
        {
            PersistentScene.Data sceneData = data.Scenes[scene];
            if(!TryGetPersistentScene(scene, out PersistentScene persistScene))
                return false;

            foreach(KeyValuePair<string, PersistentProxy.Data> proxyData in sceneData.ProxyData) {
                PersistentProxy.Data proxyDataCopy = proxyData.Value;
                if(!LoadProxyData(in proxyDataCopy, persistScene))
                    return false;
            }
            foreach(KeyValuePair<string, PersistentObject.Data> objData in sceneData.ObjectData) {
                PersistentObject.Data objDataCopy = objData.Value;
                if(!LoadObjectData(in objDataCopy, persistScene))
                    return false;
            }
            return true;
        }

        private static void LoadCompleted(string scene)
        {
            if(!TryGetPersistentScene(scene, out PersistentScene persistScene))
                return;

            foreach(KeyValuePair<string, PersistentProxy> proxy in persistScene.Proxies) {
                proxy.Value.TriggerLoaded();
            }
            foreach(KeyValuePair<string, PersistentObject> obj in persistScene.Objects) {
                obj.Value.TriggerLoaded();
            }
        }

        private static bool InitializeObject(in PersistentObject.Data data, PersistentScene scene)
        {
            PersistentObject obj;
            if(data.Type == PersistentObject.ObjectType.Prefab) {
                obj = CreatePrefab(in data);
                if(obj == null)
                    return false;
            } else {
                if(!scene.Objects.TryGetValue(data.ID, out obj)) {
                    Debug.LogError($"Could not find scene object with ID '{data.ID}'");
                    return false;
                }
            }
            obj.AssignID(in data);
            return true;
        }

        private static bool InitializeProxy(in PersistentProxy.Data data, PersistentScene scene)
        {
            PersistentProxy proxy = CreateProxy(in data);
            if(proxy == null) {
                Debug.LogError($"Failed to create proxy '{data.SpawnID}'");
                return false;
            }
            proxy.AssignID(in data);
            return true;
        }
        
        private static bool LoadObjectData(in PersistentObject.Data data, PersistentScene scene)
        {
            if(!scene.Objects.TryGetValue(data.ID, out PersistentObject obj))
                return true;

            try { 
                obj.AssignData(in data);
            } catch(Exception e) {
                Debug.LogError(e);
                if(obj.FailAction == VerificationFailAction.Abort)
                    return false;
            }
            return true;
        }

        private static bool LoadProxyData(in PersistentProxy.Data data, PersistentScene scene)
        {
            if(!scene.Proxies.TryGetValue(data.ID, out PersistentProxy proxy))
                return true;

            try {
                proxy.AssignData(in data);
            } catch(Exception e) {
                Debug.LogError(e);
                if(proxy.FailAction == VerificationFailAction.Abort)
                    return false;
            }
            return true;
        }

        private static PersistentProxy CreateProxy(in PersistentProxy.Data data)
        {
            if(string.IsNullOrEmpty(data.SpawnID)) {
                Debug.LogError("Persistent prefab spawn ID is null or empty.");
                return null;
            }
            if(!proxyPrefabs.TryGetValue(data.SpawnID, out GameObject prefab)) {
                Debug.LogError($"No persistent proxy prefab with spawn ID '{data.SpawnID}' found.");
                return null;
            }
            GameObject go         = Instantiate(prefab);
            PersistentProxy proxy = go.GetComponent<PersistentProxy>();
            if(proxy != null)
                return proxy;
            
            Debug.LogError($"{data.SpawnID} does not contain a {nameof(PersistentProxy)} component.");
            return null;
        }

        private static PersistentObject CreatePrefab(in PersistentObject.Data data)
        {
            if(string.IsNullOrEmpty(data.SpawnID)) {
                Debug.LogError("Persistent prefab spawn ID is null or empty.");
                return null;
            }
            if(!prefabs.TryGetValue(data.SpawnID, out GameObject prefab)) {
                Debug.LogError($"No persistent prefab with spawn ID '{data.SpawnID}' found.");
                return null;
            }

            TransformData t = data.Transform;
            if(data.Networked)
                return CreateNetworkedObject(in data, prefab);
            if(data.Pooled)
                return PoolManager.Retrieve(prefab, t.Position, t.Rotation, t.Scale).GetComponent<PersistentObject>();
            
            GameObject go           = Instantiate(prefab, t.Position, t.Rotation);
            PersistentObject obj    = go.GetComponent<PersistentObject>();
            go.transform.localScale = t.Scale;
            if(obj != null)
                return obj;
            
            Debug.LogError($"{data.SpawnID} does not contain a {nameof(PersistentObject)} component.");
            return null;
        }

        private static PersistentObject CreateNetworkedObject(in PersistentObject.Data data, GameObject prefab)
        {
            GameObject go;
            TransformData t = data.Transform;
            if(data.Pooled) {
                go = PoolManager.Retrieve(prefab, t.Position, t.Rotation, t.Scale).gameObject;
            } else {
                go = Instantiate(prefab, t.Position, t.Rotation);
                go.transform.localScale = t.Scale;
            }
            NetworkObject netObj = go.GetComponent<NetworkObject>();
            if(netObj == null) {
                Debug.LogError($"{data.SpawnID} does not contain a {nameof(NetworkObject)} component.");
                return null;
            }
            PersistentObject obj = go.GetComponent<PersistentObject>();
            if(obj == null) {
                Debug.LogError($"{data.SpawnID} does not contain a {nameof(PersistentObject)} component.");
                return null;
            }
            netObj.Spawn();
            return obj;
        }

        private static async UniTask AbortLoading()
        {
            await NetGame.ReturnToMenu();
            ModalUIScript.ShowGeneric(Instance.loadFailedMsg);
        }

        public enum States { Idle, Saving, Loading }
    }
}
