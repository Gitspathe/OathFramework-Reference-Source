using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EquipmentSystem;
using OathFramework.UI.Info;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.AbilitySystem
{
    public abstract class Ability : IEquatable<Ability>
    {
        public abstract string LookupKey         { get; }
        public abstract ushort? DefaultID        { get; }
        public ushort ID                         { get; private set; }
        public AbilityInfo Info                  { get; private set; }
        public virtual bool IsInstant            => true;
        public virtual bool AutoNetSync          => false; // Automatic Owner -> Server/Client sync.
        public virtual bool AutoPersistenceSync  => false; // Automatic save file sync.
        public virtual bool AutoCooldownHandling => true;  // If cooldowns are handled like basic timers.
        public virtual bool RemoveOnDeath        => true;
        public virtual bool HasCooldown          => true;
        public virtual bool HasCharges           => true;
        public virtual bool ChargeWhenActive     => false;
        public virtual bool AutoChargeDecrement  => true;

        protected LockableHashSet<Entity> Assigned { get; } = new();
        
        public virtual float GetCooldown(Entity entity)          => HasCooldown ? entity.Abilities.GetCooldown(this) : 0.0f;
        public virtual float GetMaxCooldown(Entity entity)       => 10.0f;
        public virtual bool GetIsActive(Entity entity)           => false;
        public virtual float GetMaxChargeProgress(Entity entity) => 100.0f;
        public virtual byte GetMaxCharges(Entity entity)         => 1;
        public virtual byte GetCharges(Entity entity)            => HasCharges ? entity.Abilities.GetCharges(this) : (byte)1;
        
        public virtual Dictionary<string, string> GetLocalizedParams(Entity entity) => null;

        public virtual bool GetUsable(Entity entity)
        {
            if(HasCharges && (GetCharges(entity) == 0 || GetMaxCharges(entity) == 0))
                return false;
            if(HasCooldown && GetCooldown(entity) > 0.0f)
                return false;

            return !GetIsActive(entity);
        }
        
        public void PostCtor()
        {
            if(!AbilityManager.Register(this, out ushort id)) {
                Debug.LogError($"Failed to register Ability of Type '{GetType()}'");
                return;
            }
            RegisterInfoDescCallback();
            ID = id;
        }
        
        private void RegisterInfoDescCallback()
        {
            AbilityInfo info = UIInfoManager.GetAbilityInfo(LookupKey);
            if(info == null)
                return;

            info.Ability = this;
            Info = info;
        }
        
        public bool Equals(Ability other)
        {
            if(ReferenceEquals(null, other))
                return false;
            
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj))
                return false;
            
            return obj.GetType() == GetType() && Equals((Ability)obj);
        }
        
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() => ID;

        protected void ApplyClipData(Entity invoker)
        {
            if(invoker.TryGetComponent(out EntityAudio audio)) {
                audio.ApplyClipData(Info.AudioData);
            }
        }
        
        public void Initialize()                                          => OnInitialize();
        public void Activate(Entity invoker, bool auxOnly, bool lateJoin) => OnActivate(invoker, auxOnly, lateJoin);
        public void Deactivate(Entity invoker, bool auxOnly)              => OnDeactivate(invoker, auxOnly);

        public void Added(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(Assigned.Count == 0 && this is IUpdateable updateable) {
                AbilityManager.ToUpdate.Add(updateable);
            }
            Assigned.Add(owner);
            OnAdded(owner, auxOnly, lateJoin);
        }

        public void Removed(Entity owner, bool auxOnly, bool lateJoin)
        {
            Assigned.Remove(owner);
            if(Assigned.Count == 0 && this is IUpdateable updateable) {
                AbilityManager.ToUpdate.Remove(updateable);
            }
            OnRemoved(owner, auxOnly, lateJoin);
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnActivate(Entity invoker, bool auxOnly, bool lateJoin) { }
        protected virtual void OnDeactivate(Entity invoker, bool auxOnly) { }
        protected virtual void OnAdded(Entity owner, bool auxOnly, bool lateJoin) { }
        protected virtual void OnRemoved(Entity owner, bool auxOnly, bool lateJoin) { }
        
        public bool IsAssignedTo(Entity entity) => Assigned.Contains(entity);
        
        public bool TryGetAssignedSlotAsEquipmentSlot(Entity owner, out EquipmentSlot slot)
        {
            slot = 0;
            if(!TryGetAssignedSlot(owner, out byte bSlot))
                return false;

            switch(bSlot) {
                case 0: {
                    slot = EquipmentSlot.Special1;
                    return true;
                }
                case 1: {
                    slot = EquipmentSlot.Special2;
                    return true;
                }
                case 2: {
                    slot = EquipmentSlot.Special3;
                    return true;
                }
            }
            return false;
        }
        
        public bool TryGetAssignedSlot(Entity owner, out byte slot)
        {
            slot = 0;
            if(!(owner.Abilities is PlayerAbilityHandler playerAbilities))
                return false;

            return playerAbilities.TryGetAbility(this, out _, out slot);
        }
    }
    
    public interface IActionAbility
    {
        bool SyncActivation     { get; }
        string ActionAnimParams { get; }
        void OnInvoked(Entity invoker, bool auxOnly, bool lateJoin);
        void OnCancelled(Entity invoker, bool auxOnly, bool lateJoin);
    }
}
