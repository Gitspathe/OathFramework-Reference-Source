using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.UI.Info;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.PerkSystem
{
    public abstract class Perk : IEquatable<Perk>
    {
        public abstract string LookupKey        { get; }
        public abstract ushort? DefaultID       { get; }
        public UIPerkInfo UIInfo                { get; private set; }
        public ushort ID                        { get; private set; }
        public virtual bool AutoNetSync         => false; // Automatic Owner -> Server/Client sync.
        public virtual bool AutoPersistenceSync => false; // Automatic save file sync.
        public virtual bool RemoveOnDeath       => true;
        
        protected LockableHashSet<Entity> Assigned { get; } = new();
        
        public virtual Dictionary<string, string> GetLocalizedParams(Entity entity) => null;
        
        public void PostCtor()
        {
            if(!PerkManager.Register(this, out ushort id)) {
                Debug.LogError($"Failed to register Perk of Type '{GetType()}'");
                return;
            }
            RegisterInfoCallback();
            ID = id;
        }
        
        private void RegisterInfoCallback()
        {
            UIPerkInfo info = UIInfoManager.GetPerkInfo(LookupKey);
            if(info == null)
                return;
            
            info.Perk = this;
            UIInfo    = info;
        }
        
        public bool Equals(Perk other)
        {
            if(ReferenceEquals(null, other))
                return false;
            
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj))
                return false;
            
            return obj.GetType() == GetType() && Equals((Perk)obj);
        }
        
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() => ID;
        
        public void Initialize() => OnInitialize();

        public void Added(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(Assigned.Count == 0 && this is IUpdateable updateable) {
                PerkManager.ToUpdate.Add(updateable);
            }
            Assigned.Add(owner);
            OnAdded(owner, auxOnly, lateJoin);
        }

        public void Removed(Entity owner, bool auxOnly, bool lateJoin)
        {
            Assigned.Remove(owner);
            if(Assigned.Count == 0 && this is IUpdateable updateable) {
                PerkManager.ToUpdate.Remove(updateable);
            }
            OnRemoved(owner, auxOnly, lateJoin);
        }
        
        protected virtual void OnInitialize() { }
        protected virtual void OnAdded(Entity owner, bool auxOnly, bool lateJoin) { }
        protected virtual void OnRemoved(Entity owner, bool auxOnly, bool lateJoin) { }
        
        public bool IsAssignedTo(Entity entity) => Assigned.Contains(entity);
    }
}
