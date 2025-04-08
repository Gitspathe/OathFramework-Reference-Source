using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.SpEvents.SpEventLookup.Effect;

namespace OathFramework.Data.SpEvents
{
    public sealed class ModelEffect : SpEvent
    {
        public override string LookupKey  => Lookup.ModelEffect.Key;
        public override ushort? DefaultID => Lookup.ModelEffect.DefaultID;
        public override byte NumValues    => 2;

        public static ModelEffect Instance { get; private set; }

        public override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApply(Entity entity, Values values = default, bool auxOnly = false)
        {
            ushort effectID = (ushort)values.Value1;
            byte spot       = (byte)values.Value2;
            EffectManager.Retrieve(effectID, sockets: entity.Sockets, modelSpot: spot);
        }
    }
}
