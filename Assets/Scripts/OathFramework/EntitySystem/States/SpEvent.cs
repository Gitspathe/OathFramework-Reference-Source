using System;
using UnityEngine;

namespace OathFramework.EntitySystem.States
{
    public abstract class SpEvent : IEquatable<SpEvent>
    {
        public abstract string LookupKey  { get; }
        public abstract ushort? DefaultID { get; }

        public virtual byte NumValues       => 0;
        public virtual bool ApplyOnNoDamage => true;
        
        public ushort ID { get; private set; }

        public void Initialize() => OnInitialize();
        
        public virtual void OnInitialize() { }
        
        public void PostCtor()
        {
            if(NumValues > 4) {
                Debug.LogError($"SpEvents can have a maximum of 4 values, however '{GetType()} specifies {NumValues}");
                return;
            }
            if(!SpEventManager.Register(this, out ushort id)) {
                Debug.LogError($"Failed to register Entity {nameof(SpEvent)} of Type '{GetType()}'");
                return;
            }
            ID = id;
        }

        public void Apply(Entity entity, Values values = default, bool auxOnly = false)
        {
            OnApply(entity, values, auxOnly);
        }
        
        public bool Equals(SpEvent other)
        {
            if(ReferenceEquals(null, other)) {
                return false;
            }
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) {
                return false;
            }
            return obj.GetType() == GetType() && Equals((SpEvent)obj);
        }

        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() => ID;

        protected abstract void OnApply(Entity entity, Values values = default, bool auxOnly = false);
        
        public readonly struct Values
        {
            public int Value1 { get; }
            public int Value2 { get; }
            public int Value3 { get; }
            public int Value4 { get; }

            public Values(int value1, int value2 = 0, int value3 = 0, int value4 = 0)
            {
                Value1 = value1;
                Value2 = value2;
                Value3 = value3;
                Value4 = value4;
            }
        }
    }
}
