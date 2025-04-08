using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.EntitySystem.Death
{
    public abstract class DeathEffect : MonoBehaviour, IEntityInitCallback
    {
        public abstract DeathEffects Type { get; }
        public EntityDeathHandler Handler { get; private set; }
        public Entity Entity              => Handler.Entity;
        
        uint ILockableOrderedListElement.Order => 10_000;
        
        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            Handler = entity.Death;
            Handler.RegisterDeathEffect(this);
            Initialize();
        }

        public void Trigger(in DamageValue lastDamageVal)
        {
            OnTriggered(in lastDamageVal);
        }

        public void Respawned()
        {
            OnRespawn();
        }

        protected abstract void Initialize();
        protected abstract void OnTriggered(in DamageValue lastDamageVal);
        protected abstract void OnRespawn();
    }
}
