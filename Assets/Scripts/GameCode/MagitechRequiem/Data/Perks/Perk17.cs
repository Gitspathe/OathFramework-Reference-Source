using OathFramework.Data.EntityStates;
using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Unnamed
    /// Increases damage against incapacitated enemies by 15%.
    /// </summary>
    public class Perk17 : Perk
    {
        public override string LookupKey => PerkLookup.Perk17.Key;
        public override ushort? DefaultID => PerkLookup.Perk17.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "15" } };

        private Callback callback = new();
        
        public static Perk17 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || owner.IsDummy)
                return;
            
            owner.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || owner.IsDummy)
                return;
            
            owner.Callbacks.Unregister(callback);
        }
        
        private class Callback : IEntityPreDealDamageCallback
        {
            uint ILockableOrderedListElement.Order => 20;

            void IEntityPreDealDamageCallback.OnPreDealDamage(Entity source, Entity target, bool isTest, ref DamageValue damageVal)
            {
                if(target.States.HasState(Stunned.Instance)) {
                    damageVal.Amount = (ushort)(damageVal.Amount * 1.15f);
                }
            }
        }
    }
}
