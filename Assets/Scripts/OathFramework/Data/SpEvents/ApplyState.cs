using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.SpEvents.SpEventLookup.State;

namespace OathFramework.Data.SpEvents
{
    public sealed class ApplyState : SpEvent
    {
        public override string LookupKey  => Lookup.ApplyState.Key;
        public override ushort? DefaultID => Lookup.ApplyState.DefaultID;
        public override byte NumValues    => 3;

        public static ApplyState Instance { get; private set; }

        public override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApply(Entity entity, Values values = default, bool auxOnly = false)
        {
            ushort stateID     = (ushort)values.Value1;
            ushort value       = (ushort)values.Value2;
            float  time        = values.Value3 / 1000.0f;
            EntityState eState = new(stateID, value, time);
            if(auxOnly && eState.State.NetSync)
                return;
            
            entity.States.AddState(new EntityState(stateID, value, time));
        }
    }
}
