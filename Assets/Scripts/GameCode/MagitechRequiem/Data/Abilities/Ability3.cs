using OathFramework.AbilitySystem;
using OathFramework.Data;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;
using Lookup = GameCode.MagitechRequiem.Data.States.StateLookup.Status;

namespace GameCode.MagitechRequiem.Data.Abilities
{
    public class Ability3 : Ability, IActionAbility
    {
        public override string LookupKey         => "core:shield";
        public override ushort? DefaultID        => 3;
        public override bool AutoNetSync         => true;
        public override bool AutoPersistenceSync => true;
        public bool SyncActivation               => false;
        public string ActionAnimParams           => ActionAnimParamsLookup.Ability.Shield;
        public override bool HasCharges          => true;
        public override bool IsInstant           => true;
        
        public override float GetMaxCooldown(Entity entity)       => 0.5f;
        public override byte GetMaxCharges(Entity entity)         => 1;
        public override float GetMaxChargeProgress(Entity entity) => 60.0f;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) => new() {
            { "duration", "20" }
        };
        
        public static Ability3 Instance { get; private set; }
        
        private Dictionary<Entity, Prop> curProps = new();
        
        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnActivate(Entity invoker, bool auxOnly, bool lateJoin)
        {
            ApplyClipData(invoker);
            ReturnProp(invoker);
            if(!invoker.IsOwner)
                return;
            
            invoker.States.AddState(new EntityState(Shield.Instance, 1), false, false);
        }

        public void OnInvoked(Entity invoker, bool auxOnly, bool lateJoin)
        {
            ApplyClipData(invoker);
            Prop p = PropManager.Retrieve("core:magic_rock", sockets: invoker.EntityModel.Sockets, modelSpot: ModelSpotLookup.Human.LHand);
            p.GetComponent<IColorable>().SetColor(Color.white);
            curProps[invoker] = p;
        }

        public void OnCancelled(Entity invoker, bool auxOnly, bool lateJoin)
        {
            ReturnProp(invoker);
        }
        
        private void ReturnProp(Entity invoker)
        {
            if(curProps.TryGetValue(invoker, out Prop p)) {
                PropManager.Return(p);
                curProps.Remove(invoker);
            }
        }
    }
    
    public class Shield : State
    {
        public override string LookupKey     => Lookup.Shield.Key;
        public override ushort? DefaultID    => Lookup.Shield.DefaultID;
        public override ushort MaxValue      => 1;
        public override float? MaxDuration   => 20.0f;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        private Mod mod = new();
        private Dictionary<Entity, Effect> effectsLookup = new();

        public static Shield Instance { get; private set; }
        
        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            if(originalVal != null)
                return;
            
            // TODO: Handle delayed EntityModel spawn when joining late.
            entity.Callbacks.Register(mod);
            Effect e = EffectManager.Retrieve("core:shield", sockets: entity.EntityModel.Sockets, modelSpot: ModelSpotLookup.Human.Root);
            effectsLookup[entity] = e;
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            entity.Callbacks.Unregister(mod);
            if(effectsLookup.TryGetValue(entity, out Effect e)) {
                e.Return();
                effectsLookup.Remove(entity);
            }
        }

        private class Mod : IEntityPreTakeDamageCallback
        {
            uint ILockableOrderedListElement.Order => 100;
            
            public void OnEntityInitialize(Entity entity) { }

            public void OnPreDamage(Entity entity, bool fromRpc, bool isTest, ref DamageValue val)
            {
                val.Amount        = (ushort)(val.Amount * 0.60f);
                val.StaggerAmount = (ushort)(val.Amount * 0.60f);
            }
        }
    }
}
