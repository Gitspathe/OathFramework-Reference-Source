using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.EntitySystem
{

    public sealed class EntityCallbacks
    {
        private Entity entity;
        private LockableOrderedList<IEntityInitCallback> initCallbacks                   = new();
        private LockableOrderedList<IEntityParallelUpdate> threadedUpdates               = new();
        private LockableOrderedList<IEntityPreHealCallback> preHealCallbacks             = new();
        private LockableOrderedList<IEntityHealCallback> healCallbacks                   = new();
        private LockableOrderedList<IEntityPreTakeDamageCallback> preTakeDamageCallbacks = new();
        private LockableOrderedList<IEntityTakeDamageCallback> takeDamageCallbacks       = new();
        private LockableOrderedList<IEntityPreDieCallback> preDieCallbacks               = new();
        private LockableOrderedList<IEntityDieCallback> dieCallbacks                     = new();
        private LockableOrderedList<IEntityPreDealDamageCallback> preDealDamageCallbacks = new();
        private LockableOrderedList<IEntityDealtDamageCallback> dealtDamageCallbacks     = new();
        private LockableOrderedList<IEntityScoreKillCallback> scoredKillCallbacks        = new();
        private LockableOrderedList<IEntityStaggerCallback> staggerCallbacks             = new();
        
        public Accessor Access { get; private set; }

        public EntityCallbacks()
        {
            Access = new Accessor(this);
        }
        
        public void RegisterInitCallbacks(Entity entity)
        {
            this.entity = entity;
            foreach(IEntityInitCallback callback in entity.GetComponentsInChildren<IEntityInitCallback>(true)) {
                initCallbacks.AddUnique(callback);
            }
        }
        
        public void Register(IEntityInitCallback callback)
        {
            bool exists = initCallbacks.AddUnique(callback);
            if(exists || !entity.IsSpawned)
                return;

            try {
                callback.OnEntityInitialize(entity);
            } catch(Exception e) {
                Debug.LogError(e);
            }
        }
        
        public void Register(IEntityParallelUpdate callback) => threadedUpdates.AddUnique(callback);
        public void Register(IEntityPreHealCallback callback) => preHealCallbacks.AddUnique(callback);
        public void Register(IEntityHealCallback callback) => healCallbacks.AddUnique(callback);
        public void Register(IEntityPreTakeDamageCallback callback) => preTakeDamageCallbacks.AddUnique(callback);
        public void Register(IEntityTakeDamageCallback callback) => takeDamageCallbacks.AddUnique(callback);
        public void Register(IEntityPreDieCallback callback) => preDieCallbacks.AddUnique(callback);
        public void Register(IEntityDieCallback callback) => dieCallbacks.AddUnique(callback);
        public void Register(IEntityPreDealDamageCallback callback) => preDealDamageCallbacks.AddUnique(callback);
        public void Register(IEntityDealtDamageCallback callback) => dealtDamageCallbacks.AddUnique(callback);
        public void Register(IEntityScoreKillCallback callback) => scoredKillCallbacks.AddUnique(callback);
        public void Register(IEntityStaggerCallback callback) => staggerCallbacks.AddUnique(callback);
        
        public void Unregister(IEntityParallelUpdate callback) => threadedUpdates.Remove(callback);
        public void Unregister(IEntityPreHealCallback callback) => preHealCallbacks.Remove(callback);
        public void Unregister(IEntityHealCallback callback) => healCallbacks.Remove(callback);
        public void Unregister(IEntityPreTakeDamageCallback callback) => preTakeDamageCallbacks.Remove(callback);
        public void Unregister(IEntityTakeDamageCallback callback) => takeDamageCallbacks.Remove(callback);
        public void Unregister(IEntityPreDieCallback callback) => preDieCallbacks.Remove(callback);
        public void Unregister(IEntityDieCallback callback) => dieCallbacks.Remove(callback);
        public void Unregister(IEntityPreDealDamageCallback callback) => preDealDamageCallbacks.Remove(callback);
        public void Unregister(IEntityDealtDamageCallback callback) => dealtDamageCallbacks.Remove(callback);
        public void Unregister(IEntityScoreKillCallback callback) => scoredKillCallbacks.Remove(callback);
        public void Unregister(IEntityStaggerCallback callback) => staggerCallbacks.Remove(callback);
        
        public sealed class Accessor : CallbackAccessor
        {
            private EntityCallbacks callbacks;

            public Accessor(EntityCallbacks callbacks)
            {
                this.callbacks = callbacks;
            }
            
            public void OnInitialize(AccessToken token, Entity entity)
            {
                EnsureAccess(token);
                if(callbacks.initCallbacks.Count == 0)
                    return;
                
                callbacks.initCallbacks.Lock();
                foreach(IEntityInitCallback callback in callbacks.initCallbacks.Current) {
                    try {
                        callback.OnEntityInitialize(entity);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.initCallbacks.Unlock();
            }

            public void OnThreadedUpdate(AccessToken token)
            {
                EnsureAccess(token);
                if(callbacks.threadedUpdates.Count == 0)
                    return;
                
                callbacks.threadedUpdates.Lock();
                foreach(IEntityParallelUpdate update in callbacks.threadedUpdates.Current) {
                    try {
                        update.OnUpdateParallel(callbacks.entity);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.threadedUpdates.Unlock();
            }

            public void OnPreHeal(AccessToken token, bool fromRpc, ref HealValue val)
            {
                EnsureAccess(token);
                if(callbacks.preHealCallbacks.Count == 0)
                    return;
                
                callbacks.preHealCallbacks.Lock();
                foreach(IEntityPreHealCallback callback in callbacks.preHealCallbacks.Current) {
                    try {
                        callback.OnPreHeal(callbacks.entity, fromRpc, ref val);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.preHealCallbacks.Unlock();
            }

            public void OnHeal(AccessToken token, bool fromRpc, in HealValue val)
            {
                EnsureAccess(token);
                if(callbacks.healCallbacks.Count == 0)
                    return;
                
                callbacks.healCallbacks.Lock();
                foreach(IEntityHealCallback callback in callbacks.healCallbacks.Current) {
                    try {
                        callback.OnHeal(callbacks.entity, fromRpc, val);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.healCallbacks.Unlock();
            }

            public void OnPreTakeDamage(AccessToken token, bool fromRpc, bool isTest, ref DamageValue val)
            {
                EnsureAccess(token);
                if(callbacks.preTakeDamageCallbacks.Count == 0)
                    return;
                
                callbacks.preTakeDamageCallbacks.Lock();
                foreach(IEntityPreTakeDamageCallback callback in callbacks.preTakeDamageCallbacks.Current) {
                    try {
                        callback.OnPreDamage(callbacks.entity, fromRpc, isTest, ref val);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.preTakeDamageCallbacks.Unlock();
            }

            public void OnTakeDamage(AccessToken token, bool fromRpc, in DamageValue val)
            {
                EnsureAccess(token);
                if(callbacks.takeDamageCallbacks.Count == 0)
                    return;
                
                callbacks.takeDamageCallbacks.Lock();
                foreach(IEntityTakeDamageCallback callback in callbacks.takeDamageCallbacks.Current) {
                    try {
                        callback.OnDamage(callbacks.entity, fromRpc, in val);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.takeDamageCallbacks.Unlock();
            }

            public bool OnPreDie(AccessToken token, in DamageValue lastDamageVal)
            {
                EnsureAccess(token);
                if(callbacks.preDieCallbacks.Count == 0)
                    return false;
                
                callbacks.preDieCallbacks.Lock();
                bool ret = false;
                foreach(IEntityPreDieCallback callback in callbacks.preDieCallbacks.Current) {
                    try {
                        if(!callback.OnPreDie(callbacks.entity, lastDamageVal))
                            continue;

                        ret = true;
                    } catch(Exception e) {
                        Debug.LogError(e);
                        continue;
                    }
                    break;
                }
                callbacks.preDieCallbacks.Unlock();
                return ret;
            }

            public void OnDie(AccessToken token, in DamageValue lastDamageVal)
            {
                EnsureAccess(token);
                if(callbacks.dieCallbacks.Count == 0)
                    return;
                
                callbacks.dieCallbacks.Lock();
                foreach(IEntityDieCallback callback in callbacks.dieCallbacks.Current) {
                    try {
                        callback.OnDie(callbacks.entity, lastDamageVal);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.dieCallbacks.Unlock();
            }

            public void OnPreDealDamage(AccessToken token, Entity target, bool isTest, ref DamageValue val)
            {
                EnsureAccess(token);
                if(callbacks.preDealDamageCallbacks.Count == 0)
                    return;
                
                callbacks.preDealDamageCallbacks.Lock();
                foreach(IEntityPreDealDamageCallback callback in callbacks.preDealDamageCallbacks.Current) {
                    try {
                        callback.OnPreDealDamage(callbacks.entity, target, isTest, ref val);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.preDealDamageCallbacks.Unlock();
            }

            public void OnDealtDamage(AccessToken token, Entity target, bool fromRpc, in DamageValue val)
            {
                EnsureAccess(token);
                if(callbacks.dealtDamageCallbacks.Count == 0)
                    return;
                
                callbacks.dealtDamageCallbacks.Lock();
                foreach(IEntityDealtDamageCallback callback in callbacks.dealtDamageCallbacks.Current) {
                    try {
                        callback.OnDealtDamage(callbacks.entity, target, fromRpc, in val);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.dealtDamageCallbacks.Unlock();
            }

            public void OnScoredKill(AccessToken token, IEntity other, in DamageValue lastDamageVal, float ratio)
            {
                EnsureAccess(token);
                if(callbacks.scoredKillCallbacks.Count == 0)
                    return;
                
                callbacks.scoredKillCallbacks.Lock();
                foreach(IEntityScoreKillCallback callback in callbacks.scoredKillCallbacks.Current) {
                    try {
                        callback.OnScoredKill(callbacks.entity, other, in lastDamageVal, ratio);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.scoredKillCallbacks.Unlock();
            }

            public void OnStagger(AccessToken token, StaggerStrength strength, Entity instigator)
            {
                EnsureAccess(token);
                if(callbacks.staggerCallbacks.Count == 0)
                    return;
                
                callbacks.staggerCallbacks.Lock();
                foreach(IEntityStaggerCallback callback in callbacks.staggerCallbacks.Current) {
                    try {
                        callback.OnStagger(callbacks.entity, strength, instigator);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.staggerCallbacks.Unlock();
            }
        }
    }
    
    public interface IEntityCallback : ILockableOrderedListElement {}

    public interface IEntityInitCallback : IEntityCallback
    {
        void OnEntityInitialize(Entity entity);
    }

    public interface IEntityParallelUpdate : IEntityInitCallback
    {
        void OnUpdateParallel(Entity entity);
    }

    public interface IEntityPreHealCallback : IEntityCallback
    {
        void OnPreHeal(Entity entity, bool fromRpc, ref HealValue val);
    }

    public interface IEntityHealCallback : IEntityCallback
    {
        void OnHeal(Entity entity, bool fromRpc, in HealValue val);
    }

    public interface IEntityPreTakeDamageCallback : IEntityCallback
    {
        void OnPreDamage(Entity entity, bool fromRpc, bool isTest, ref DamageValue val);
    }

    public interface IEntityTakeDamageCallback : IEntityCallback
    {
        void OnDamage(Entity entity, bool fromRpc, in DamageValue val);
    }

    public interface IEntityScoreKillCallback : IEntityCallback
    {
        void OnScoredKill(Entity entity, IEntity other, in DamageValue lastDamageVal, float ratio);
    }

    public interface IEntityPreDieCallback : IEntityCallback
    {
        bool OnPreDie(Entity entity, in DamageValue lastDamageVal);
    }

    public interface IEntityDieCallback : IEntityCallback
    {
        void OnDie(Entity entity, in DamageValue lastDamageVal);
    }

    public interface IEntityPreDealDamageCallback : IEntityCallback
    {
        void OnPreDealDamage(Entity source, Entity target, bool isTest, ref DamageValue damageVal);
    }

    public interface IEntityDealtDamageCallback : IEntityCallback
    {
        void OnDealtDamage(Entity source, Entity target, bool fromRpc, in DamageValue damageVal);
    }

    public interface IEntityStaggerCallback : IEntityCallback
    {
        void OnStagger(Entity entity, StaggerStrength strength, Entity instigator);
    }

}
