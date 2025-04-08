using OathFramework.Core;
using OathFramework.EntitySystem.Actions;
using OathFramework.Pooling;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

namespace OathFramework.EntitySystem.Monsters
{

    [RequireComponent(typeof(Entity), typeof(MonsterAnimation), typeof(NavMeshAgent))]
    public abstract class MonsterController : NetLoopComponent, 
        ILoopLateUpdate, IEntityParallelUpdate, IEntityControllerBase, 
        IPoolableComponent, IEntityDieCallback, IEntityTakeDamageCallback, 
        IEntityStaggerCallback
    {
        [SerializeField] private int maxTargetReachChecks = 3;
        
        private LateUpdateTask finalizeTask;
        private QList<EntityDistance> targetChecks = new(16);

        [NonSerialized] protected Dictionary<Entity, NavMeshPath> ReachableTargets = new(16);
        protected MonsterMeleeAttack curMeleeAttack;
        
        public override int UpdateOrder => GameUpdateOrder.AIProcessing;
        
        public Transform CTransform      { get; private set; }
        public Entity Entity             { get; private set; }
        public EntityModel Model         { get; private set; }
        public EntityAnimation Animation { get; private set; }
        public NavMeshAgent NavAgent     { get; private set; }
        protected int CurProcessFrame    { get; private set; }
        protected int CurReachCheckFrame { get; private set; }
        
        public MonsterAnimation MonsterAnimation => Animation as MonsterAnimation;
        
        public PoolableGameObject PoolableGO { get; set; }
        public bool IsPlayer => false;
        
        uint ILockableOrderedListElement.Order => 10_000;

        protected virtual void Awake()
        {
            if(Game.Initialized) {
                CurProcessFrame    = EntityManager.GetProcessAIFrame();
                CurReachCheckFrame = EntityManager.GetReachCheckFrame();
            }
            CTransform   = transform;
            Entity       = GetComponent<Entity>();
            Animation    = GetComponent<MonsterAnimation>();
            NavAgent     = GetComponent<NavMeshAgent>();
            finalizeTask = new LateUpdateTask(FinalizeAI, GameUpdateOrder.Finalize);
            finalizeTask.Register();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsTargetReachable(Entity entity) => ReachableTargets.ContainsKey(entity);

        protected override void OnDisable()
        {
            base.OnDisable();
            finalizeTask?.Unregister();
            foreach(NavMeshPath path in ReachableTargets.Values) {
                EntityManager.ReturnPath(path);
            }
            ReachableTargets.Clear();
            targetChecks.Clear();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            finalizeTask?.Register();
        }
        
        public virtual void OnUpdateParallel(Entity entity)
        {
            if(!IsServer)
                return;
        }

        public virtual void LoopLateUpdate()
        {
            if(!IsOwner || Entity.IsDead)
                return;
            
            if(Game.IsQuitting) {
                NavAgent.speed        = 0.0f;
                NavAgent.angularSpeed = 0.0f;
                return;
            }
            
            NavAgent.speed        = Entity.CurStats.speed;
            NavAgent.angularSpeed = Entity.CurStats.turnSpeed;
            if(!ReferenceEquals(curMeleeAttack, null)) {
                // Handle melee attack pivoting and movement.
                HandleAttackMovement();
            } else {
                // Handle movement when there is no melee attack, AND target is within NavAgent.StoppingDistance.
                HandleClosePivot();
            }
            
            if(--CurReachCheckFrame <= 0) {
                CurReachCheckFrame = EntityManager.Instance.ReachCheckFrames;
                FindReachableTargets();
            }
            if(--CurProcessFrame <= 0) {
                CurProcessFrame = EntityManager.Instance.ProcessAIFrames;
                ProcessAI();
            }
        }

        private void FindReachableTargets()
        {
            foreach(NavMeshPath path in ReachableTargets.Values) {
                EntityManager.ReturnPath(path);
            }
            
            ReachableTargets.Clear();
            targetChecks.Clear();
            foreach(EntityTeams team in EntityTypes.GetEnemies(Entity.Team)) {
                Entity.Targeting.GetDistances(targetChecks, team);
            }

            EntityDistance[] arr = targetChecks.Array;
            for(int index = 0, i = 0; index < targetChecks.Count; index++, i++) {
                if(i > maxTargetReachChecks)
                    break;
                
                EntityDistance dist = arr[index];
                if(dist.Entity == null || dist.Entity.IsDead)
                    continue;

                NavMeshPath path = EntityManager.RetrievePath();
                try {
                    bool success = NavAgent.CalculatePath(dist.Entity.transform.position, path);
                    if(!success) {
                        EntityManager.ReturnPath(path);
                        continue;
                    }
                    ReachableTargets.Add(dist.Entity, path);
                } catch(Exception e) {
                    Debug.LogError(e);
                    EntityManager.ReturnPath(path);
                }
            }
        }

        protected virtual void HandleAttackMovement()
        {
            Entity curTarget = Entity.Targeting.CurrentTarget;
            if(ReferenceEquals(curMeleeAttack, null) || curTarget == null || curTarget.IsDead)
                return;
            
            // Pivoting.
            Vector3 pos       = CTransform.position;
            Vector3 targetPos = curTarget.CTransform.position;
            Vector3 posA      = new(targetPos.x, 0.0f, targetPos.z);
            Vector3 posB      = new(pos.x, 0.0f, pos.z);
            Vector3 dirToDest = (posA - posB).normalized;
            float angle       = Vector3.Angle(CTransform.forward, dirToDest);
            if(angle >= curMeleeAttack.PreferredAngle) {
                Quaternion look     = Quaternion.LookRotation(posA - posB);
                float delta         = curMeleeAttack.CurPivotSpeed * Time.deltaTime;
                CTransform.rotation = Quaternion.RotateTowards(CTransform.rotation, look, delta);
            }
            
            // Movement.
            float mult = 1.0f;
            if(curMeleeAttack.AutoBrake) {
                float dist = Entity.Targeting.GetDistance(curTarget);
                mult       = curMeleeAttack.GetBrakeMult(dist);
            }
            NavAgent.Move(CTransform.forward * (curMeleeAttack.CurMoveSpeed * mult * Time.deltaTime));
        }

        protected virtual void HandleClosePivot()
        {
            Entity curTarget = Entity.Targeting.CurrentTarget;
            if(curTarget == null || curTarget.IsDead)
                return;
            
            float dist = Entity.Targeting.GetDistance(curTarget);
            if(dist > NavAgent.stoppingDistance)
                return;
            
            // Pivoting.
            Vector3 pos         = CTransform.position;
            Vector3 targetPos   = curTarget.CTransform.position;
            Vector3 posA        = new(targetPos.x, 0.0f, targetPos.z);
            Vector3 posB        = new(pos.x, 0.0f, pos.z);
            Quaternion look     = Quaternion.LookRotation(posA - posB);
            float delta         = Entity.CurStats.turnSpeed * Time.deltaTime;
            CTransform.rotation = Quaternion.RotateTowards(CTransform.rotation, look, delta);
        }

        protected abstract void ProcessAI();
        protected abstract void FinalizeAI();

        public virtual void OnDamage(Entity entity, bool fromRpc, in DamageValue val)
        {

        }

        public virtual void OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            if(!IsServer)
                return;
            
            NavAgent.speed        = 0.0f;
            NavAgent.angularSpeed = 0.0f;
        }

        public virtual void OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityParallelUpdate)this);
            entity.Callbacks.Register((IEntityDieCallback)this);
            entity.Callbacks.Register((IEntityTakeDamageCallback)this);
            entity.Callbacks.Register((IEntityStaggerCallback)this);
        }

        public virtual void OnStagger(Entity entity, StaggerStrength strength, Entity instigator)
        {
            
        }

        public virtual void OnRetrieve()
        {
            NavAgent.enabled   = true;
            CurProcessFrame    = EntityManager.GetProcessAIFrame();
            CurReachCheckFrame = EntityManager.GetReachCheckFrame();
        }

        public virtual void OnReturn(bool initialization)
        {
            NavAgent.enabled = false;
        }
    }

}
