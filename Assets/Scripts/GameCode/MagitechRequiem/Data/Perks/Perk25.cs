using OathFramework.Data.StatParams;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// unnamed
    /// For each missing Quickheal charge (based on your maximum), all damage dealt is increased by a stacking 3%.
    /// </summary>
    public class Perk25 : Perk
    {
        public override string LookupKey => PerkLookup.Perk25.Key;
        public override ushort? DefaultID => PerkLookup.Perk25.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "damage_amt", "3" } };

        private Callback callback = new();
        public static Perk25 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is PlayerController))
                return;

            owner.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is PlayerController))
                return;
            
            owner.Callbacks.Unregister(callback);
        }

        private class Callback : IEntityPreDealDamageCallback
        {
            uint ILockableOrderedListElement.Order => 9999;

            void IEntityPreDealDamageCallback.OnPreDealDamage(Entity source, Entity target, bool isTest, ref DamageValue damageVal)
            {
                byte curCharges = ((PlayerController)source.Controller).QuickHeal.CurrentCharges;
                byte maxCharges = (byte)source.CurStats.GetParam(QuickHealCharges.Instance);
                byte missing    = (byte)Mathf.Clamp(maxCharges - curCharges, 0, byte.MaxValue);
                if(missing == 0 || maxCharges == 0)
                    return;

                float amt        = missing * 0.03f;
                damageVal.Amount = (ushort)Mathf.Clamp(damageVal.Amount * (1.0f + amt), 0.0f, ushort.MaxValue);
            }
        }
    }
}
