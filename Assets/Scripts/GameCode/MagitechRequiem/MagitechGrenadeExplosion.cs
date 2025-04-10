using OathFramework.Achievements;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EquipmentSystem;
using OathFramework.Pooling;
using System.Collections.Generic;
using UnityEngine;

namespace GameCode.MagitechRequiem
{
    public class MagitechGrenadeExplosion : NetLoopComponent, 
        IPoolableComponent, IEffectSpawned, IEffectBoxDamagedEntity
    {
        [SerializeField] private float baseRadius = 3.0f;
        [SerializeField] private Effect effect;
        [SerializeField] private EffectBox[] effectBoxes;
        
        private int killedEnemies;
        private Animation anim;
        
        public PoolableGameObject PoolableGO { get; set; }

        private void Awake()
        {
            anim = GetComponent<Animation>();
            for(int i = 0; i < effectBoxes.Length; i++) {
                effectBoxes[i].Callbacks.Register((IEffectBoxDamagedEntity)this);
            }
        }
        
        private void Execute()
        {
            if(!(effect.Source is Entity source))
                return;
            
            foreach(EffectBox hb in effectBoxes) {
                hb.Deactivate();
            }
            List<HitEffectInfo> hitEffects = null;
            Vector3 scale                  = Vector3.one;
            float radius                   = baseRadius;
            DamageValue val = new(
                325,
                source.transform.position,
                DamageSource.Explosion,
                StaggerStrength.Medium,
                100,
                instigator: source
            );
            if(effect.ExtraData != 0 && EquippableGrenadeUtil.GetEquippable(source, effect.ExtraData, out Equippable equippable)) {
                EquippableGrenadeUtil.GetDamageValue(source, equippable, ref radius, out hitEffects, in val, out val);
                EquippableGrenadeUtil.GetHitEffects(source, equippable, out hitEffects);
                if(radius <= 0.001f) {
                    gameObject.SetActive(false);
                    return;
                }
                float scaleF = radius / baseRadius;
                scale = new Vector3(scaleF, scaleF, scaleF);
            }
            transform.localScale = scale;
            foreach(EffectBox eb in effectBoxes) {
                eb.Setup(val, EntityTypes.AllTypes, hitEffects, isOwner: effect.Source.IsOwner);
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
        
        void IEffectBoxDamagedEntity.OnEffectBoxDamagedEntity(IEntity entity, in DamageValue damageValue)
        {
            if(!IsOwner || !(entity is Entity e))
                return;

            if(e.IsDead || e.CurStats.health == 0) {
                killedEnemies++;
            }
            if(killedEnemies >= 5) {
                AchievementManager.UnlockAchievement("enemies_killed_total_grenade_instant_5");
            }
        }
    }
}
