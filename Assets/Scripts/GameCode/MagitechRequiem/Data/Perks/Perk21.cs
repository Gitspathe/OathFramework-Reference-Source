using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Projectiles;
using OathFramework.EntitySystem.States;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Gunslinger
    /// Killing an enemy with a pistol grants +20% damage bonus to your next pistol shot. Stacks 3 times. All stacks are lost upon swapping weapons.
    /// </summary>
    public class Perk21 : Perk
    {
        public override string LookupKey => PerkLookup.Perk21.Key;
        public override ushort? DefaultID => PerkLookup.Perk21.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "20" }, { "max_stacks", "3" }, { "duration", "30" } };

        private Dictionary<Entity, Callback> callbacks = new();
        private HashSet<string> appliesTo = new() {
            "core:silenced_pistol", 
            "core:jw_model_19", 
            "core:opus"
        };
        
        public static Perk21 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || ReferenceEquals(owner.Abilities, null) || !(owner.Controller is IEquipmentUserController equipmentUser))
                return;

            Callback callback = StaticObjectPool<Callback>.Retrieve().Init(Instance, owner);
            callbacks[owner] = callback;
            owner.Callbacks.Register((IEntityDealtDamageCallback)callback);
            equipmentUser.Equipment.Callbacks.Register((IEquipmentSwapCallback)callback);
            equipmentUser.Equipment.Projectiles.Callbacks.Register((IOnProjectileDespawned)callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || ReferenceEquals(owner.Abilities, null) || !(owner.Controller is IEquipmentUserController equipmentUser))
                return;
            if(!callbacks.TryGetValue(owner, out Callback callback))
                return;
            
            owner.States.RemoveState(new EntityState(Perk21State.Instance));
            owner.Callbacks.Unregister((IEntityDealtDamageCallback)callback);
            equipmentUser.Equipment.Callbacks.Unregister((IEquipmentSwapCallback)callback);
            equipmentUser.Equipment.Projectiles.Callbacks.Unregister((IOnProjectileDespawned)callback);
            StaticObjectPool<Callback>.Return(callback.Clear());
        }

        private class Callback : IEntityDealtDamageCallback, IOnProjectileDespawned, IEquipmentSwapCallback
        {
            private Perk21 perk;
            private Entity entity;
            
            public Callback() { }

            public Callback Init(Perk21 perk, Entity entity)
            {
                this.perk   = perk;
                this.entity = entity;
                return this;
            }
            
            uint ILockableOrderedListElement.Order => 999;

            void IEntityDealtDamageCallback.OnDealtDamage(Entity source, Entity target, bool fromRpc, in DamageValue damageVal)
            {
                if(!(source.Controller is IEquipmentUserController equipmentUser) || equipmentUser.Equipment.CurrentSlot.IsEmpty)
                    return;
                if(!perk.appliesTo.Contains(equipmentUser.Equipment.CurrentSlot.EquippableKey))
                    return;
                
                if(target.CurStats.health == 0) {
                    // This was a fatal shot.
                    source.States.AddState(new EntityState(Perk21State.Instance, 1), applyStats: false);
                } else {
                    // Not fatal - lose all stacks.
                    source.States.RemoveState(new EntityState(Perk21State.Instance), applyStats: false);
                }
            }

            void IOnProjectileDespawned.OnProjectileDespawned(IProjectile projectile, bool missed)
            {
                if(missed) {
                    entity.States.RemoveState(new EntityState(Perk21State.Instance), applyStats: false);
                }
            }

            void IEquipmentSwapCallback.OnEquipmentSwap(EntityEquipment equipment, Equippable from, Equippable to)
            {
                entity.States.RemoveState(new EntityState(Perk21State.Instance), applyStats: false);
            }

            public Callback Clear()
            {
                entity = null;
                return this;
            }
        }
    }
    
    public class Perk21State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk21State.Key;
        public override ushort? DefaultID    => StateLookup.Perk21State.DefaultID;
        public override ushort MaxValue      => 3;
        public override float? MaxDuration   => 30.0f;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        private Callback callback;
        
        public static Perk21State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
            callback = new Callback();
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            if(val > 0) {
                entity.Callbacks.Register((IEntityPreDealDamageCallback)callback);
            }
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            if(originalVal - val == 0) {
                entity.Callbacks.Unregister((IEntityPreDealDamageCallback)callback);
            }
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val) { }

        private class Callback : IEntityPreDealDamageCallback
        {
            uint ILockableOrderedListElement.Order => 100;

            void IEntityPreDealDamageCallback.OnPreDealDamage(Entity source, Entity target, bool isTest, ref DamageValue damageVal)
            {
                if(!source.States.TryGetValue(Instance, out ushort stacks) || stacks == 0)
                    return;

                damageVal.Amount = (ushort)Mathf.Clamp(damageVal.Amount * (1.0f + (stacks * 0.2f)), 0.0f, ushort.MaxValue);
            }
        }
    }
}
