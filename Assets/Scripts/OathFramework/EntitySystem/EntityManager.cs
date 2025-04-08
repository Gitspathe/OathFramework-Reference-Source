using System;
using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Pooling;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem
{
    public sealed partial class EntityManager : Subsystem, IGameQuitCallback, IResetGameStateCallback
    {
        [field: Header("Friendly Fire Settings")]
        
        [field: SerializeField] public FriendlyFireConfig SelfFireProjectile     { get; private set; }
        [field: SerializeField] public FriendlyFireConfig SelfFireExplosive      { get; private set; }
        [field: SerializeField] public FriendlyFireConfig SelfFireMelee          { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public FriendlyFireConfig FriendlyFireProjectile { get; private set; }
        [field: SerializeField] public FriendlyFireConfig FriendlyFireExplosive  { get; private set; }
        [field: SerializeField] public FriendlyFireConfig FriendlyFireMelee      { get; private set; }

        public static EntityManager Instance { get; private set; }

        public override string Name => "Entity Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EntityManager;

        public override async UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(EntityManager)} singleton.");
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            
            if(INISettings.GetNumeric("AI/ProcessTimeSlicing", out int val)) {
                ProcessAIFrames = val;
            }
            if(INISettings.GetNumeric("AI/NavmeshTimeSlicing", out val)) {
                ReachCheckFrames = val;
            }
            if(INISettings.GetBool("AI/MultithreadedAI") == false) {
                parallel = false;
            }
            
            EntityJobs.SetEntityManagerInstance(this);
            foreach(EntityParams @params in entites) {
                if(!RegisterParams(@params)) {
                    Debug.LogError($"Attempted to register duplicate entity for ID {@params.LookupKey}");
                    continue;
                }
                if(@params.IsPooled && @params.Pool.Prefab != null) {
                    await PoolManager.RegisterPoolAsync(timer, new PoolManager.GameObjectPool(@params.Pool), true);
                }
            }

            pathsPool = new ObjectPool<NavMeshPath>(256);
            new EarlyUpdateTask(DoEntityPreprocessing, GameUpdateOrder.EntityPreprocessing).Register();
            new LateUpdateTask(FinishEntityPreprocessing, GameUpdateOrder.FinishEntityPreprocessing).Register();
            GameCallbacks.Register((IGameQuitCallback)this);
            GameCallbacks.Register((IResetGameStateCallback)this);
        }

        private void Clear()
        {
            if(NetworkManager.Singleton.IsServer) {
                foreach(Entity e in allEntities) {
                    e.GetComponent<NetworkObject>().Despawn(false);
                }
            }
            allEntities.Clear();
            entitiesByTeam.Clear();
            toAdd.Clear();
            toRemove.Clear();
        }

        public static bool ApplyFriendlyFireModifiers(IEntity target, ref DamageValue val)
        {
            if(!val.GetInstigator(out Entity source) || !EntityTypes.AreFriends(target.Team, source.Team))
                return false;
            
            FriendlyFireConfig conf = null;
            Entity tEntity          = target as Entity;
            EntityManager i         = Instance;
            switch(val.Source) {
                case DamageSource.Projectile: {
                    conf = !ReferenceEquals(tEntity, null) && tEntity == source ? i.SelfFireProjectile : i.FriendlyFireProjectile;
                } break;
                case DamageSource.Melee: {
                    conf = !ReferenceEquals(tEntity, null) && tEntity == source ? i.SelfFireMelee : i.FriendlyFireMelee;
                } break;
                case DamageSource.Explosion: {
                    conf = !ReferenceEquals(tEntity, null) && tEntity == source ? i.SelfFireExplosive : i.FriendlyFireExplosive;
                } break;
                
                case DamageSource.Undefined:
                case DamageSource.Status:
                case DamageSource.Environment:
                case DamageSource.SyncDeath:
                case DamageSource.DieCommand:
                default:
                    break;
            }
            if(conf == null)
                return false;

            val.Amount        = (ushort)Mathf.Clamp(val.Amount * conf.DamageRate, 0.0f, ushort.MaxValue);
            val.StaggerAmount = (ushort)Mathf.Clamp(val.StaggerAmount * conf.StaggerRate, 0.0f, ushort.MaxValue);
            if(val.StaggerAmount <= 0) {
                val.StaggerStrength = StaggerStrength.None;
            }
            return conf.IsImmunity;
        }

        void IGameQuitCallback.OnGameQuit()
        {
            Clear();
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            Clear();
        }
    }

    [Serializable]
    public class FriendlyFireConfig
    {
        [field: SerializeField] public float DamageRate  { get; private set; } = 0.5f;
        [field: SerializeField] public float StaggerRate { get; private set; } = 0.5f;

        public bool IsImmunity => DamageRate <= 0.0f && StaggerRate <= 0.0f;
    }
}
