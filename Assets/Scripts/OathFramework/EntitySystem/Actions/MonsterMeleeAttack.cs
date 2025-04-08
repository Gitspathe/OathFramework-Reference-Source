using Cysharp.Threading.Tasks;
using OathFramework.Data.StatParams;
using System.Threading;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public class MonsterMeleeAttack : MeleeAttack
    {
        [field: Header("AI Settings")]
        
        [field: SerializeField] public float DistanceMin       { get; private set; } = 0.0f;
        [field: SerializeField] public float DistanceMax       { get; private set; } = 2.5f;
        [field: SerializeField] public float Angle             { get; private set; } = 30.0f;
        [field: SerializeField] public float PreferredAngle    { get; private set; } = 10.0f;
        [field: SerializeField] public float AutoBrakeDistance { get; private set; } = 0.5f;
        [field: SerializeField] public bool AutoBrake          { get; private set; } = true;
        
        [field: Space(5)]
        
        [field: SerializeField] public AnimationCurve BrakeCurve { get; private set; }
        [field: SerializeField] public AnimationCurve PivotSpeed { get; private set; }
        [field: SerializeField] public AnimationCurve MoveSpeed  { get; private set; }
        [field: SerializeField] public int AnimMoveSpeed         { get; private set; } = 1;
        
        public float CurPivotSpeed => PivotSpeed.Evaluate(Progress) * Entity.CurStats.turnSpeed;
        public float CurMoveSpeed  => MoveSpeed.Evaluate(Progress) * Entity.CurStats.speed;
        
        public float GetBrakeMult(float distance)
        {
            float minDist = AutoBrakeDistance;
            float maxDist = DistanceMax;
            float ratio   = Mathf.Clamp((distance - minDist) * (maxDist - minDist), 0.0f, 1.0f);
            return 1.0f - BrakeCurve.Evaluate(Mathf.Lerp(0.0f, 1.0f, ratio));
        }

        protected override void OnStart(CancellationToken ct)
        {
            Progress = 0.0f;
            _ = ExecuteTask(ct);
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            if(IsOwner) { // todo fix bug
                Animator.ResetTrigger(NameHash);
            }
        }

        private async UniTask ExecuteTask(CancellationToken ct)
        {
            Entity.Animation.ResetTriggers("attack");
            Animator.SetInteger(IndexHash, AttackAnimIndex);
            Animator.SetTrigger(NameHash);
            EffectBoxController effectBoxController = Entity.EntityModel.EfxBoxController;
            if(EffectBoxes != null && EffectBoxes.Length > 0) {
                float baseDam    = Entity.CurStats.GetParam(BaseDamage.Instance);
                ushort dmgAmount = (ushort)Mathf.Clamp(baseDam * DamageMult, 0.0f, ushort.MaxValue - 1.0f);
                DamageValue damageVal = new(
                    dmgAmount,
                    transform.position,
                    DamageSource.Melee,
                    Stagger,
                    StaggerAmount,
                    DamageFlags.HasInstigator, 
                    Entity
                );
                effectBoxController.SetupEffectBoxes(
                    Entity, 
                    EffectBoxes,
                    damageVal,
                    EntityTypes.GetEnemies(Entity.Team), 
                    true,
                    HasUnion ? Union : null
                );
            }
            if(await ShouldExit(UniTask.WaitForSeconds(Duration, cancellationToken: ct)))
                return;
            
            if(Interruption == InterruptionSource.None) {
                Completed();
            }
        }
    }
}
