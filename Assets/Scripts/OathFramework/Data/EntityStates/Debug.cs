using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.EntityStates.StateLookup.Debug;

namespace OathFramework.Data.EntityStates
{
    public class GodModeState : State
    {
        public override string LookupKey     => Lookup.GodMode.Key;
        public override ushort? DefaultID    => Lookup.GodMode.DefaultID;
        public override uint Order           => 10_000;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => false;
        public override bool PersistenceSync => false;
        
        public static GodModeState Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
    }
}
