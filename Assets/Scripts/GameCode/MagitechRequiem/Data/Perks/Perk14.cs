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
    /// Master Escapist
    /// Stamina and stamina regen is increased by 30%, and roll iframes by 6, while max hp is reduced by 25%.
    /// </summary>
    public class Perk14 : Perk
    {
        public override string LookupKey => PerkLookup.Perk14.Key;
        public override ushort? DefaultID => PerkLookup.Perk14.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "stamina_amt", "30%" }, { "iframes", "6" }, { "hp_malus", "25%" } };
        
        public static Perk14 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            owner.States.SetState(new EntityState(Perk14State.Instance), true);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            owner.States.SetState(new EntityState(Perk14State.Instance), false);
        }
    }
    
    public class Perk14State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk14State.Key;
        public override ushort? DefaultID    => StateLookup.Perk14State.DefaultID;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        public static Perk14State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            entity.CurStats.maxHealth  = (ushort)(entity.CurStats.maxHealth * 0.75f);
            entity.CurStats.maxStamina = (ushort)(entity.CurStats.maxStamina * 1.3f);
            entity.CurStats.SetParam(StaminaRegen.Instance, entity.CurStats.GetParam(StaminaRegen.Instance) * 1.3f);
            entity.CurStats.SetParam(DodgeIFrames.Instance, entity.CurStats.GetParam(DodgeIFrames.Instance) + 6);
        }
    }
}
