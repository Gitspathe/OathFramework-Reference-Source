using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Data.StatParams;
using OathFramework.Effects;
using OathFramework.EntitySystem;
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
    /// Heal and Defy
    /// Quick heal buffs allies within 16 meters with +25% attack damage for 5 seconds.
    /// However, the HP restored is reduced by 70%.
    /// </summary>
    public class Perk16 : Perk
    {
        public override string LookupKey => PerkLookup.Perk16.Key;
        public override ushort? DefaultID => PerkLookup.Perk16.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "range", "16" }, { "amt", "25%" }, { "duration", "5" }, { "heal_reduction", "70%" } };

        private Callback callback = new();
        
        public static Perk16 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            owner.States.AddState(new EntityState(Perk16PassiveState.Instance), applyStats: false);
            if(auxOnly || owner.IsDummy || !owner.TryGetComponent(out QuickHealHandler handler))
                return;
            
            handler.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            owner.States.RemoveState(new EntityState(Perk16PassiveState.Instance), applyStats: false);
            if(auxOnly || owner.IsDummy || !owner.TryGetComponent(out QuickHealHandler handler))
                return;
            
            handler.Callbacks.Unregister(callback);
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
                    
                    dist.Entity.States.AddState(new EntityState(Perk16State.Instance), applyStats: false);
                }
                entityCache.Clear();
            }
        }
    }

    public class Perk16PassiveState : PerkState
    {
        public override string LookupKey     => StateLookup.Perk16PassiveState.Key;
        public override ushort? DefaultID    => StateLookup.Perk16PassiveState.DefaultID;
        public override ushort MaxValue      => 1;
        public override float? MaxDuration   => null;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
    
        public static Perk16PassiveState Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal) { }
        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal) { }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            entity.CurStats.SetParam(QuickHealAmount.Instance, entity.CurStats.GetParam(QuickHealAmount.Instance) * 0.3f);
        }
    }
    
    public class Perk16State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk16State.Key;
        public override ushort? DefaultID    => StateLookup.Perk16State.DefaultID;
        public override ushort MaxValue      => 1;
        public override float? MaxDuration   => 5.0f;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;
        
        private Callback callback = new();
        private Dictionary<Entity, Effect> effectDict = new();

        public static Perk16State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            entity.Callbacks.Register(callback);
            if(effectDict.TryGetValue(entity, out Effect effect)) {
                effect.Return();
                effectDict.Remove(entity);
            }
            Effect e = EffectManager.Retrieve("core:small_aura", sockets: entity.Sockets);
            e.GetComponent<IColorable>().SetColor(Color.red);
            effectDict.Add(entity, e);
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            entity.Callbacks.Unregister(callback);
            if(!effectDict.TryGetValue(entity, out Effect effect))
                return;

            effect.Return();
            effectDict.Remove(entity);
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val) { }

        private class Callback : IEntityPreDealDamageCallback
        {
            uint ILockableOrderedListElement.Order => 10;

            void IEntityPreDealDamageCallback.OnPreDealDamage(Entity source, Entity target, bool isTest, ref DamageValue damageVal)
            {
                damageVal.Amount = (ushort)(damageVal.Amount * 1.25f);
            }
        }
    }
}
