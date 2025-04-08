using Cysharp.Threading.Tasks;
using OathFramework.EntitySystem.Players;
using OathFramework.Utility;
using System.Threading;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public class PlayerDodge : Dodge
    {
        private PlayerModel Model           => (PlayerModel)Entity.EntityModel;
        private PlayerController Controller => (PlayerController)Entity.Controller;
        private bool IsReloading            => Controller.Equipment.IsReloading;
        
        [field: Space(5)]
        
        [field: SerializeField] public float AimRatio                 { get; private set; } = 0.9f;
        [field: SerializeField] public AnimationCurve AimIKCurve      { get; private set; }
        [field: SerializeField] public AnimationCurve MoveSpeedDampen { get; private set; }
        [field: SerializeField] public AnimationCurve AimSpeedDampen  { get; private set; }

        private ExtValue<AimIKGoalParams> aimIKVal = new(9);
        private ExtValue<float> movementDampenVal  = new(10);
        private ExtValue<float> aimDampenVal       = new(10);
        private CancellationTokenSource cts;
        
        protected override void OnStart(CancellationToken ct)
        {
            base.OnStart(ct);
            if(IsOwner) {
                Controller.MovementDampen.Add(movementDampenVal);
                Controller.AimDampen.Add(aimDampenVal);
                Controller.ActionBlocks.Add(ActionBlockers.Dodge);
            }
            cts?.Cancel();
            cts = CancellationTokenSource.CreateLinkedTokenSource(ct, new CancellationToken());
            _ = ExecuteTask(cts.Token);
        }

        protected override void OnTick(float deltaTime)
        {
            base.OnTick(deltaTime);
            float curvePos = CurTime / (Duration * AnimLayerRatio);
            aimIKVal.Value = new AimIKGoalParams(AimIKCurve.Evaluate(curvePos));
            if(!IsOwner)
                return;

            movementDampenVal.Value = MoveSpeedDampen.Evaluate(CurTime / Duration);
            aimDampenVal.Value      = AimSpeedDampen.Evaluate(CurTime / Duration);
            if(movementDampenVal.Value <= 0.01f) {
                Controller.MovementDampen.Remove(movementDampenVal);
            }
            if(aimDampenVal.Value <= 0.01f) {
                Controller.AimDampen.Remove(aimDampenVal);
            }
            if(CurTime > Duration * UncontrollableRatio) {
                Model.AimIKGoal.Remove(aimIKVal);
                Controller.ActionBlocks.Remove(ActionBlockers.Dodge);
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            Model.AimIKGoal.Remove(aimIKVal);
            if(!IsOwner)
                return;
            
            Controller.MovementDampen.Remove(movementDampenVal);
            Controller.AimDampen.Remove(aimDampenVal);
            Controller.ActionBlocks.Remove(ActionBlockers.Dodge);
        }
        
        protected override void HandleMotion(Vector3 velocity)
        {
            Controller.Movement.Move(velocity * Time.deltaTime);
        }
        
        private async UniTask ExecuteTask(CancellationToken ct)
        {
            Animator.SetTrigger(AnimNameHash);
            Animator.SetInteger(IndexHash, AnimIndex);
            Model.AimIKGoal.Add(aimIKVal);
            Entity.Animation.ResetTriggers("dodge");
            if(await ShouldExit(UniTask.WaitForSeconds(Duration * AimRatio, cancellationToken: ct)))
                return;
            
            if(!IsReloading) {
                Model.AimIKGoal.Remove(aimIKVal);
            }
            if(await ShouldExit(UniTask.WaitForSeconds(Duration * (1.0f - AimRatio), cancellationToken: ct)))
                return;
            
            if(Interruption == InterruptionSource.None) {
                Completed();
            }
        }
    }
}
