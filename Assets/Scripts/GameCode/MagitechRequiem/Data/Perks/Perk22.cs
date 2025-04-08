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
    /// Heart of Stone
    /// Quickheal no longer restores health. However, Quickheal use speed is increased by 100%, and gain 2 extra charges.
    /// </summary>
    public class Perk22 : Perk
    {
        public override string LookupKey => PerkLookup.Perk22.Key;
        public override ushort? DefaultID => PerkLookup.Perk22.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "speed_amt", "50" }, { "charges_amt", "2" } };
        
        public static Perk22 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;

            owner.States.AddState(new EntityState(Perk22State.Instance));
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.RemoveState(new EntityState(Perk22State.Instance));
        }
    }
    
    public class Perk22State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk22State.Key;
        public override ushort? DefaultID    => StateLookup.Perk22State.DefaultID;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
        
        public static Perk22State Instance { get; private set; }

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
            float curSpeed   = entity.CurStats.GetParam(QuickHealSpeedMult.Instance);
            float curCharges = entity.CurStats.GetParam(QuickHealCharges.Instance);
            entity.CurStats.SetParam(QuickHealSpeedMult.Instance, curSpeed * 1.5f);
            entity.CurStats.SetParam(QuickHealAmount.Instance, 0.0f);
            entity.CurStats.SetParam(QuickHealCharges.Instance, curCharges + 2);
        }
    }
}
