using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Stonewall
    /// Stagger damage is reduced by 33%. If your health is below 40%, this reduction is increased to 50%.
    /// </summary>
    public class Perk12 : Perk
    {
        public override string LookupKey => PerkLookup.Perk12.Key;
        public override ushort? DefaultID => PerkLookup.Perk12.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "33%" }, { "cond", "40%" }, { "amt2", "50%" } };
        
        private Callback callback = new();
        
        public static Perk12 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            owner.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            owner.Callbacks.Unregister(callback);
        }

        private class Callback : IEntityPreTakeDamageCallback
        {
            uint ILockableOrderedListElement.Order => 10;

            void IEntityPreTakeDamageCallback.OnPreDamage(Entity entity, bool fromRpc, bool isTest, ref DamageValue val)
            {
                float ratio = (float)entity.CurStats.health / (float)entity.CurStats.maxHealth;
                if(ratio > 0.4f) {
                    val.StaggerAmount -= (ushort)(val.StaggerAmount * 0.33f);
                } else {
                    val.StaggerAmount += (ushort)(val.StaggerAmount * 0.5f);
                }
            }
        }
    }
}
