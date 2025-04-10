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
    /// Dexterous
    /// Boost reload speed by 25% and swap speed by 36%
    /// </summary>
    public class Perk6 : Perk
    {
        public override string LookupKey => PerkLookup.Perk6.Key;
        public override ushort? DefaultID => PerkLookup.Perk6.DefaultID;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new(){ {"reload_amt", "25%"}, {"swap_amt", "36%"} };

        public static Perk6 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.SetState(new EntityState(Perk6State.Instance), true);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.RemoveState(new EntityState(Perk6State.Instance), resetCurrent: true);
        }
    }

    public class Perk6State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk6State.Key;
        public override ushort? DefaultID    => StateLookup.Perk6State.DefaultID;
        public override ushort MaxValue      => 1;
        public override uint Order           => 1000;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        public static Perk6State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            float curReloadSpeed = entity.CurStats.GetParam(ReloadSpeedMult.Instance);
            float curSwapSpeed = entity.CurStats.GetParam(SwapSpeedMult.Instance);
            entity.CurStats.SetParam(ReloadSpeedMult.Instance, curReloadSpeed * 1.25f);
            entity.CurStats.SetParam(SwapSpeedMult.Instance, curSwapSpeed * 1.36f);
        }
    }
}
