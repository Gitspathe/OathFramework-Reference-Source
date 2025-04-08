using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.AbilitySystem
{
    public sealed class AbilityHandlerCallbacks
    {
        private AbilityHandler handler;
        private LockableOrderedList<IOnAbilityInvoked> invokedCallbacks           = new();
        private LockableOrderedList<IOnAbilityActivated> activatedCallbacks       = new();
        private LockableOrderedList<IOnAbilityChargeDecrement> decrementCallbacks = new();
        private LockableOrderedList<IOnAbilityDeactivated> deactivatedCallbacks   = new();
        private LockableOrderedList<IOnAbilityCancelled> cancelledCallbacks       = new();

        public Accessor Access { get; private set; }

        public AbilityHandlerCallbacks(AbilityHandler handler)
        {
            this.handler = handler;
            Access       = new Accessor(this);
        }
        
        public void Register(IOnAbilityInvoked callback)         => invokedCallbacks.AddUnique(callback);
        public void Register(IOnAbilityActivated callback)       => activatedCallbacks.AddUnique(callback);
        public void Register(IOnAbilityChargeDecrement callback) => decrementCallbacks.AddUnique(callback);
        public void Register(IOnAbilityDeactivated callback)     => deactivatedCallbacks.AddUnique(callback);
        public void Register(IOnAbilityCancelled callback)       => cancelledCallbacks.AddUnique(callback);

        public void Unregister(IOnAbilityInvoked callback)         => invokedCallbacks.Remove(callback);
        public void Unregister(IOnAbilityActivated callback)       => activatedCallbacks.Remove(callback);
        public void Unregister(IOnAbilityChargeDecrement callback) => decrementCallbacks.Remove(callback);
        public void Unregister(IOnAbilityDeactivated callback)     => deactivatedCallbacks.Remove(callback);
        public void Unregister(IOnAbilityCancelled callback)       => cancelledCallbacks.Remove(callback);

        public sealed class Accessor : CallbackAccessor
        {
            private AbilityHandlerCallbacks callbacks;
            
            public void OnAbilityInvoked(AccessToken token, Ability ability, bool auxOnly)
            {
                EnsureAccess(token);
                if(callbacks.invokedCallbacks.Count == 0)
                    return;
                
                callbacks.invokedCallbacks.Lock();
                foreach(IOnAbilityInvoked callback in callbacks.invokedCallbacks.Current) {
                    try {
                        callback.OnAbilityInvoked(callbacks.handler, ability, auxOnly);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.invokedCallbacks.Unlock();
            }
            
            public void OnAbilityActivated(AccessToken token, Ability ability, bool auxOnly)
            {
                EnsureAccess(token);
                if(callbacks.activatedCallbacks.Count == 0)
                    return;
                
                callbacks.activatedCallbacks.Lock();
                foreach(IOnAbilityActivated callback in callbacks.activatedCallbacks.Current) {
                    try {
                        callback.OnAbilityActivated(callbacks.handler, ability, auxOnly);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.activatedCallbacks.Unlock();
            }
            
            public void OnAbilityDeactivated(AccessToken token, Ability ability, bool auxOnly)
            {
                EnsureAccess(token);
                if(callbacks.deactivatedCallbacks.Count == 0)
                    return;
                
                callbacks.deactivatedCallbacks.Lock();
                foreach(IOnAbilityDeactivated callback in callbacks.deactivatedCallbacks.Current) {
                    try {
                        callback.OnAbilityDeactivated(callbacks.handler, ability, auxOnly);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.deactivatedCallbacks.Unlock();
            }
            
            public void OnAbilityChargeDecrement(AccessToken token, Ability ability, bool auxOnly)
            {
                EnsureAccess(token);
                if(callbacks.decrementCallbacks.Count == 0)
                    return;
                
                callbacks.decrementCallbacks.Lock();
                foreach(IOnAbilityChargeDecrement callback in callbacks.decrementCallbacks.Current) {
                    try {
                        callback.OnAbilityChargeDecrement(callbacks.handler, ability, auxOnly);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.decrementCallbacks.Unlock();
            }
            
            public void OnAbilityCancelled(AccessToken token, Ability ability, bool auxOnly)
            {
                EnsureAccess(token);
                if(callbacks.cancelledCallbacks.Count == 0)
                    return;
                
                callbacks.cancelledCallbacks.Lock();
                foreach(IOnAbilityCancelled callback in callbacks.cancelledCallbacks.Current) {
                    try {
                        callback.OnAbilityCancelled(callbacks.handler, ability, auxOnly);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.cancelledCallbacks.Unlock();
            }

            public Accessor(AbilityHandlerCallbacks callbacks)
            {
                this.callbacks = callbacks;
            }
        }
    }
    
    public interface IAbilityCallback : ILockableOrderedListElement { }

    public interface IOnAbilityInvoked : IAbilityCallback
    {
        void OnAbilityInvoked(AbilityHandler handler, Ability ability, bool auxOnly);
    }
    
    public interface IOnAbilityActivated : IAbilityCallback
    {
        void OnAbilityActivated(AbilityHandler handler, Ability ability, bool auxOnly);
    }
    
    public interface IOnAbilityDeactivated : IAbilityCallback
    {
        void OnAbilityDeactivated(AbilityHandler handler, Ability ability, bool auxOnly);
    }
    
    public interface IOnAbilityChargeDecrement : IAbilityCallback
    {
        void OnAbilityChargeDecrement(AbilityHandler handler, Ability ability, bool auxOnly);
    }

    public interface IOnAbilityCancelled : IAbilityCallback
    {
        void OnAbilityCancelled(AbilityHandler handler, Ability ability, bool auxOnly);
    }
}
