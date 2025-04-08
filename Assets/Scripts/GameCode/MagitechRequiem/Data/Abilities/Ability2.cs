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
    public class Ability2 : Ability, IActionAbility
    {
        public override string LookupKey         => "core:earthquake";
        public override ushort? DefaultID        => 2;
        public override bool AutoNetSync         => true;
        public override bool AutoPersistenceSync => true;
        public bool SyncActivation               => false;
        public string ActionAnimParams           => ActionAnimParamsLookup.Ability.Earthquake;
        public override bool HasCharges          => true;
        public override bool IsInstant           => true;
        
        public override float GetMaxCooldown(Entity entity)       => 0.5f;
        public override byte GetMaxCharges(Entity entity)         => 2;
        public override float GetMaxChargeProgress(Entity entity) => 20.0f;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) => new() {
            { "damage", "200" }
        };
        
        public static Ability2 Instance { get; private set; }

        private QList<EntitySpEvent> events       = new();
        private Dictionary<Entity, Prop> curProps = new();
        
        protected override void OnInitialize()
        {
            Instance = this;
            events.Add(new EntitySpEvent(QuickHeal.Instance));
            if(!EffectManager.TryGetID(ModelEffectLookup.Status.QuickHeal, out ushort eID)) {
                Debug.LogError("Failed to retrieve ID for quick heal model effect.");
                return;
            }
            events.Add(new EntitySpEvent(ModelEffect.Instance, new SpEvent.Values(eID, ModelSpotLookup.Human.Root)));
        }

        protected override void OnActivate(Entity invoker, bool auxOnly, bool lateJoin)
        {
            ApplyClipData(invoker);
            ReturnProp(invoker);
            if(!invoker.IsOwner)
                return;

            Vector3 forwardDirection = invoker.transform.forward;
            forwardDirection.y       = 0f;
            Quaternion quat          = Quaternion.LookRotation(forwardDirection);
            EffectManager.Retrieve("core:earthquake", invoker, invoker.transform.position, quat, local: true);
        }

        public void OnInvoked(Entity invoker, bool auxOnly, bool lateJoin)
        {
            ApplyClipData(invoker);
            Prop p = PropManager.Retrieve("core:magic_rock", sockets: invoker.EntityModel.Sockets, modelSpot: ModelSpotLookup.Human.RHand);
            p.GetComponent<IColorable>().SetColor(Color.red);
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
