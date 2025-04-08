using OathFramework.Achievements;
using OathFramework.Core;
using OathFramework.Data.EntityStates;
using OathFramework.Data.SpEvents;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.EquipmentSystem;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace GameCode.MagitechRequiem
{
    public class MagitechFlashbangExplosion : NetLoopComponent, 
        IPoolableComponent, IEffectSpawned, IEffectBoxDamagedEntity
    {
        [SerializeField] private float baseRadius = 3.0f;
        [SerializeField] private Effect effect;
        [SerializeField] private EffectBox[] effectBoxes;

        private int hitEnemies;
        private Animation anim;
        private QList<EntitySpEvent> spEvents = new();
        
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
            spEvents.Clear();
            spEvents.Add(new EntitySpEvent(ApplyState.Instance, new SpEvent.Values(Stunned.Instance.ID, 1, 3000)));
            if(!(effect.Source is Entity source))
                return;
            
            foreach(EffectBox hb in effectBoxes) {
                hb.Deactivate();
            }
            List<HitEffectInfo> hitEffects = null;
            Vector3 scale                  = Vector3.one;
            float radius                   = baseRadius;
            DamageValue val = new(
                0, 
                source.transform.position, 
                DamageSource.Explosion, 
                StaggerStrength.None, 
                0, 
                instigator: source,
                spEvents: spEvents
            );
            if(effect.ExtraData != 0 && EquippableGrenadeUtil.GetEquippable(source, effect.ExtraData, out Equippable equippable)) {
                EquippableGrenadeUtil.GetDamageValue(source, equippable, ref radius, out hitEffects, in val, out val, true);
                EquippableGrenadeUtil.GetHitEffects(source, equippable, out hitEffects);
                if(radius <= 0.001f) {
                    gameObject.SetActive(false);
                    return;
                }
                float scaleF = radius / baseRadius;
                scale        = new Vector3(scaleF, scaleF, scaleF);
            }
            transform.localScale = scale;
            if(!source.IsOwner)
                return;
            
            foreach(EffectBox eb in effectBoxes) {
                eb.Setup(val, EntityTypes.AllTypes, hitEffects, ignore: source);
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
            hitEnemies = 0;
            Execute();
        }
        
        void IEffectBoxDamagedEntity.OnEffectBoxDamagedEntity(IEntity entity, in DamageValue damageValue)
        {
            if(!IsOwner)
                return;
            
            if(++hitEnemies >= 5) {
                AchievementManager.UnlockAchievement("enemies_flashbanged_instant_5");
            }
        }
    }
}
