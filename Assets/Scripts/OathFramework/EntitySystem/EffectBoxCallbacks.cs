using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public sealed class EffectBoxCallbacks
    {
        private LockableHashSet<IEffectBoxDamagedEntity> onDamage = new();
        private LockableHashSet<IEffectBoxHealedEntity> onHeal    = new();

        public Accessor Access { get; private set; }

        public EffectBoxCallbacks()
        {
            Access = new Accessor(this);
        }
        
        public void Register(IEffectBoxDamagedEntity callback) => onDamage.Add(callback);
        public void Register(IEffectBoxHealedEntity callback) => onHeal.Add(callback);

        public void Unregister(IEffectBoxDamagedEntity callback) => onDamage.Remove(callback);
        public void Unregister(IEffectBoxHealedEntity callback) => onHeal.Remove(callback);
        
        public sealed class Accessor : CallbackAccessor
        {
            private EffectBoxCallbacks callbacks;

            public Accessor(EffectBoxCallbacks callbacks)
            {
                this.callbacks = callbacks;
            }
            
            public void OnDamagedEntity(AccessToken token, IEntity entity, in DamageValue damageValue)
            {
                EnsureAccess(token);
                if(callbacks.onDamage.Count == 0)
                    return;
                
                callbacks.onDamage.Lock();
                foreach(IEffectBoxDamagedEntity damage in callbacks.onDamage.Current) {
                    try {
                        damage.OnEffectBoxDamagedEntity(entity, in damageValue);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.onDamage.Unlock();
            }
            
            public void OnHealedEntity(AccessToken token, IEntity entity, in HealValue healValue)
            {
                EnsureAccess(token);
                if(callbacks.onHeal.Count == 0)
                    return;
                
                callbacks.onHeal.Lock();
                foreach(IEffectBoxHealedEntity heal in callbacks.onHeal.Current) {
                    try {
                        heal.OnEffectBoxHealedEntity(entity, in healValue);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.onHeal.Unlock();
            }
        }
    }
    
    public interface IEffectBoxDamagedEntity
    {
        void OnEffectBoxDamagedEntity(IEntity entity, in DamageValue damageValue);
    }
    
    public interface IEffectBoxHealedEntity
    {
        void OnEffectBoxHealedEntity(IEntity entity, in HealValue healValue);
    }
}
