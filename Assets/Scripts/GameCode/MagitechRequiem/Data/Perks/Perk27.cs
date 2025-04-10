using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Bloodlust
    /// Killing an enemy restores 15 HP, and causes stamina to regenerate immediately.
    /// </summary>
    public class Perk27 : Perk
    {
        public override string LookupKey => PerkLookup.Perk27.Key;
        public override ushort? DefaultID => PerkLookup.Perk27.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "heal_amt", "15" } };

        private Callback callback = new();
        
        public static Perk27 Instance { get; private set; }

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
                
                entity.Heal(new HealValue(15, instigator: entity));
                entity.IncrementStamina(1, true);
            }
        }
    }
}
