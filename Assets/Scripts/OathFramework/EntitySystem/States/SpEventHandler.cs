using OathFramework.Core;
using OathFramework.Utility;
using System;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.States
{
    [RequireComponent(typeof(Entity))]
    public sealed class SpEventHandler : MonoBehaviour
    {
        private Entity entity;

        private void Awake()
        {
            entity = GetComponent<Entity>();
        }

        public void ApplyEvent(EntitySpEvent ev, bool auxOnly = false)
        {
            ev.Event?.Apply(entity, ev.Values, auxOnly);
        }

        public void ApplyEvents(QList<EntitySpEvent> ev, bool auxOnly = false)
        {
            int count = ev.Count;
            for(int i = 0; i < count; i++) {
                ApplyEvent(ev.Array[i], auxOnly);
            }
        }
    }

    public struct NetSpEvent : INetworkSerializable
    {
        public EntitySpEvent SpEvent { get; private set; }

        public NetSpEvent(EntitySpEvent spEvent)
        {
            SpEvent = spEvent;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if(serializer.IsReader) {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out ushort id);
                SpEvent ev = SpEventManager.Get(id);
                int val1 = 0, val2 = 0, val3 = 0, val4 = 0;
                if(ev.NumValues > 0) { reader.ReadValueSafe(out val1); }
                if(ev.NumValues > 1) { reader.ReadValueSafe(out val2); }
                if(ev.NumValues > 2) { reader.ReadValueSafe(out val3); }
                if(ev.NumValues > 3) { reader.ReadValueSafe(out val4); }
                SpEvent = new EntitySpEvent(ev, new SpEvent.Values(val1, val2, val3, val4));
            } else {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(SpEvent.ID);
                if(SpEvent.Event.NumValues > 0) { writer.WriteValueSafe(SpEvent.Values.Value1); }
                if(SpEvent.Event.NumValues > 1) { writer.WriteValueSafe(SpEvent.Values.Value2); }
                if(SpEvent.Event.NumValues > 2) { writer.WriteValueSafe(SpEvent.Values.Value3); }
                if(SpEvent.Event.NumValues > 3) { writer.WriteValueSafe(SpEvent.Values.Value4); }
            }
        }
    }

    public readonly struct EntitySpEvent : IEquatable<EntitySpEvent>
    {
        public SpEvent Event         { get; }
        public SpEvent.Values Values { get; }
        
        public string LookupKey      => Event.LookupKey;
        public ushort ID             => Event.ID;
        
        public EntitySpEvent(SpEvent ev, SpEvent.Values values = default)
        {
            Event  = ev;
            Values = values;
        }

        public EntitySpEvent(string key, SpEvent.Values values = default)
        {
            Values = values;
            if(!SpEventManager.TryGet(key, out SpEvent ev)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(key)));
                }
                Event = null;
            }
            Event = ev;
        }

        public EntitySpEvent(ushort id, SpEvent.Values values = default)
        {
            Values = values;
            if(!SpEventManager.TryGet(id, out SpEvent ev)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(id)));
                }
                Event = null;
            }
            Event = ev;
        }

        public bool Equals(EntitySpEvent other) => ID == other.ID;
        public override bool Equals(object obj) => obj is EntitySpEvent other && Equals(other);
        public override int GetHashCode() => ID;
    }
}
