using OathFramework.Core;
using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.EntitySystem.States
{
    public abstract class State : IEquatable<State>
    {
        public abstract string LookupKey         { get; }
        public abstract ushort? DefaultID        { get; }
        public virtual uint Order                => 100;
        public virtual ushort MaxValue           => ushort.MaxValue;
        public virtual float? MaxDuration        => null;
        public virtual bool RemoveAllValOnExpire => false;
        public virtual bool NetSync              => false; // Owner -> Server/Client sync.
        public virtual bool PersistenceSync      => false; // Save file sync.
        public virtual bool RemoveOnDeath        => true;
        
        public ushort ID { get; private set; }
        
        public bool Stackable => MaxValue > 1;

        protected LockableHashSet<Entity> Affecting { get; } = new();
        
        public bool Equals(State other)
        {
            if(ReferenceEquals(null, other)) {
                return false;
            }
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) {
                return false;
            }
            return obj.GetType() == GetType() && Equals((State)obj);
        }
        
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() => ID;

        public void PostCtor()
        {
            if(!StateManager.Register(this, out ushort id)) {
                Debug.LogError($"Failed to register Entity State of Type '{GetType()}'");
                return;
            }
            ID = id;
        }

        public void Initialize()
        {
            Affecting.Lock();
            try {
                OnInitialize();
            } catch(Exception e) {
                Debug.LogError(e);
            }
            Affecting.Unlock();
        }

        public void ApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            Affecting.Lock();
            try {
                OnApplyStatChanges(entity, lateJoin, val);
            } catch(Exception e) {
                Debug.LogError(e);
            }
            Affecting.Unlock();
        }

        public void Applied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            try {
                if(originalVal.HasValue) {
                    Exec();
                    return;
                }

                if(Affecting.Count == 0 && this is IUpdateable updateable) {
                    StateManager.ToUpdate.Add(updateable);
                }
                Affecting.Add(entity);
                Exec();
            } catch(Exception e) {
                Debug.LogError(e);
            }
            Affecting.Unlock();
            return;

            void Exec()
            {
                Affecting.Lock();
                OnApplied(entity, lateJoin, val, originalVal);
                Affecting.Unlock();
            }
        }

        public void Removed(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            try {
                if(originalVal - val != 0) {
                    Exec();
                    return;
                }

                Affecting.Remove(entity);
                if(Affecting.Count == 0 && this is IUpdateable updateable) {
                    StateManager.ToUpdate.Remove(updateable);
                }
                Exec();
            } catch(Exception e) {
                Debug.LogError(e);
            }
            Affecting.Unlock();
            return;
            
            void Exec()
            {
                Affecting.Lock();
                OnRemoved(entity, lateJoin, val, originalVal);
                Affecting.Unlock();
            }
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val) { }
        protected virtual void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal) { }
        protected virtual void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal) { }

        public bool IsAffecting(Entity entity) => Affecting.Current.Contains(entity);
    }
}
