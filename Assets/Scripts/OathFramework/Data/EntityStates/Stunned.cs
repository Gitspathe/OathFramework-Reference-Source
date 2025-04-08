using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using System.Collections.Generic;
using UnityEngine;
using Lookup = OathFramework.Data.EntityStates.StateLookup.Offensive;

namespace OathFramework.Data.EntityStates
{
    public class Stunned : State
    {
        public override string LookupKey          => Lookup.Stunned.Key;
        public override ushort? DefaultID         => Lookup.Stunned.DefaultID;
        public override ushort MaxValue           => 1;
        public override float? MaxDuration        => 10;
        public override bool RemoveAllValOnExpire => true;
        public override bool NetSync              => true;

        private ushort eID;
        private Dictionary<Entity, Effect> effectsDict = new();
        
        public static Stunned Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
            if(!EffectManager.TryGetID(ModelEffectLookup.Status.Stunned, out ushort eID)) {
                Debug.LogError("Failed to retrieve ID for quick heal model effect.");
                return;
            }
            this.eID = eID;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            if(effectsDict.TryGetValue(entity, out Effect existing)) {
                existing.Return(true);
                effectsDict.Remove(entity);
            }
            Effect e = EffectManager.Retrieve(eID, sockets: entity.Sockets, modelSpot: ModelSpotLookup.Core.Head);
            effectsDict.Add(entity, e);
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            if(effectsDict.TryGetValue(entity, out Effect existing)) {
                existing.Return();
            }
            effectsDict.Remove(entity);
        }
    }
}
