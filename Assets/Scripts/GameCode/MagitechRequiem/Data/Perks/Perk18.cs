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
    /// Unnamed
    /// Increases range of explosives by 25%.
    /// </summary>
    public class Perk18 : Perk
    {
        public override string LookupKey => PerkLookup.Perk18.Key;
        public override ushort? DefaultID => PerkLookup.Perk18.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "25" } };
        
        public static Perk18 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.AddState(new EntityState(Perk18State.Instance));
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;
            
            owner.States.RemoveState(new EntityState(Perk18State.Instance));
        }
    }
    
    public class Perk18State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk18State.Key;
        public override ushort? DefaultID    => StateLookup.Perk18State.DefaultID;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
        
        public static Perk18State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal) { }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal) { }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            entity.CurStats.SetParam(ExplosiveRangeMult.Instance, entity.CurStats.GetParam(ExplosiveRangeMult.Instance) * 1.25f);
        }
    }
}
