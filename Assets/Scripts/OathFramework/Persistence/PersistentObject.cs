using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.Pooling;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace OathFramework.Persistence
{
    public partial class PersistentObject : MonoBehaviour, IPoolableComponent
    {
        [field: SerializeField] public bool DisablePooling               { get; private set; }
        [field: SerializeField] public VerificationFailAction FailAction { get; private set; } = VerificationFailAction.Continue;
        [field: SerializeField] public ObjectType Type                   { get; private set; }
        
        [field: SerializeField]
        [field: ValueDropdown("GetPrefabs", AppendNextDrawer = true, DoubleClickToConfirm = true, OnlyChangeValueOnConfirm = true)]
        public string SpawnID { get; set; }
        
#if UNITY_EDITOR
        [Preserve, MethodImpl(MethodImplOptions.NoOptimization)]
        public static IEnumerable GetPrefabs()
        {
            ValueDropdownList<string> ret = new();
            List<string> paths = AssetDatabase.FindAssets("t:PrefabCollection").Select(AssetDatabase.GUIDToAssetPath).ToList();
            paths.AddRange(AssetDatabase.FindAssets("t:ProxyPrefabCollection").Select(AssetDatabase.GUIDToAssetPath));
            foreach(string path in paths) {
                try {
                    ScriptableObject collection = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    switch (collection) {
                        case PrefabCollection prefabCollection: {
                            foreach(PrefabCollection.Node node in prefabCollection.Prefabs) {
                                ret.Add(node.DropdownValue, node.ID);
                            }
                        } break;
                        case ProxyPrefabCollection proxyCollection: {
                            foreach(ProxyPrefabCollection.Node node in proxyCollection.Proxies) {
                                ret.Add(node.DropdownValue, node.ID);
                            }
                        } break;
                    }
                } catch{ /* ignored */ }
            }
            return ret;
        }
#endif
        
        public string ID {
            get => id;
            private set {
                if(value == id)
                    return;

                Scene?.Unregister(this);
                id = value;
                Scene?.Register(this);
            }
        }
        private string id;
        
        public PersistentScene Scene {
            get => scene;
            private set {
                if(value == scene)
                    return;
                
                Scene?.Unregister(this);
                scene = value;
                Scene?.Register(this);
            }
        }
        private PersistentScene scene;

        public bool IsNetworked   { get; private set; }
        public bool IsLoaded      { get; private set; }
        public bool IsPooled      { get; private set; }
        public bool GenerateProxy => Type == ObjectType.ProxyGlobal || Type == ObjectType.ProxyPrefab;

        PoolableGameObject IPoolableComponent.PoolableGO { get; set; }
        
        public Dictionary<string, IPersistableComponent> Persistables { get; private set; } = new();
        
        private void Awake()
        {
            IsPooled    = !DisablePooling && TryGetComponent<PoolableGameObject>(out _);
            IsNetworked = TryGetComponent<NetworkBehaviour>(out _);
            RegisterPersistables();
            if(IsPooled)
                return;

            AssignScene();
            GenerateID();
            if(PersistenceManager.State == PersistenceManager.States.Idle) {
                IsLoaded = true;
            }
        }

        private void RegisterPersistables()
        {
            foreach(IPersistableComponent persist in gameObject.GetComponentsInChildren<IPersistableComponent>(true)) {
                Persistables.Add(persist.PersistableID, persist);
            }
        }

        private void AssignScene()
        {
            if(Type == ObjectType.Global || Type == ObjectType.ProxyGlobal) {
                Scene = PersistenceManager.GlobalScene;
            } else {
                // TODO: How to handle Scene without persistence?
                Scene = SceneScript.Main.Persistence ?? PersistenceManager.GlobalScene;
            }
        }

        private void GenerateID()
        {
            switch(Type) {
                case ObjectType.Global: {
                    ID = SpawnID;
                } break;
                case ObjectType.Scene: {
                    ID = StringBuilderCache.Retrieve.Append(Scene.ID).Append("_").Append(SpawnID).ToString();
                } break;
                case ObjectType.ProxyGlobal:
                case ObjectType.ProxyPrefab:
                case ObjectType.Prefab: {
                    ID = PersistenceManager.GenerateRandomID();
                } break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void AssignFromProxy(PersistentProxy proxy)
        {
            string proxyID = proxy.ID;
            Transform t    = transform;
            t.position     = proxy.Transform.Position;
            t.rotation     = proxy.Transform.Rotation;
            t.localScale   = proxy.Transform.Scale;
            SpawnID        = proxy.SpawnID;

            TransformData tData = proxy.Transform;
            if(IsNetworked) {
                SyncNetTransform(in tData);
            }
            
            List<IPersistableComponent> components = StaticObjectPool<List<IPersistableComponent>>.Retrieve();
            components.AddRange(Persistables.Values);
            components.Sort((x, y) => x.Order.CompareTo(y.Order));
            try {
                foreach(IPersistableComponent component in components) {
                    AssignProxyComponent(proxy, component);
                }
            } catch(LoadAbortedException) {
                Abort();
                throw;
            } finally {
                components.Clear();
                StaticObjectPool<List<IPersistableComponent>>.Return(components);
                ID = proxyID;
            }
            Destroy(proxy);
        }

        private void AssignProxyComponent(PersistentProxy proxy, IPersistableComponent component)
        {
            try {
                if(!proxy.Components.TryGetValue(component.PersistableID, out ComponentData cData))
                    throw new NullReferenceException($"Persistent component '{component.PersistableID}' not found.");
                if(!component.Verify(cData, proxy))
                    throw new Exception("Verification failed.");
                
                component.ApplyPersistenceData(cData, proxy);
            } catch(Exception e) {
                Debug.LogError($"Failed to assign persistent proxy data: {e}");
                if(component.VerificationFailAction == VerificationFailAction.Abort)
                    throw new LoadAbortedException($"Aborted loading due to error on persistent proxy data: {e}");
            }
        }

        public void AssignID(in Data data)
        {
            if(Type == ObjectType.Prefab) {
                ID = data.ID;
            }
        }

        public void AssignData(in Data data)
        {
            Transform t  = transform;
            t.position   = data.Transform.Position;
            t.rotation   = data.Transform.Rotation;
            t.localScale = data.Transform.Scale;
            SpawnID      = data.SpawnID;
            
            List<IPersistableComponent> components = StaticObjectPool<List<IPersistableComponent>>.Retrieve();
            components.AddRange(Persistables.Values);
            components.Sort((x, y) => x.Order.CompareTo(y.Order));
            try {
                foreach(IPersistableComponent component in components) {
                    AssignComponent(in data, component);
                }
            } catch(LoadAbortedException) {
                Abort();
                throw;
            } finally {
                components.Clear();
                StaticObjectPool<List<IPersistableComponent>>.Return(components);
            }
            
            if(IsNetworked) {
                SyncNetTransform(in data.Transform);
            }
        }

        private void AssignComponent(in Data data, IPersistableComponent component)
        {
            try {
                if(!data.ComponentData.TryGetValue(component.PersistableID, out ComponentData cData))
                    throw new NullReferenceException($"Persistent component '{component.PersistableID}' not found.");
                if(!component.Verify(cData, null))
                    throw new Exception("Verification failed.");
                
                component.ApplyPersistenceData(cData, null);
            } catch (Exception e) {
                Debug.LogError($"Failed to assign persistent data: {e}");
                if(component.VerificationFailAction == VerificationFailAction.Abort)
                    throw new LoadAbortedException($"Aborted loading due to error on persistent data: {e}");
            }
        }

        private void Abort()
        {
            if(IsNetworked) {
                GetComponent<NetworkObject>().Despawn();
                return;
            } 
            if(IsPooled) {
                GetComponent<PoolableGameObject>().Return();
                return;
            }
            Destroy(gameObject);
        }
        
        private void SyncNetTransform(in TransformData transformData)
        {
            if(!TryGetComponent(out NetworkTransform netTrans))
                return;

            if(!netTrans.IsOwner && !netTrans.IsServerAuthoritative()) {
                if(!(netTrans is ClientNetworkTransform cNetTrans))
                    return;
                
                cNetTrans.ForceTeleportOwnerRpc(transformData.Position, transformData.Rotation, transformData.Scale);
                return;
            }
            netTrans.Teleport(transformData.Position, transformData.Rotation, transformData.Scale);
        }

        public void TriggerLoaded()
        {
            if(IsLoaded)
                return;
            
            IsLoaded = true;
        }

        private void OnDestroy()
        {
            if(Type != ObjectType.Scene) {
                Scene = null;
            }
        }
        
        void IPoolableComponent.OnRetrieve()
        {
            if(!IsPooled)
                return;
            
            AssignScene();
            GenerateID();
            if(PersistenceManager.State == PersistenceManager.States.Idle) {
                IsLoaded = true;
            }
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            if(!IsPooled)
                return;
            
            if(Type != ObjectType.Scene) {
                Scene = null;
            }
            IsLoaded = false;
        }
        
        public enum ObjectType { Global, Scene, Prefab, ProxyGlobal, ProxyPrefab }
    }
}
