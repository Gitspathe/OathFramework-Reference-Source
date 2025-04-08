using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Toughness
    /// Reduce all damage by a flat 5 points (after all other modifiers). Can reduce to one.
    /// </summary>
    public class Perk10 : Perk
    {
        public override string LookupKey => PerkLookup.Perk10.Key;
        public override ushort? DefaultID => PerkLookup.Perk10.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "5" } };
        
        private Callback callback = new();
        
        public static Perk10 Instance { get; private set; }

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

        private class Callback : IEntityPreTakeDamageCallback
        {
            uint ILockableOrderedListElement.Order => 10_000;
            
            void IEntityPreTakeDamageCallback.OnPreDamage(Entity entity, bool fromRpc, bool isTest, ref DamageValue val)
            {
                if(!entity.IsOwner)
                    return;
                
                val.Amount = (ushort)Mathf.Clamp(val.Amount - 5, 0, ushort.MaxValue);
            }
        }
    }
}
