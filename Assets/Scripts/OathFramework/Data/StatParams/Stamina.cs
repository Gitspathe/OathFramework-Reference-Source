using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.StatParams.ParamLookup.Stamina;

namespace OathFramework.Data.StatParams
{
    public class StaminaRegen : StatParam
    {
        public override string LookupKey   => Lookup.Regen.Key;
        public override ushort? DefaultID  => Lookup.Regen.DefaultID;
        public override float DefaultValue => 50.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "stamina/regen";
        
        public static StaminaRegen Instance { get; private set; }
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
    
    public class StaminaRegenDelay : StatParam
    {
        public override string LookupKey   => Lookup.RegenDelay.Key;
        public override ushort? DefaultID  => Lookup.RegenDelay.DefaultID;
        public override float DefaultValue => 2.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "stamina/regen delay";
        
        public static StaminaRegenDelay Instance { get; private set; }
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
}
