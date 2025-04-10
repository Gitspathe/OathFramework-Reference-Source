using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Data.EntityStates;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.EntitySystem.States;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Unnamed
    /// Using a Quickheal charge buffs all allies within 16 meters with invincibility for 1.5 seconds.
    /// After the buff expires, allies' damage resistance is increased by 25% for a further 5 seconds.
    /// </summary>
    public class Perk23 : Perk
    {
        public override string LookupKey => PerkLookup.Perk23.Key;
        public override ushort? DefaultID => PerkLookup.Perk23.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "range", "16" }, { "invincible_duration", "1.5" }, { "defense_amt", "25" }, { "defence_duration", "5" } };

        private Callback callback = new();
        public static Perk23 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is PlayerController playerController))
                return;

            playerController.QuickHeal.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is PlayerController playerController))
                return;
            
            owner.States.RemoveState(new EntityState(Invulnerable.Instance));
            owner.States.RemoveState(new EntityState(Perk23State.Instance));
            playerController.QuickHeal.Callbacks.Unregister(callback);
        }

        private class Callback : IOnUseQuickHealCallback
        {
            private QList<EntityDistance> entityCache = new();
            
            void IOnUseQuickHealCallback.OnUseQuickHeal(QuickHealHandler handler, bool auxOnly)
            {
                if(auxOnly)
                    return;
                
                handler.Entity.Targeting.GetDistances(entityCache, handler.Entity.Team);
                for(int i = 0; i < entityCache.Count; i++) {
                    EntityDistance dist = entityCache.Array[i];
                    if(dist.Distance > 16.0f)
                        continue;
                    
                    dist.Entity.States.AddState(new EntityState(Invulnerable.Instance, duration: 1.5f));
                    dist.Entity.States.AddState(new EntityState(Perk23State.Instance));
                }
                entityCache.Clear();
            }
        }
    }
    
    public class Perk23State : PerkState, IEntityPreTakeDamageCallback
    {
        public override string LookupKey     => StateLookup.Perk23State.Key;
        public override ushort? DefaultID    => StateLookup.Perk23State.DefaultID;
        public override float? MaxDuration   => 6.5f; // 1.5 + 5.0.
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
        
        private Dictionary<Entity, Effect> effectDict = new();
        
        public static Perk23State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            if(effectDict.TryGetValue(entity, out Effect effect)) {
                effect.Return();
                effectDict.Remove(entity);
            }
            Effect e = EffectManager.Retrieve("core:small_aura", sockets: entity.Sockets);
            e.GetComponent<IColorable>().SetColor(Color.white);
            effectDict.Add(entity, e);
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            if(!effectDict.TryGetValue(entity, out Effect effect))
                return;

            effect.Return();
            effectDict.Remove(entity);
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            
        }

        public void OnPreDamage(Entity entity, bool fromRpc, bool isTest, ref DamageValue val)
        {
            val.Amount = (ushort)Mathf.Clamp(val.Amount * 0.75f, 1.0f, ushort.MaxValue);
        }
    }
}
