using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Data.EntityStates;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.EntitySystem.States;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using System.Collections.Generic;
using UnityEngine;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Unnamed
    /// Using a Quickheal charge buffs all allies within 15 meters with invincibility for 3 seconds.
    /// After the buff expires, allies' damage resistance is increased by 25% for a further 5 seconds.
    /// </summary>
    public class Perk23 : Perk
    {
        public override string LookupKey => PerkLookup.Perk23.Key;
        public override ushort? DefaultID => PerkLookup.Perk23.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "range", "15" }, { "invincible_duration", "3" }, { "defense_amt", "25" }, { "defence_duration", "5" } };

        private Callback callback = new();
        public static Perk23 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is PlayerController playerController))
                return;

            playerController.QuickHeal.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is PlayerController playerController))
                return;
            
            owner.States.RemoveState(new EntityState(Invulnerable.Instance));
            owner.States.RemoveState(new EntityState(Perk23State.Instance));
            playerController.QuickHeal.Callbacks.Unregister(callback);
        }

        private class Callback : IOnUseQuickHealCallback
        {
            public void OnUseQuickHeal(QuickHealHandler handler, bool auxOnly)
            {
                if(auxOnly)
                    return;
                
                handler.Entity.States.AddState(new EntityState(Invulnerable.Instance));
                handler.Entity.States.AddState(new EntityState(Perk23State.Instance));
            }
        }
    }
    
    public class Perk23State : PerkState, IEntityPreTakeDamageCallback
    {
        public override string LookupKey     => StateLookup.Perk23State.Key;
        public override ushort? DefaultID    => StateLookup.Perk23State.DefaultID;
        public override float? MaxDuration   => 8.0f; // 3.0 + 5.0.
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
        
        public static Perk23State Instance { get; private set; }

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
            
        }

        public void OnPreDamage(Entity entity, bool fromRpc, bool isTest, ref DamageValue val)
        {
            val.Amount = (ushort)Mathf.Clamp(val.Amount * 0.75f, 1.0f, ushort.MaxValue);
        }
    }
}
