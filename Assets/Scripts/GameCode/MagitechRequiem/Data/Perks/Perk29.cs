using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Data.StatParams;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.EntitySystem.States;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Sleight of Hand
    /// Perfect dodging an attack increases damage dealt by 10%, and accuracy by 20%, for 5 seconds. Maximum 2 stacks.
    /// </summary>
    public class Perk29 : Perk
    {
        public override string LookupKey => PerkLookup.Perk29.Key;
        public override ushort? DefaultID => PerkLookup.Perk29.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "damage_amt", "10" }, { "accuracy_amt", "20" }, { "duration", "5" }, { "max_stacks", "2" } };

        private DodgeCallback dodgeCallback   = new();
        private DamageCallback damageCallback = new();
        
        public static Perk29 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is PlayerController player))
                return;

            owner.Callbacks.Register(damageCallback);
            player.DodgeHandler.Callbacks.Register(dodgeCallback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is PlayerController player))
                return;

            owner.Callbacks.Unregister(damageCallback);
            player.DodgeHandler.Callbacks.Unregister(dodgeCallback);
        }

        private class DodgeCallback : IEntityDodgedAttackCallback
        {
            uint ILockableOrderedListElement.Order => 100;

            void IEntityDodgedAttackCallback.OnDodgedAttack(Entity entity, in DamageValue damageVal, int dodgeCount)
            {
                if(dodgeCount > 1)
                    return;
                
                entity.States.AddState(new EntityState(Perk29State.Instance, 1));
            }
        }

        private class DamageCallback : IEntityPreDealDamageCallback
        {
            uint ILockableOrderedListElement.Order => 100;

            void IEntityPreDealDamageCallback.OnPreDealDamage(Entity source, Entity target, bool isTest, ref DamageValue damageVal)
            {
                if(!source.States.TryGetValue(Perk29State.Instance, out ushort stacks))
                    return;

                damageVal.Amount = (ushort)Mathf.Clamp(damageVal.Amount * (1.0f + (stacks * 0.1f)), 0.0f, ushort.MaxValue);
            }
        }
    }
    
    public class Perk29State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk29State.Key;
        public override ushort? DefaultID    => StateLookup.Perk29State.DefaultID;
        public override float? MaxDuration   => 5.0f;
        public override ushort MaxValue      => 2;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
        
        public static Perk29State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            float cur = entity.CurStats.GetParam(AccuracyMult.Instance);
            entity.CurStats.SetParam(AccuracyMult.Instance, cur * (1.0f + (1.2f * val)));
        }
    }
}
