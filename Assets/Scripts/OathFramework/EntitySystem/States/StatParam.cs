using OathFramework.Core;
using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.EntitySystem.States
{
    public abstract class StatParam : IEquatable<StatParam>, IStringDropdownValue
    {
        public abstract string LookupKey    { get; }
        public abstract ushort? DefaultID   { get; }
        public ushort ID                    { get; private set; }
        public virtual float DefaultValue   => 0.0f;
        public virtual float MinValue       => 0.0f;
        public virtual float MaxValue       => uint.MaxValue;
        public virtual string DropdownVal   => LookupKey;
        string IStringDropdownValue.TrueVal => LookupKey;
        
        public void Initialize()
        {
            if(!StatParamManager.Register(this, out ushort id)) {
                Debug.LogError($"Failed to register Stat Param of Type '{GetType()}'");
                return;
            }
            ID = id;
            StatParamDefaults.Set(ID, DefaultValue);
            OnInitialize();
        }

        protected UIDiff GetDiff(float? oldVal, float curVal) 
            => oldVal == null ? UIDiff.None : Math.Abs(curVal - oldVal.Value) < 0.0001f 
                ? UIDiff.None : curVal > oldVal ? UIDiff.Increment : UIDiff.Decrement;

        protected virtual void OnInitialize() { }

        public virtual bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            val  = null;
            diff = UIDiff.None;
            return false;
        }
        
        public bool Equals(StatParam other) => !ReferenceEquals(null, other) && ID == other.ID;

        public override bool Equals(object obj) => !ReferenceEquals(null, obj) && obj.GetType() == GetType() && Equals((StatParam)obj);

        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() => ID;
        
        public static implicit operator EntityStatParam(StatParam param) => new(param);

        public enum UIDiff
        {
            None      = 0,
            Increment = 1,
            Decrement = 2
        }
    }
}
