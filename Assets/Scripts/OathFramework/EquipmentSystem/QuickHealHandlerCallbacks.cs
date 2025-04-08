using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{
    public sealed class QuickHealHandlerCallbacks
    {
        private LockableHashSet<IOnUseQuickHealCallback> useCallbacks = new();
        
        public Accessor Access { get; private set; }

        public QuickHealHandlerCallbacks()
        {
            Access = new Accessor(this);
        }

        public void Register(IOnUseQuickHealCallback callback) => useCallbacks.Add(callback);
        
        public void Unregister(IOnUseQuickHealCallback callback) => useCallbacks.Remove(callback);

        public sealed class Accessor : CallbackAccessor
        {
            private QuickHealHandlerCallbacks callbacks;

            public Accessor(QuickHealHandlerCallbacks callbacks)
            {
                this.callbacks = callbacks;
            }

            public void OnUseQuickHeal(AccessToken token, QuickHealHandler handler, bool auxOnly)
            {
                EnsureAccess(token);
                if(callbacks.useCallbacks.Count == 0)
                    return;
                
                callbacks.useCallbacks.Lock();
                foreach(IOnUseQuickHealCallback callback in callbacks.useCallbacks.Current) {
                    try {
                        callback.OnUseQuickHeal(handler, auxOnly);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.useCallbacks.Unlock();
            }
        }
    }

    public interface IOnUseQuickHealCallback
    {
        void OnUseQuickHeal(QuickHealHandler handler, bool auxOnly);
    }
}
