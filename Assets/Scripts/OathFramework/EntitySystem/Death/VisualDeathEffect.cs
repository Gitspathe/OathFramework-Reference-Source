using UnityEngine;

namespace OathFramework.EntitySystem.Death
{
    public class VisualDeathEffect : DeathEffect
    {
        [field: SerializeField] public GameObject[] Effects { get; private set; }

        public override DeathEffects Type => DeathEffects.VisualEffect;

        protected override void Initialize()
        {
            
        }

        protected override void OnTriggered(in DamageValue lastDamageVal)
        {
            foreach(GameObject effect in Effects) {
                effect.SetActive(true);
            }
        }

        protected override void OnRespawn()
        {
            foreach(GameObject effect in Effects) {
                effect.SetActive(false);
            }
        }
    }
}
