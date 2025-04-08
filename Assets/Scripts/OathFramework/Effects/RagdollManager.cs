using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Pooling;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.Effects
{
    public class RagdollManager : Subsystem
    {
        public override string Name => "Ragdoll Manager";
        public override uint LoadOrder => SubsystemLoadOrders.RagdollManager;

        [field: SerializeField] public float SleepThreshold              { get; private set; } = 0.05f;
        [field: SerializeField] public List<RagdollParams> RagdollParams { get; private set; }
        
        public static RagdollManager Instance { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(RagdollManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }
            
            DontDestroyOnLoad(gameObject);
            Instance = this;
            foreach(RagdollParams @params in RagdollParams) {
                PoolManager.RegisterPool(new PoolManager.GameObjectPool(@params.PrefabPool), true);
            }
            return UniTask.CompletedTask;
        }

        public static RagdollTarget SpawnRagdoll(RagdollSource source, RagdollParams @params, in DamageValue lastDamageVal)
        {
            Transform st          = source.transform;
            PoolableGameObject go = PoolManager.Retrieve(@params.PrefabPool.Prefab, st.position, st.rotation, st.localScale);
            RagdollTarget target  = go.GetComponent<RagdollTarget>();
            if(target == null) {
                Debug.LogError($"Failed to retrieve {nameof(RagdollTarget)} on {go.name}.");
                return null;
            }
            
            target.AssignTransforms(source);
            switch(lastDamageVal.Source) {
                case DamageSource.Projectile: {
                    ApplyForceProjectile(target, lastDamageVal.HitPosition, lastDamageVal.StaggerStrength);
                } break;
                case DamageSource.Melee: {
                    ApplyForceMelee(target, lastDamageVal.HitPosition, lastDamageVal.StaggerStrength);
                } break;
                case DamageSource.Explosion: {
                    ApplyForceExplosion(target, lastDamageVal.HitPosition, lastDamageVal.StaggerStrength);
                } break;

                case DamageSource.Undefined:
                case DamageSource.SyncDeath:
                case DamageSource.DieCommand:
                default:
                    break;
            }
            return target;
        }

        private static void ApplyForceProjectile(RagdollTarget target, Vector3 hitPos, StaggerStrength stagger)
        {
            switch(stagger) {
                case StaggerStrength.None:
                case StaggerStrength.Low: {
                    target.ApplyForce(17500.0f, 5.0f, 0.2f, hitPos);
                } break;
                case StaggerStrength.Medium: {
                    target.ApplyForce(19000.0f, 5.0f, 0.2f, hitPos);
                } break;
                case StaggerStrength.High: {
                    target.ApplyForce(23000.0f, 5.0f, 0.2f, hitPos);
                } break;
            }
        }
        
        private static void ApplyForceMelee(RagdollTarget target, Vector3 hitPos, StaggerStrength stagger)
        {
            switch(stagger) {
                case StaggerStrength.None:
                case StaggerStrength.Low: {
                    target.ApplyForce(18000.0f, 5.0f, 0.0f, hitPos);
                } break;
                case StaggerStrength.Medium: {
                    target.ApplyForce(21500.0f, 5.0f, 0.0f, hitPos);
                } break;
                case StaggerStrength.High: {
                    target.ApplyForce(24000.0f, 5.0f, 0.0f, hitPos);
                } break;
            }
        }
        
        private static void ApplyForceExplosion(RagdollTarget target, Vector3 hitPos, StaggerStrength stagger)
        {
            switch(stagger) {
                case StaggerStrength.None:
                case StaggerStrength.Low: {
                    target.ApplyForce(21500.0f, 5.0f, 1.0f, hitPos);
                } break;
                case StaggerStrength.Medium: {
                    target.ApplyForce(24000.0f, 5.0f, 1.0f, hitPos);
                } break;
                case StaggerStrength.High: {
                    target.ApplyForce(27500.0f, 5.0f, 1.0f, hitPos);
                } break;
            }
        }
    }
}
