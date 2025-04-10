using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Stoicism
    /// Reduce damage by 3.6% for every 10% of missing hp.
    /// </summary>
    public class Perk2 : Perk
    {
        public override string LookupKey => PerkLookup.Perk2.Key;
        public override ushort? DefaultID => PerkLookup.Perk2.DefaultID;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new(){ {"amt", "3.6%"}, {"missing_hp", "10%"} };

        private static DamageCallback Callback = new();
        public static Perk2 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            owner.Callbacks.Register(Callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            owner.Callbacks.Unregister(Callback);
        }

        private class DamageCallback : IEntityPreTakeDamageCallback
        {
            uint ILockableOrderedListElement.Order => 100;
            
            void IEntityPreTakeDamageCallback.OnPreDamage(Entity entity, bool fromRpc, bool isTest, ref DamageValue val)
            {
                int stack;
                if(entity.CurStats.health == 0 || entity.CurStats.maxHealth == 0) {
                    stack = 10;
                } else {
                    float missingPercent = 1.0f - ((float)entity.CurStats.health / (float)entity.CurStats.maxHealth);
                    stack = (int)(missingPercent * 10.0f);
                }
                val.Amount -= (ushort)(val.Amount * 0.036f * stack);
            }
        }
    }
}
