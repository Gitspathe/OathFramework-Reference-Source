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
    /// Sharpshooter
    /// Accuracy and range of all firearms is increased by 25%
    /// </summary>
    public class Perk30 : Perk
    {
        public override string LookupKey => PerkLookup.Perk30.Key;
        public override ushort? DefaultID => PerkLookup.Perk30.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "accuracy_amt", "25" }, { "range_amt", "25" } };
        
        public static Perk30 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;

            owner.States.AddState(new EntityState(Perk30State.Instance));
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.RemoveState(new EntityState(Perk30State.Instance));
        }
    }
    
    public class Perk30State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk30State.Key;
        public override ushort? DefaultID    => StateLookup.Perk30State.DefaultID;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
        
        public static Perk30State Instance { get; private set; }

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
            float curAcc   = entity.CurStats.GetParam(AccuracyMult.Instance);
            float curRange = entity.CurStats.GetParam(MaxRangeMult.Instance);
            entity.CurStats.SetParam(AccuracyMult.Instance, curAcc * 1.25f);
            entity.CurStats.SetParam(MaxRangeMult.Instance, curRange * 1.25f);
        }
    }
}
