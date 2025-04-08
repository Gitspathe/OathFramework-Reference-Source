using OathFramework.AbilitySystem;
using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Unnamed
    /// Using an ability restores 10% of your hp and stamina, and causes stamina to regenerate immediately.
    /// </summary>
    public class Perk19 : Perk
    {
        public override string LookupKey => PerkLookup.Perk19.Key;
        public override ushort? DefaultID => PerkLookup.Perk19.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "10" } };

        private Callback callback = new();
        
        public static Perk19 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || ReferenceEquals(owner.Abilities, null))
                return;
            
            owner.Abilities.Callbacks.Register((IOnAbilityChargeDecrement)callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || ReferenceEquals(owner.Abilities, null))
                return;
            
            owner.Abilities.Callbacks.Unregister((IOnAbilityChargeDecrement)callback);
        }
        
        private class Callback : IOnAbilityChargeDecrement
        {
            uint ILockableOrderedListElement.Order => 100;

            void IOnAbilityChargeDecrement.OnAbilityChargeDecrement(AbilityHandler handler, Ability ability, bool auxOnly)
            {
                float healAmt  = handler.Entity.CurStats.maxHealth * 0.1f;
                float stamAmt  = handler.Entity.CurStats.maxStamina * 0.1f;
                HealValue hVal = new(
                    (ushort)healAmt, 
                    source: HealSource.Player,
                    flags: HealFlags.BypassInstigatorCallbacks, 
                    instigator: handler.Entity
                );
                handler.Entity.Heal(hVal);
                handler.Entity.IncrementStamina((ushort)stamAmt, true);
            }
        }
    }
}
