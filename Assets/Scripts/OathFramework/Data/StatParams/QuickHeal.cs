using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.StatParams.ParamLookup.QuickHeal;

namespace OathFramework.Data.StatParams
{
    public sealed class QuickHealCharges : StatParam
    {
        public override string LookupKey   => Lookup.Charges.Key;
        public override ushort? DefaultID  => Lookup.Charges.DefaultID;
        public override string DropdownVal => "quick heal/charges";
        public override float DefaultValue => 3;

        public static QuickHealCharges Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
    }
    
    public sealed class QuickHealAmount : StatParam
    {
        public override string LookupKey   => Lookup.Amount.Key;
        public override ushort? DefaultID  => Lookup.Amount.DefaultID;
        public override string DropdownVal => "quick heal/amount";
        public override float DefaultValue => 350.0f;

        public static QuickHealAmount Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
    }
    
    public sealed class QuickHealSpeedMult : StatParam
    {
        public override string LookupKey   => Lookup.SpeedMult.Key;
        public override ushort? DefaultID  => Lookup.SpeedMult.DefaultID;
        public override string DropdownVal => "quick heal/use speed mult";
        public override float DefaultValue => 1.0f;

        public static QuickHealSpeedMult Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
    }
}
