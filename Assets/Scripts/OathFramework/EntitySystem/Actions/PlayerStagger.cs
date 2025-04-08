using Cysharp.Threading.Tasks;
using OathFramework.EntitySystem.Players;
using OathFramework.Utility;
using System.Threading;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public class PlayerStagger : Stagger
    {
        private PlayerModel Model           => (PlayerModel)Entity.EntityModel;
        private PlayerController Controller => (PlayerController)Entity.Controller;
        private bool IsReloading            => Controller.Equipment.IsReloading;

        [field: Space(5)]
       
        [field: SerializeField] public float AimIKRatio                { get; private set; } = 0.7f;
        [field: SerializeField] public AnimationCurve AimIKCurve      { get; private set; }
        [field: SerializeField] public AnimationCurve MoveSpeedDampen { get; private set; }
        [field: SerializeField] public AnimationCurve AimSpeedDampen  { get; private set; }

        private ExtValue<AimIKGoalParams> aimIKVal = new(9);
        private ExtValue<float> movementDampenVal  = new(20);
        private ExtValue<float> aimDampenVal       = new(20);
        
        protected override void OnStart(CancellationToken ct)
        {
            _ = ExecuteTask(ct);
            if(!IsOwner)
                return;

            Controller.ActionBlocks.Add(ActionBlockers.Stagger);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            DirectionHash = Animator.StringToHash(AnimatorDirection);
        }

        protected override void OnTick(float deltaTime)
        {
            base.OnTick(deltaTime);
            aimIKVal.Value = new AimIKGoalParams(AimIKCurve.Evaluate(CurTime / AdjWaitTime));
            if(!IsOwner)
                return;
            
            movementDampenVal.Value = MoveSpeedDampen.Evaluate(CurTime / AdjWaitTime);
            aimDampenVal.Value = AimSpeedDampen.Evaluate(CurTime / AdjWaitTime);
            if(movementDampenVal.Value <= 0.01f) {
                Controller.MovementDampen.Remove(movementDampenVal);
            } 
            if(aimDampenVal.Value <= 0.01f) {
                Controller.AimDampen.Remove(aimDampenVal);
            }
            if(CurTime > UncontrollableTime * AdjWaitTime) {
                Controller.ActionBlocks.Remove(ActionBlockers.Stagger);
            }
        }

        protected override void HandleKnockBack(Vector3 velocity)
        {
            Controller.Movement.Move(velocity * Time.deltaTime);
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            Model.AimIKGoal.Remove(aimIKVal);
            if(!IsOwner)
                return;

            Controller.ActionBlocks.Remove(ActionBlockers.Stagger);
        }
        
        private async UniTask ExecuteTask(CancellationToken ct)
        {
            Animator.SetTrigger(NameHash);
            Animator.SetInteger(IndexHash, AnimIndex);
            Model.AimIKGoal.Add(aimIKVal);
            Controller.MovementDampen.Add(movementDampenVal);
            Controller.AimDampen.Add(aimDampenVal);
            Entity.Animation.ResetTriggers("stagger");
            if(await ShouldExit(UniTask.WaitForSeconds(AdjWaitTime * AimIKRatio, cancellationToken: ct)))
                return;
            
            Model.AimIKGoal.Remove(aimIKVal);
            if(await ShouldExit(UniTask.WaitForSeconds(AdjWaitTime * (1.0f - AimIKRatio), cancellationToken: ct)))
                return;
            
            if(Interruption == InterruptionSource.None) {
                Completed();
            }
        }
    }
}
