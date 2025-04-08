using OathFramework.AbilitySystem;
using OathFramework.Data;
using OathFramework.Data.SpEvents;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace GameCode.MagitechRequiem.Data.Abilities
{
    public class Ability1 : Ability, IActionAbility
    {
        public override string LookupKey         => "core:heal_pool";
        public override ushort? DefaultID        => 1;
        public override bool AutoNetSync         => true;
        public override bool AutoPersistenceSync => true;
        public bool SyncActivation               => false;
        public string ActionAnimParams           => ActionAnimParamsLookup.Ability.HealingPool;
        public override bool HasCharges          => true;
        public override bool IsInstant           => true;

        public override float GetMaxCooldown(Entity entity)       => 0.5f;
        public override byte GetMaxCharges(Entity entity)         => 1;
        public override float GetMaxChargeProgress(Entity entity) => 60.0f;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) => new() {
            { "duration", "10" }
        };

        public static Ability1 Instance { get; private set; }

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

            EffectManager.Retrieve("core:heal_pool", invoker, invoker.transform.position, local: true);
        }

        public void OnInvoked(Entity invoker, bool auxOnly, bool lateJoin)
        {
            ApplyClipData(invoker);
            Prop p = PropManager.Retrieve("core:magic_rock", sockets: invoker.EntityModel.Sockets, modelSpot: ModelSpotLookup.Human.LHand);
            p.GetComponent<IColorable>().SetColor(Color.green);
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
}
