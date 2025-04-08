using OathFramework.Achievements;
using OathFramework.Core;
using OathFramework.Data.EntityStates;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Human Resolve
    /// When an attack would kill you, it instead sets your hp to 1 and makes you immune for 3 seconds. cooldown: 60 sec / the round.
    /// </summary>
    public class Perk7 : Perk, IUpdateable
    {
        public override string LookupKey => PerkLookup.Perk7.Key;
        public override ushort? DefaultID => PerkLookup.Perk7.DefaultID;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "duration", "3" } };

        private OnPreDieCallback callback               = new();
        private Dictionary<Entity, float> coolDowns     = new();
        private Dictionary<Entity, float> coolDownsCopy = new();

        public static Perk7 Instance { get; private set; }

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
            owner.States.RemoveState(new EntityState(Invulnerable.Instance));
        }
        
        void IUpdateable.Update()
        {
            foreach(KeyValuePair<Entity, float> pair in coolDowns) {
                float t = pair.Value - Time.deltaTime;
                if(t >= 0.0f) {
                    coolDownsCopy[pair.Key] = t;
                }
            }
            coolDowns.Clear();
            foreach(KeyValuePair<Entity,float> pair in coolDownsCopy) {
                coolDowns[pair.Key] = pair.Value;
            }
            coolDownsCopy.Clear();
        }
        
        private bool IsInCooldown(Entity entity) => coolDowns.ContainsKey(entity) && coolDowns[entity] > 0.0f;

        private class OnPreDieCallback : IEntityPreDieCallback
        {
            uint ILockableOrderedListElement.Order => 100;
            
            bool IEntityPreDieCallback.OnPreDie(Entity entity, in DamageValue lastDamageVal)
            {
                if(Instance.IsInCooldown(entity))
                    return false;

                entity.States.AddState(new EntityState(Invulnerable.Instance));
                Instance.coolDowns[entity] = 60.0f;
                AchievementManager.UnlockAchievement("perk7_use");
                return true;
            }
        }
    }
}
