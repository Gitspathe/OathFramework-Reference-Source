using OathFramework.Core;
using OathFramework.Data.EntityStates;
using OathFramework.EntitySystem.Actions;
using OathFramework.Utility;
using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace OathFramework.EntitySystem.Monsters
{ 

    public class BasicMonsterController : MonsterController
    {
        private MonsterAnimation anim;
        private Entity nextTarget;
        private State state;

        private Action<InterruptionSource> onAttackCompletedDelegate;
        private QList<EntityDistance> possibleTargets     = new();
        private QList<MonsterMeleeAttack> meleeAttacks   = new();
        private QList<MonsterMeleeAttack> attacksInRange = new();

        protected override void Awake()
        {
            base.Awake();
            anim = GetComponent<MonsterAnimation>();
            onAttackCompletedDelegate = OnAttackCompleted;
        }

        protected override void FinalizeAI()
        {
            if(Entity.States.HasState(Stunned.Instance)) {
                ChangeState(State.Idle);
            }
            
            // TODO: Handle damage awareness update.
        }
        
        public override void OnEntityInitialize(Entity entity)
        {
            base.OnEntityInitialize(entity);
            entity.Actions.GetActionsOfType(meleeAttacks);
        }

        public override void OnUpdateParallel(Entity entity)
        {
            base.OnUpdateParallel(entity);
            if(!IsServer || Game.IsQuitting)
                return;
            
            possibleTargets.Clear();
            foreach(EntityTeams team in EntityTypes.GetEnemies(Entity.Team)) {
                Entity.Targeting.GetDistances(possibleTargets, team);
            }
            if(possibleTargets.Count == 0) {
                nextTarget = null;
                return;
            }

            EntityDistance[] arr          = possibleTargets.Array;
            float highestRating           = -50_000f;
            EntityDistance? highestTarget = null;
            for(int i = 0; i < possibleTargets.Count; i++) {
                EntityDistance entityDist = arr[i];
                Entity targetEntity = entityDist.Entity;
                if(targetEntity.IsDead || !IsTargetReachable(targetEntity))
                    continue;
                
                float rating = 1000;
                rating      -= entityDist.Distance * 5.0f;
                rating      -= entityDist.Entity.Targeting.TargetedLevel;
                if(Entity.Targeting.CurrentTarget == entityDist.Entity) {
                    rating += 50;
                }
                if(rating <= highestRating)
                    continue;
                
                highestTarget = entityDist;
                highestRating = rating;
            }
            nextTarget = highestTarget?.Entity;
        }
        
        public override void LoopLateUpdate()
        {
            base.LoopLateUpdate();
        }

        protected override void ProcessAI()
        {
            EntityTargeting targeting = Entity.Targeting;
            if(nextTarget != targeting.CurrentTarget) {
                TargetChanged();
            }

            if(Entity.States.HasState(Stunned.Instance))
                return;
            
            Entity curTarget = targeting.CurrentTarget;
            switch(state) {
                case State.Idle: {
                    if(Entity.Stagger.IsStaggered)
                        return;

                    if(curTarget != null) {
                        ChangeState(State.Chase);
                        break;
                    }
                } break;
                case State.Chase: {
                    if(curTarget == null) {
                        ChangeState(State.Idle);
                        break;
                    }

                    Entity target = targeting.CurrentTarget;
                    if(!ReachableTargets.TryGetValue(target, out NavMeshPath reachableTarget))
                        return;
                    
                    NavAgent.SetPath(reachableTarget);
                    GetAttacksInRange();
                    if(attacksInRange.Count > 0) {
                        ChangeState(State.Attack);
                    }
                } break;
                case State.Attack: {
                    if(curTarget == null) {
                        ChangeState(State.Idle);
                        break;
                    }
                } break;
                
                default:
                    Debug.LogError("Invalid AI state.");
                    break;
            }
        }

        private void ChangeState(State newState)
        {
            switch(newState) {
                case State.Idle: {
                    curMeleeAttack     = null;
                    NavAgent.isStopped = true;
                    NavAgent.ResetPath();
                    anim.SetMoveSpeed(0);
                } break;
                case State.Chase: {
                    curMeleeAttack     = null;
                    NavAgent.isStopped = false;
                    anim.SetMoveSpeed(2);
                } break;
                case State.Attack: {
                    NavAgent.isStopped = true;
                    curMeleeAttack     = attacksInRange.Array[Random.Range(0, attacksInRange.Count)];
                    NavAgent.ResetPath();
                    anim.SetMoveSpeed(curMeleeAttack.AnimMoveSpeed);
                    Entity.Actions.InvokeAction(curMeleeAttack, onComplete: onAttackCompletedDelegate);
                    Entity.Actions.InvokeActionClientRpc(curMeleeAttack);
                } break;
                
                default:
                    Debug.LogError("Attempted to change to an invalid AI state.");
                    break;
            }
            state = newState;
        }

        private void OnAttackCompleted(InterruptionSource interrupt)
        {
            if(interrupt != InterruptionSource.None) 
                return;

            ChangeState(Entity.Targeting.CurrentTarget != null ? State.Chase : State.Idle);
        }

        private void TargetChanged()
        {
            Entity.Targeting.ChangeTarget(nextTarget);
        }

        private void GetAttacksInRange()
        {
            attacksInRange.Clear();
            EntityTargeting targeting = Entity.Targeting;
            Entity curTarget          = targeting.CurrentTarget;
            if(ReferenceEquals(curTarget, null))
                return;

            Vector3 pos       = CTransform.position;
            Vector3 targetPos = curTarget.CTransform.position;
            Vector3 posA      = new(targetPos.x, 0.0f, targetPos.z);
            Vector3 posB      = new(pos.x, 0.0f, pos.z);
            Vector3 dirToDest = (posA - posB).normalized;
            float angle       = Vector3.Angle(CTransform.forward, dirToDest);
            float distance    = targeting.GetDistance(curTarget);
            for(int i = 0; i < meleeAttacks.Count; i++) {
                MonsterMeleeAttack attack = meleeAttacks.Array[i];
                if(angle <= attack.Angle && distance >= attack.DistanceMin && distance <= attack.DistanceMax) {
                    attacksInRange.Add(attack);
                }
            }
        }

        public override void OnDamage(Entity entity, bool fromRpc, in DamageValue val)
        {
            base.OnDamage(entity, fromRpc, val);
        }

        public override void OnStagger(Entity entity, StaggerStrength strength, Entity instigator)
        {
            base.OnStagger(entity, strength, instigator);
            if(!IsServer)
                return;
            
            ChangeState(State.Idle);
        }

        public override void OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            base.OnDie(entity, lastDamageVal);
            if(!IsServer)
                return;

            ChangeState(State.Idle);
            NavAgent.enabled = false;
        }

        public override void OnRetrieve()
        {
            base.OnRetrieve();
            NavAgent.enabled = true;
        }

        protected override void HandleAttackMovement()
        {
            base.HandleAttackMovement();
        }

        protected override void HandleClosePivot()
        {
            base.HandleClosePivot();
        }

        public override void OnReturn(bool initialization)
        {
            base.OnReturn(initialization);
            nextTarget = null;
            state      = State.Idle;
        }

        private enum State
        {
            Idle,
            Chase,
            Attack
        }
    }

}
