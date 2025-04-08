using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;

namespace OathFramework.Data.EntityStates
{
    public class Invulnerable : State, IEntityPreTakeDamageCallback
    {
        public override string LookupKey          => StateLookup.Defensive.Invulnerable.Key;
        public override ushort? DefaultID         => StateLookup.Defensive.Invulnerable.DefaultID;
        public override float? MaxDuration        => 3.0f;
        public override ushort MaxValue           => 1;
        public override bool RemoveAllValOnExpire => true;
        public override bool NetSync              => true;
        public override bool PersistenceSync      => true;

        public static Invulnerable Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            entity.Callbacks.Register(this);
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            entity.Callbacks.Unregister(this);
        }
        
        void IEntityPreTakeDamageCallback.OnPreDamage(Entity entity, bool fromRpc, bool isTest, ref DamageValue val)
        {
            if(val.IsUnavoidableDeath)
                return;

            val.Amount          = 0;
            val.StaggerAmount   = 0;
            val.StaggerStrength = StaggerStrength.None;
        }
    }
}
