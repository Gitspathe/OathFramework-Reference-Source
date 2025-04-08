using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Pooling;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.Death
{
    public class EntityDeathHandler : NetLoopComponent, IPoolableComponent
    {
        private Dictionary<DeathEffects, DeathEffect> effects = new();
        private HashSet<DeathEffect> curEffects               = new();
        private NetworkObject netObj;
        private AccessToken token;

        public Entity Entity                                { get; private set; }
        [field: SerializeField] public DestroyAfterTime TTL { get; private set; }
        public PoolableGameObject PoolableGO                { get; set; }
        
        private bool HasNullDeathParams => Entity.Params.DeathEffects == null || Entity.Params.DeathEffects.Length == 0;
        
        private void Awake()
        {
            Entity = GetComponent<Entity>();
            netObj = GetComponent<NetworkObject>();
        }

        public void SetCallbackToken(AccessToken token)
        {
            this.token = token;
        }

        public void RegisterDeathEffect(DeathEffect effect)
        {
            if(!effects.TryAdd(effect.Type, effect)) {
                Debug.LogError($"Attempted to register duplicate death effect type for {effect.Type}");
            }
        }
        
        public void TriggerDeath(in DamageValue lastDamageVal)
        {
            // No DamageRecorder - Fire one ScoredKill callback to the last attacker.
            // With DamageRecorder - Fire ScoredKill callbacks for every previous attacker.
            if(!ReferenceEquals(Entity.DamageRecorder, null)) {
                Entity.DamageRecorder.FireScoredKillCallbacks(in lastDamageVal);
            } else {
                if(lastDamageVal.GetInstigator(out Entity instigator)) {
                    instigator.OnScoredKill(Entity, in lastDamageVal, 1.0f);
                }
            }

            bool nullDeathParams = HasNullDeathParams;
            Entity.Callbacks.Access.OnDie(token, in lastDamageVal);
            if(IsServer && TTL != null) {
                TTL.enabled = true;
                if(nullDeathParams)
                    return;
            }
            if(nullDeathParams) {
                Despawn();
                return;
            }
            foreach(DeathEffects effectType in Entity.Params.DeathEffects) {
                if(!effects.TryGetValue(effectType, out DeathEffect effect)) {
                    Debug.LogError($"No death effect for '{effectType}' found on {name}");
                    continue;
                }
                if(!effect.isActiveAndEnabled)
                    continue;
                
                effect.Trigger(in lastDamageVal);
                curEffects.Add(effect);
            }
        }

        public void Despawn()
        {
            if(IsServer) {
                _ = DelayedDespawn(); // Despawn must be delayed so death messages are sent.
            }
        }

        private async UniTask DelayedDespawn()
        {
            if(await UniTask.Delay(TimeSpan.FromSeconds(3.0f), cancellationToken: Game.ResetCancellation.Token).SuppressCancellationThrow())
                return;
            
            netObj.Despawn();
        }

        void IPoolableComponent.OnRetrieve()
        {
            foreach(DeathEffect effect in curEffects) {
                effect.Respawned();
            }
            
            curEffects.Clear();
            if(IsServer && TTL != null) {
                TTL.enabled = false;
            }
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            
        }
    }
}
