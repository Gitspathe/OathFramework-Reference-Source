using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.StatParams.ParamLookup.Attack;

namespace OathFramework.Data.StatParams
{
    public sealed class CriticalMult : StatParam
    {
        public override string LookupKey   => Lookup.CriticalMult.Key;
        public override ushort? DefaultID  => Lookup.CriticalMult.DefaultID;
        public override float DefaultValue => 1.5f;
        public override float MinValue     => 1.0f;
        public override string DropdownVal => "attack/critical mult";
        
        public static CriticalMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = (curVal * 100.0f).ToString("0.0") + "%";
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class CriticalChance : StatParam
    {
        public override string LookupKey   => Lookup.CriticalChance.Key;
        public override ushort? DefaultID  => Lookup.CriticalChance.DefaultID;
        public override float DefaultValue => 0.0f;
        public override float MaxValue     => 1.0f;
        public override string DropdownVal => "attack/critical chance";
        
        public static CriticalChance Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = (curVal * 100.0f).ToString("0.0") + "%";
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class BaseDamage : StatParam
    {
        public override string LookupKey   => Lookup.BaseDamage.Key;
        public override ushort? DefaultID  => Lookup.BaseDamage.DefaultID;
        public override float DefaultValue => 0.0f;
        public override float MaxValue     => ushort.MaxValue;
        public override string DropdownVal => "attack/base damage";
        
        public static BaseDamage Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = curVal.ToString("0");
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class DamageMult : StatParam
    {
        public override string LookupKey   => Lookup.DamageMult.Key;
        public override ushort? DefaultID  => Lookup.DamageMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "attack/damage mult";

        public static DamageMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = (curVal * 100.0f).ToString("0.0") + "%";
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class AttackSpeedMult : StatParam
    {
        public override string LookupKey   => Lookup.AttackSpeedMult.Key;
        public override ushort? DefaultID  => Lookup.AttackSpeedMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "attack/speed mult";
        
        public static AttackSpeedMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = (curVal * 100.0f).ToString("0.0") + "%";
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class ReloadSpeedMult : StatParam
    {
        public override string LookupKey   => Lookup.ReloadSpeedMult.Key;
        public override ushort? DefaultID  => Lookup.ReloadSpeedMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "attack/reload speed";
        
        public static ReloadSpeedMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = (curVal * 100.0f).ToString("0.0") + "%";
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class SwapSpeedMult : StatParam
    {
        public override string LookupKey   => Lookup.SwapSpeedMult.Key;
        public override ushort? DefaultID  => Lookup.SwapSpeedMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.01f;
        public override string DropdownVal => "attack/swap speed";
        
        public static SwapSpeedMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = (curVal * 100.0f).ToString("0.0") + "%";
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class AccuracyMult : StatParam
    {
        public override string LookupKey   => Lookup.AccuracyMult.Key;
        public override ushort? DefaultID  => Lookup.AccuracyMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "attack/accuracy";
        
        public static AccuracyMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = (curVal * 100.0f).ToString("0.0") + "%";
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class ProjectileSpeedMult : StatParam
    {
        public override string LookupKey   => Lookup.ProjectileSpeedMult.Key;
        public override ushort? DefaultID  => Lookup.ProjectileSpeedMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "attack/projectile speed";

        public static ProjectileSpeedMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val          = (curVal * 100.0f).ToString("0.0") + "%";
            diff         = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
    
    public sealed class ExplosiveRangeMult : StatParam
    {
        public override string LookupKey   => Lookup.ExplosiveRangeMult.Key;
        public override ushort? DefaultID  => Lookup.ExplosiveRangeMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "attack/explosive range";

        public static ExplosiveRangeMult Instance { get; private set; }
        protected override void OnInitialize() => Instance = this;

        public override bool GetUIInfo(Stats oldStats, Stats curStats, out string val, out UIDiff diff)
        {
            float oldVal = oldStats?.GetParam(this) ?? 0.0f;
            float curVal = curStats.GetParam(this);
            val  = (curVal * 100.0f).ToString("0.0") + "%";
            diff = GetDiff(oldStats == null ? null : oldVal, curVal);
            return true;
        }
    }
}
