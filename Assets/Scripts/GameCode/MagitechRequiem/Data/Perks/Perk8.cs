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
    /// Agile
    /// Increase iframes by 3, and roll distance by 15%.
    /// </summary>
    public class Perk8 : Perk
    {
        public override string LookupKey => PerkLookup.Perk8.Key;
        public override ushort? DefaultID => PerkLookup.Perk8.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity)
            => new() { { "iframes", "3" }, { "distance", "15%" } };
        
        public static Perk8 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.AddState(new EntityState(Perk8State.Instance));
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.RemoveState(new EntityState(Perk8State.Instance));
        }
    }

    public class Perk8State : PerkState
    {
        public override string LookupKey          => StateLookup.Perk8State.Key;
        public override ushort? DefaultID         => StateLookup.Perk8State.DefaultID;
        public override ushort MaxValue           => 1;
        public override bool RemoveAllValOnExpire => true;
        public override bool NetSync              => true;
        public override bool PersistenceSync      => true;

        public static Perk8State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
        
        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            float curDodgeSpeed   = entity.CurStats.GetParam(DodgeSpeedMult.Instance);
            int curDodgeIFrameMod = (int)entity.CurStats.GetParam(DodgeIFrames.Instance);
            entity.CurStats.SetParam(DodgeSpeedMult.Instance, curDodgeSpeed * 1.15f);
            entity.CurStats.SetParam(DodgeIFrames.Instance, curDodgeIFrameMod + 3);
        }
    }
}
