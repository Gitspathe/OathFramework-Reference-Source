using UnityEngine;

namespace OathFramework.EntitySystem.Death
{
    public class AnimationDeathEffect : DeathEffect
    {
        [field: SerializeField] public string AnimBoolParam { get; private set; } = "IsDead";

        private int animBoolHash;

        public override DeathEffects Type => DeathEffects.Animation;

        protected override void Initialize()
        {
            animBoolHash = Animator.StringToHash(AnimBoolParam);
        }

        protected override void OnTriggered(in DamageValue lastDamageVal)
        {
            Entity.EntityModel.Animator.SetBool(animBoolHash, true);
        }

        protected override void OnRespawn()
        {
            Entity.EntityModel.Animator.SetBool(animBoolHash, false);
        }
    }
}
