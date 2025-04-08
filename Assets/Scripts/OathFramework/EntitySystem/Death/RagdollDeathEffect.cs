using OathFramework.Effects;
using UnityEngine;

namespace OathFramework.EntitySystem.Death
{
    public class RagdollDeathEffect : DeathEffect
    {
        [SerializeField] private RagdollSource ragdollSource;
        [SerializeField] private RagdollParams ragdollPrefab;

        public override DeathEffects Type => DeathEffects.Ragdoll;

        protected override void Initialize()
        {
            
        }

        protected override void OnTriggered(in DamageValue lastDamageVal)
        {
            RagdollManager.SpawnRagdoll(ragdollSource, ragdollPrefab, in lastDamageVal);
            Handler.Despawn();
        }

        protected override void OnRespawn()
        {
            
        }
    }
}
