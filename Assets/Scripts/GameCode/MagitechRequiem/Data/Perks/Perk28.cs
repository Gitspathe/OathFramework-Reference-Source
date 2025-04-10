using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Data.StatParams;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Abyssal Charge
    /// Killing an enemy reduces Magitech cooldowns by 1 second, and increases Magitech use speed by 35% for 5 seconds.
    /// </summary>
    public class Perk28 : Perk
    {
        public override string LookupKey => PerkLookup.Perk28.Key;
        public override ushort? DefaultID => PerkLookup.Perk28.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "cooldown_reduction", "1" }, { "add_use_speed", "35" }, { "use_speed_duration", "5" } };

        private Callback callback = new();
        
        public static Perk28 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;

            owner.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly)
                return;

            owner.Callbacks.Unregister(callback);
        }

        private class Callback : IEntityScoreKillCallback
        {
            uint ILockableOrderedListElement.Order => 100;

            void IEntityScoreKillCallback.OnScoredKill(Entity entity, IEntity other, in DamageValue lastDamageVal, float ratio)
            {
                if(!lastDamageVal.GetInstigator(out Entity instigator) || instigator != entity)
                    return;
                
                entity.Abilities.AddChargeProgress(1.0f);
                entity.States.AddState(new EntityState(Perk28State.Instance));
            }
        }
    }
    
    public class Perk28State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk28State.Key;
        public override ushort? DefaultID    => StateLookup.Perk28State.DefaultID;
        public override float? MaxDuration   => 5.0f;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
        
        public static Perk28State Instance { get; private set; }

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
            float curAbilitySpeed = entity.CurStats.GetParam(AbilityUseSpeedMult.Instance);
            entity.CurStats.SetParam(AbilityUseSpeedMult.Instance, curAbilitySpeed * 1.35f);
        }
    }
}
