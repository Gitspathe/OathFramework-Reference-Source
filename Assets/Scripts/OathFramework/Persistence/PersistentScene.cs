using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Persistence
{
    [Serializable]
    public partial class PersistentScene
    {
        [field: SerializeField] public string ID            { get; private set; }
        public Dictionary<string, PersistentObject> Objects { get; private set; } = new();
        public Dictionary<string, PersistentProxy> Proxies  { get; private set; } = new();
        
        public bool IsGlobal => ID == "_GLOBAL";
        
        public PersistentScene() { }

        public PersistentScene(string id)
        {
            ID = id;
        }

        public void Clear()
        {
            Objects.Clear();
            Proxies.Clear();
        }

        public void Register(PersistentObject obj)
        {
            if(string.IsNullOrEmpty(obj.ID))
                return;
            
            if(PersistenceManager.RegisterObject(obj.ID, obj)) {
                Objects.Add(obj.ID, obj);
            }
        }

        public void Register(PersistentProxy proxy)
        {
            if(string.IsNullOrEmpty(proxy.ID))
                return;
            
            if(PersistenceManager.RegisterProxy(proxy.ID, proxy)) {
                Proxies.Add(proxy.ID, proxy);
            }
        }

        public void Unregister(PersistentObject obj)
        {
            if(string.IsNullOrEmpty(obj.ID))
                return;
            
            if(PersistenceManager.UnregisterObject(obj.ID)) {
                Objects.Remove(obj.ID);
            }
        }

        public void Unregister(PersistentProxy proxy)
        {
            if(string.IsNullOrEmpty(proxy.ID))
                return;
            
            if(PersistenceManager.UnregisterProxy(proxy.ID)) {
                Proxies.Remove(proxy.ID);
            }
        }
    }
}
