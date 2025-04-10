using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public class DodgeHandlerCallbacks
    {
        private LockableOrderedList<IEntityDodgedAttackCallback> dodgedAttackCallbacks = new();

        public Accessor Access { get; private set; }

        public DodgeHandlerCallbacks()
        {
            Access = new Accessor(this);
        }
        
        public void Register(IEntityDodgedAttackCallback callback) => dodgedAttackCallbacks.AddUnique(callback);

        public void Unregister(IEntityDodgedAttackCallback callback) => dodgedAttackCallbacks.Remove(callback);
        
        public sealed class Accessor : CallbackAccessor
        {
            private DodgeHandlerCallbacks callbacks;

            public Accessor(DodgeHandlerCallbacks callbacks)
            {
                this.callbacks = callbacks;
            }

            public void OnDodgedAttack(AccessToken token, Entity entity, in DamageValue damageVal, int dodgeCount)
            {
                EnsureAccess(token);
                if(callbacks.dodgedAttackCallbacks.Count == 0)
                    return;

                callbacks.dodgedAttackCallbacks.Lock();
                foreach(IEntityDodgedAttackCallback callback in callbacks.dodgedAttackCallbacks.Current) {
                    try {
                        callback.OnDodgedAttack(entity, damageVal, dodgeCount);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.dodgedAttackCallbacks.Unlock();
            }
        }
    }

    public interface IEntityDodgedAttackCallback : IEntityCallback
    {
        void OnDodgedAttack(Entity entity, in DamageValue damageVal, int dodgeCount);
    }
}
