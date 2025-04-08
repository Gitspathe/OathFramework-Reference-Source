using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Data.StatParams;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.PerkSystem;
using System.Collections.Generic;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Diviner's Instinct
    /// Gain an extra quick heal charge, and boost restored hp from quick heal by 10%.
    /// </summary>
    public class Perk13 : Perk
    {
        public override string LookupKey => PerkLookup.Perk13.Key;
        public override ushort? DefaultID => PerkLookup.Perk13.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "extra_charge", "1" }, { "amt", "10%" } };
        
        public static Perk13 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.SetState(new EntityState(Perk13State.Instance), true);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.SetState(new EntityState(Perk13State.Instance), false);
        }
    }
    
    public class Perk13State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk13State.Key;
        public override ushort? DefaultID    => StateLookup.Perk13State.DefaultID;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        public static Perk13State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            entity.CurStats.SetParam(QuickHealCharges.Instance, entity.CurStats.GetParam(QuickHealCharges.Instance) + 1);
            entity.CurStats.SetParam(QuickHealAmount.Instance, entity.CurStats.GetParam(QuickHealAmount.Instance) * 1.1f);
        }
    }
}
