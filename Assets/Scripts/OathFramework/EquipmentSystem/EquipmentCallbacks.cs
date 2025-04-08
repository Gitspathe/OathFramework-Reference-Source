using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{
    public class EquipmentCallbacks
    {
        public EquipmentCallbacksAccessor Access { get; private set; }
        
        private LockableHashSet<IEquipmentUseCallback> useCallbacks                 = new();
        private LockableHashSet<IEquipmentSwapCallback> swapCallbacks               = new();
        private LockableHashSet<IEquipmentReloadCallback> reloadCallbacks           = new();
        private LockableHashSet<IEquipmentBeginReloadCallback> beginReloadCallbacks = new();

        public EquipmentCallbacks()
        {
            Access = new EquipmentCallbacksAccessor(this);
        }
        
        public void Register(IEquipmentUseCallback callback) => useCallbacks.Add(callback);
        public void Register(IEquipmentSwapCallback callback) => swapCallbacks.Add(callback);
        public void Register(IEquipmentReloadCallback callback) => reloadCallbacks.Add(callback);
        public void Register(IEquipmentBeginReloadCallback callback) => beginReloadCallbacks.Add(callback);

        public void Unregister(IEquipmentUseCallback callback) => useCallbacks.Remove(callback);
        public void Unregister(IEquipmentSwapCallback callback) => swapCallbacks.Remove(callback);
        public void Unregister(IEquipmentReloadCallback callback) => reloadCallbacks.Remove(callback);
        public void Unregister(IEquipmentBeginReloadCallback callback) => beginReloadCallbacks.Remove(callback);

        public class EquipmentCallbacksAccessor : CallbackAccessor
        {
            private EquipmentCallbacks callbacks;
            
            public EquipmentCallbacksAccessor(EquipmentCallbacks callbacks)
            {
                this.callbacks = callbacks;
            }
            
            public void OnUse(AccessToken token, EntityEquipment equipment, Equippable equippable, int ammo)
            {
                EnsureAccess(token);
                if(callbacks.useCallbacks.Count == 0)
                    return;
                
                callbacks.useCallbacks.Lock();
                foreach(IEquipmentUseCallback callback in callbacks.useCallbacks.Current) {
                    try {
                        callback.OnEquipmentUse(equipment, equippable, ammo);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.useCallbacks.Unlock();
            }
        
            public void OnSwap(AccessToken token, EntityEquipment equipment, Equippable from, Equippable to)
            {
                EnsureAccess(token);
                if(callbacks.swapCallbacks.Count == 0)
                    return;
                
                callbacks.swapCallbacks.Lock();
                foreach(IEquipmentSwapCallback callback in callbacks.swapCallbacks.Current) {
                    try {
                        callback.OnEquipmentSwap(equipment, from, to);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.swapCallbacks.Unlock();
            }
        
            public void OnReload(AccessToken token, EntityEquipment equipment, Equippable equippable, int amount)
            {
                EnsureAccess(token);
                if(callbacks.reloadCallbacks.Count == 0)
                    return;
                
                callbacks.reloadCallbacks.Lock();
                foreach(IEquipmentReloadCallback callback in callbacks.reloadCallbacks.Current) {
                    try {
                        callback.OnEquipmentReload(equipment, equippable, amount);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.reloadCallbacks.Unlock();
            }
            
            public void OnBeginReload(AccessToken token, EntityEquipment equipment, Equippable equippable)
            {
                EnsureAccess(token);
                if(callbacks.beginReloadCallbacks.Count == 0)
                    return;
                
                callbacks.beginReloadCallbacks.Lock();
                foreach(IEquipmentBeginReloadCallback callback in callbacks.beginReloadCallbacks.Current) {
                    try {
                        callback.OnEquipmentBeginReload(equipment, equippable);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.beginReloadCallbacks.Unlock();
            }
        }
    }

    public interface IEquipmentUseCallback
    {
        void OnEquipmentUse(EntityEquipment equipment, Equippable equippable, int ammo);
    }

    public interface IEquipmentSwapCallback
    {
        void OnEquipmentSwap(EntityEquipment equipment, Equippable from, Equippable to);
    }

    public interface IEquipmentReloadCallback
    {
        void OnEquipmentReload(EntityEquipment equipment, Equippable equippable, int amount);
    }
    
    public interface IEquipmentBeginReloadCallback
    {
        void OnEquipmentBeginReload(EntityEquipment equipment, Equippable equippable);
    }

}
