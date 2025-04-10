using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Gambit
    /// Taking damage lowers the cooldown of your abilities, at a conversion rate of 60 damage per second.
    /// </summary>
    public class Perk24 : Perk
    {
        public override string LookupKey => PerkLookup.Perk24.Key;
        public override ushort? DefaultID => PerkLookup.Perk24.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "conversion", "60" } };

        private Callback callback = new();
        public static Perk24 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || owner.Abilities == null)
                return;

            owner.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || owner.Abilities == null)
                return;
            
            owner.Callbacks.Unregister(callback);
        }

        private class Callback : IEntityTakeDamageCallback
        {
            uint ILockableOrderedListElement.Order => 9999;

            void IEntityTakeDamageCallback.OnDamage(Entity entity, bool fromRpc, in DamageValue val)
            {
                float amt = (float)val.Amount / 60.0f;
                entity.Abilities.AddChargeProgress(amt);
            }
        }
    }
}
