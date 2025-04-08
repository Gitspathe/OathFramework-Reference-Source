using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Vengeance
    /// Boost damage dealt by 3% for every 10% of missing hp.
    /// </summary>
    public class Perk3 : Perk
    {
        public override string LookupKey => PerkLookup.Perk3.Key;
        public override ushort? DefaultID => PerkLookup.Perk3.DefaultID;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new(){ {"amt", "3%"}, {"missing_hp", "10%"} };

        private static DamageCallback Callback = new();
        public static Perk3 Instance { get; private set; }

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

        private class DamageCallback : IEntityPreDealDamageCallback
        {
            uint ILockableOrderedListElement.Order => 100;
            
            void IEntityPreDealDamageCallback.OnPreDealDamage(Entity entity, Entity target, bool isTest, ref DamageValue val)
            {
                if(!entity.IsOwner)
                    return;
                
                int stack;
                if(entity.CurStats.health == 0 || entity.CurStats.maxHealth == 0) {
                    stack = 10;
                } else {
                    float missingPercent = 1.0f - ((float)entity.CurStats.health / (float)entity.CurStats.maxHealth);
                    stack = (int)(missingPercent * 10.0f);
                }
                val.Amount += (ushort)(val.Amount * 0.03f * stack);
            }
        }
    }
}
