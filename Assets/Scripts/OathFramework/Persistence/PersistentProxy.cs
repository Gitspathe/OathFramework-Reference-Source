using OathFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Persistence
{
    public abstract partial class PersistentProxy : MonoBehaviour
    {
        [field: SerializeField] public string SpawnID                    { get; private set; }

        [field: SerializeField] public VerificationFailAction FailAction { get; private set; } = VerificationFailAction.Continue;
        
        public bool Enabled                                 { get; private set; }
        public bool IsGlobal                                { get; private set; }
        public TransformData Transform                      { get; private set; }
        public Dictionary<string, ComponentData> Components { get; private set; }
        
        public string ID {
            get => id;
            private set {
                if(value == id)
                    return;

                Scene?.Unregister(this);
                id = value;
                if(string.IsNullOrEmpty(id))
                    return;
                
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
                if(string.IsNullOrEmpty(id))
                    return;
                
                Scene?.Register(this);
            }
        }
        private PersistentScene scene;

        private void Awake()
        {
            AssignScene();
            OnInitialize();
        }

        public void AssignID(in Data data)
        {
            IsGlobal = data.IsGlobal;
            if(IsGlobal) {
                DontDestroyOnLoad(gameObject);
            }
            AssignScene();
            ID = data.ID;
            OnAssignID();
        }
        
        public void AssignData(in Data data)
        {
            Enabled    = data.Enabled;
            Transform  = data.Transform;
            Components = data.Components;
            OnAssignData();
        }
        
        private void AssignScene()
        {
            if(IsGlobal) {
                Scene = PersistenceManager.GlobalScene;
            } else if(PersistenceManager.State == PersistenceManager.States.Idle) {
                // TODO: How to handle Scene without persistence?
                Scene = SceneScript.Main.Persistence ?? PersistenceManager.GlobalScene;
            }
        }

        public void TriggerLoaded()
        {
            OnLoaded();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnAssignID() { }
        protected virtual void OnAssignData() { }
        protected virtual void OnLoaded() { }
        
        protected virtual void OnDestroy()
        {
            Scene = null;
        }
    }
}
