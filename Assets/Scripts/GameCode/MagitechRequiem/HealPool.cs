using Cysharp.Threading.Tasks;
using GameCode.MagitechRequiem.Data.Perks;
using OathFramework.Core;
using OathFramework.Data;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.Networking;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace GameCode.MagitechRequiem
{
    public class HealPool : NetLoopComponent, ILoopUpdate, IPoolableComponent
    {
        [SerializeField] private Effect effect;

        private Dictionary<Entity, float> timers     = new();
        private Dictionary<Entity, float> timersCopy = new();
        private QList<Entity> entities               = new();
        private float timer;
        private bool dissipate;
        
        public PoolableGameObject PoolableGO { get; set; }
        
        private static string AuxEffectKey => "core:heal_aura_aux";

        private void OnTriggerEnter(Collider other)
        {
            if(dissipate || !other.TryGetComponent(out EffectReceiver e) || !e.Entity.IsPlayer)
                return;

            HandleModelEffect(e.Entity);
            if(NetClient.Self == null || e.Entity != NetClient.Self.PlayerController.Entity)
                return;

            timers.Add(e.Entity, 0.0f);
        }

        private void HandleModelEffect(Entity entity)
        {
            if(!entity.ModelSpawned) {
                _ = DelayedModelEffect(entity);
                return;
            } 
            
            ModelSocketHandler handler = entity.EntityModel.Sockets;
            bool hasEffect             = handler.TryGetEffect(AuxEffectKey, out Effect efx);
            entities.Add(entity);
            if(hasEffect && efx.IsDissipating) {
                efx.Return(true);
                hasEffect = false;
            } 
            if(!hasEffect) {
                EffectManager.Retrieve(AuxEffectKey, sockets: handler, modelSpot: ModelSpotLookup.Human.Root);
            }
        }
        
        private async UniTask DelayedModelEffect(Entity entity)
        {
            if(!await entity.WaitForModel())
                return;
            
            ModelSocketHandler handler = entity.EntityModel.Sockets;
            EffectManager.Retrieve(AuxEffectKey, sockets: handler, modelSpot: ModelSpotLookup.Human.Root);
        }
        
        private void OnTriggerExit(Collider other)
        {
            if(!other.TryGetComponent(out EffectReceiver e) || !e.Entity.IsPlayer)
                return;
            
            ModelSocketHandler handler = e.Entity.EntityModel.Sockets;
            entities.Remove(e.Entity);
            timers.Remove(e.Entity);
            if(handler.TryGetEffect(AuxEffectKey, out Effect efx)) {
                efx.Return();
            }
        }

        void ILoopUpdate.LoopUpdate()
        {
            if(dissipate)
                return;
            
            timer -= Time.deltaTime;
            if(timer <= 0.0f) {
                Clear();
                dissipate = true;
                if(IsServer || effect.Local) {
                    effect.Return();
                }
            }
            if(effect.Source == null)
                return;

            // Perk bonus.
            float healAmt = 3;
            if(!(effect.Source is Entity source) || source.Perks.HasPerk(Perk1.Instance)) {
                healAmt *= 1.3f;
            }
            
            foreach((Entity e, float value) in timers) {
                float t     = value + Time.deltaTime;
                float extra = e.CurStats.maxHealth / 200.0f;
                while(t > 0.1f) {
                    e.Heal(new HealValue((ushort)(healAmt + extra)));
                    t -= 0.1f;
                }
                timersCopy[e] = t;
            }
            timers.Clear();
            foreach(KeyValuePair<Entity, float> pair in timersCopy) {
                timers.Add(pair.Key, pair.Value);
            }
            timersCopy.Clear();
        }

        private void Clear()
        {
            for(int i = 0; i < entities.Count; i++) {
                Entity e                   = entities.Array[i];
                ModelSocketHandler handler = e.EntityModel.Sockets;
                if(handler.TryGetEffect(AuxEffectKey, out Effect efx)) {
                    efx.Return();
                }
            }
            entities.Clear();
            timers.Clear();
            timersCopy.Clear();
        }

        void IPoolableComponent.OnRetrieve()
        {
            timer     = 12.0f;
            dissipate = false;
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            Clear();
        }
    }
}
