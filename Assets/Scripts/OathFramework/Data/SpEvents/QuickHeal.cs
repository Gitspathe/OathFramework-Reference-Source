using OathFramework.Data.StatParams;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.SpEvents.SpEventLookup.QuickHeal;

namespace OathFramework.Data.SpEvents
{
    public sealed class QuickHeal : SpEvent
    {
        public override string LookupKey  => Lookup.Trigger.Key;
        public override ushort? DefaultID => Lookup.Trigger.DefaultID;
        
        public static QuickHeal Instance { get; private set; }

        public override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApply(Entity entity, Values values = default, bool auxOnly = false)
        {
            if(auxOnly)
                return;
            
            Stats stats   = entity.CurStats;
            float healAmt = stats.GetParam(QuickHealAmount.Instance);
            entity.Heal(new HealValue((ushort)healAmt, source: HealSource.Player, instigator: entity));
        }
    }
}
