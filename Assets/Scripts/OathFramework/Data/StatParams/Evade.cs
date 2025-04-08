using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.StatParams.ParamLookup.Evade;

namespace OathFramework.Data.StatParams
{
    public class DodgeStaminaUse : StatParam
    {
        public override string LookupKey   => Lookup.StaminaUse.Key;
        public override ushort? DefaultID  => Lookup.StaminaUse.DefaultID;
        public override float DefaultValue => 50.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "dodge/stamina use";
        
        public static DodgeStaminaUse Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = curVal.ToString("0.0");
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public class DodgeDurationMult : StatParam
    {
        public override string LookupKey   => Lookup.DurationMult.Key;
        public override ushort? DefaultID  => Lookup.DurationMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "dodge/duration mult";
        
        public static DodgeDurationMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = curVal.ToString("0.0");
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public class DodgeSpeedMult : StatParam
    {
        public override string LookupKey   => Lookup.SpeedMult.Key;
        public override ushort? DefaultID  => Lookup.SpeedMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "dodge/speed mult";
        
        public static DodgeSpeedMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val  = curVal.ToString("0.0");
            diff = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public class DodgeIFramesMult : StatParam
    {
        public override string LookupKey   => Lookup.IFramesMult.Key;
        public override ushort? DefaultID  => Lookup.IFramesMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "dodge/iFrames mult";
        
        public static DodgeIFramesMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = curVal.ToString("0.0");
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public class DodgeIFrames : StatParam
    {
        public override string LookupKey   => Lookup.IFrames.Key;
        public override ushort? DefaultID  => Lookup.IFrames.DefaultID;
        public override float DefaultValue => 0.0f;
        public override float MinValue     => -100.0f;
        public override string DropdownVal => "dodge/iFrames";
        
        public static DodgeIFrames Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val  = curVal.ToString("0");
            diff = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
}
