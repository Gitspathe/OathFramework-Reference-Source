using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.StatParams.ParamLookup.Progression;

namespace OathFramework.Data.StatParams
{
    public sealed class ExpReward : StatParam
    {
        public override string LookupKey   => Lookup.ExpReward.Key;
        public override ushort? DefaultID  => Lookup.ExpReward.DefaultID;
        public override string DropdownVal => "progression/exp reward";
        
        public static ExpReward Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
    }

    public sealed class ExpMult : StatParam
    {
        public override string LookupKey   => Lookup.ExpMult.Key;
        public override ushort? DefaultID  => Lookup.ExpMult.DefaultID;
        public override float DefaultValue => 1.0f;
        public override string DropdownVal => "progression/exp mult";

        public static ExpMult Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
    }
}
