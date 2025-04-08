using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.Pooling;
using UnityEngine;

namespace GameCode.MagitechRequiem
{
    public class Earthquake : NetLoopComponent, 
        IPoolableComponent, IEffectSpawned
    {
        [SerializeField] private Effect effect;
        [SerializeField] private EffectBox[] effectBoxes;
        private Animation anim;
        
        public PoolableGameObject PoolableGO { get; set; }

        private void Awake()
        {
            anim = GetComponent<Animation>();
        }
        
        private void Execute()
        {
            foreach(EffectBox hb in effectBoxes) {
                hb.Deactivate();
            }
            if(!(effect.Source is Entity source) || !source.IsOwner)
                return;

            DamageValue val = new(
                250, 
                source.transform.position, 
                DamageSource.Explosion, 
                StaggerStrength.Medium, 
                100, 
                instigator: source
            );
            foreach(EffectBox hb in effectBoxes) {
                hb.Setup(val, EntityTypes.AllTypes, ignore: source);
            }
            anim.Play();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        void IPoolableComponent.OnRetrieve()
        {
            
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            anim.Stop();
        }

        void IEffectSpawned.OnEffectSpawned()
        {
            Execute();
        }
    }
}
