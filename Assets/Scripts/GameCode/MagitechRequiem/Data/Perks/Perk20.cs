using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Unnamed
    /// Killing an enemy grants a 3% damage bonus. Stacks 5 times and has a 1.3 second duration.
    /// </summary>
    public class Perk20 : Perk
    {
        public override string LookupKey => PerkLookup.Perk20.Key;
        public override ushort? DefaultID => PerkLookup.Perk20.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "3" }, { "max_stacks", "5" }, { "duration", "1.3" } };

        private Callback callback;
        
        public static Perk20 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
            callback = new Callback(this);
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || ReferenceEquals(owner.Abilities, null))
                return;
            
            owner.Callbacks.Register((IEntityScoreKillCallback)callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || ReferenceEquals(owner.Abilities, null))
                return;
            
            owner.Callbacks.Unregister((IEntityScoreKillCallback)callback);
            owner.States.RemoveState(new EntityState(Perk20State.Instance));
        }

        private class Callback : IEntityScoreKillCallback
        {
            private Perk20 perk;

            public Callback(Perk20 perk)
            {
                this.perk = perk;
            }
            
            uint ILockableOrderedListElement.Order => 999;

            void IEntityScoreKillCallback.OnScoredKill(Entity entity, IEntity other, in DamageValue lastDamageVal, float ratio)
            {
                if(!(other is Entity e) || !EntityTypes.AreEnemies(entity.Team, e.Team))
                    return;
                
                entity.States.AddState(new EntityState(Perk20State.Instance, 1), applyStats: false);
            }
        }
    }
    
    public class Perk20State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk20State.Key;
        public override ushort? DefaultID    => StateLookup.Perk20State.DefaultID;
        public override ushort MaxValue      => 5;
        public override float? MaxDuration   => 1.3f;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        private Callback callback;
        
        public static Perk20State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
            callback = new Callback();
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            if(val > 0) {
                entity.Callbacks.Register((IEntityPreDealDamageCallback)callback);
            }
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            if(originalVal - val == 0) {
                entity.Callbacks.Unregister((IEntityPreDealDamageCallback)callback);
            }
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val) { }

        private class Callback : IEntityPreDealDamageCallback
        {
            uint ILockableOrderedListElement.Order => 100;

            void IEntityPreDealDamageCallback.OnPreDealDamage(Entity source, Entity target, bool isTest, ref DamageValue damageVal)
            {
                if(!source.States.TryGetValue(Instance, out ushort stacks) || stacks == 0)
                    return;

                damageVal.Amount = (ushort)Mathf.Clamp(damageVal.Amount * (1.0f + (stacks * 0.03f)), 0.0f, ushort.MaxValue);
            }
        }
    }
}
