using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.PerkSystem;
using System.Collections.Generic;
using UnityEngine;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Juggernaut
    /// Boost hp by 20%.
    /// </summary>
    public class Perk5 : Perk
    {
        public override string LookupKey => PerkLookup.Perk5.Key;
        public override ushort? DefaultID => PerkLookup.Perk5.DefaultID;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new(){ {"amt", "20%"} };

        public static Perk5 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.SetState(new EntityState(Perk5State.Instance), true);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.RemoveState(new EntityState(Perk5State.Instance), resetCurrent: true);
        }
    }

    public class Perk5State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk5State.Key;
        public override ushort? DefaultID    => StateLookup.Perk5State.DefaultID;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        public static Perk5State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            entity.CurStats.maxHealth = (uint)Mathf.Clamp(entity.CurStats.maxHealth * 1.20f, 0.0f, uint.MaxValue);
        }
    }
}
