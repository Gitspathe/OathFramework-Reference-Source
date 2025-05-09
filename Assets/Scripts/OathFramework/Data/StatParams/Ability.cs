using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.StatParams.ParamLookup.Ability;

namespace OathFramework.Data.StatParams
{
    public sealed class AbilityUseSpeedMult : StatParam
    {
        public override string LookupKey   => Lookup.UseSpeedMult.Key;
        public override ushort? DefaultID  => Lookup.UseSpeedMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override float MinValue     => 0.0f;
        public override string DropdownVal => "ability/use speed mult";
        
        public static AbilityUseSpeedMult Instance { get; private set; }
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
