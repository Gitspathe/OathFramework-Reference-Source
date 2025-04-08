using Cysharp.Threading.Tasks;
using OathFramework.EntitySystem.Players;
using OathFramework.Utility;
using System.Threading;

namespace OathFramework.EntitySystem.Actions
{
    public class PlayerUseAbility : UseAbility
    {
        private PlayerModel Model           => (PlayerModel)Entity.EntityModel;
        private PlayerController Controller => (PlayerController)Entity.Controller;
        private bool IsReloading            => Controller.Equipment.IsReloading;

        private ExtValue<AimIKGoalParams> aimIKVal = new(15);
        private ExtValue<float> movementDampen     = new(9);
        private ExtValue<float> aimDampen          = new(9);
        private ExtBool hideWeapon                 = new(order: 1);
        private CancellationTokenSource cts;
        
        protected override void OnStart(CancellationToken ct)
        {
            base.OnStart(ct);
            if(IsOwner) {
                if(@params.DoMovementSpeedDampen) {
                    Controller.MovementDampen.Add(movementDampen);
                }
                if(@params.DoAimSpeedDampen) {
                    Controller.AimDampen.Add(aimDampen);
                }
                HandleWeaponHiding(true);
                Controller.ActionBlocks.Add(ActionBlockers.AbilityUse);
            }
            
            cts?.Cancel();
            if(ReferenceEquals(@params, null)) {
                Completed();
                return;
            }
            cts = CancellationTokenSource.CreateLinkedTokenSource(ct, new CancellationToken());
            _ = ExecuteTask(cts.Token);
        }

        protected override void OnTick(float deltaTime)
        {
            base.OnTick(deltaTime);
            float aimCurvePos = CurTime / (Duration * @params.AimRatio);
            aimIKVal.Value    = new AimIKGoalParams(@params.AimIKCurve.Evaluate(aimCurvePos));
            if(!IsOwner || ReferenceEquals(@params, null))
                return;

            if(@params.DoMovementSpeedDampen) {
                movementDampen.Value = @params.MovementDampen.Evaluate(CurTime / Duration);
            }
            if(@params.DoAimSpeedDampen) {
                aimDampen.Value = @params.AimSpeedDampen.Evaluate(CurTime / Duration);
            }
            if(CurTime > Duration * @params.UncontrollableRatio) {
                Controller.MovementDampen.Remove(movementDampen);
                Controller.AimDampen.Remove(aimDampen);
                Controller.ActionBlocks.Remove(ActionBlockers.AbilityUse);
                HandleWeaponHiding(false);
                if(@params.DoAimSpeedDampen && @params.ExtraAimSmoothenTime > 0.001f) {
                    Controller.AddAimSmoothen(@params.ExtraAimSmoothenTime);
                }
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            Model.AimIKGoal.Remove(aimIKVal);
            if(!IsOwner)
                return;
            
            Controller.MovementDampen.Remove(movementDampen);
            Controller.AimDampen.Remove(aimDampen);
            Controller.ActionBlocks.Remove(ActionBlockers.AbilityUse);
            HandleWeaponHiding(false);
        }
        
        private async UniTask ExecuteTask(CancellationToken ct)
        {
            Animator.SetTrigger(AnimNameHash);
            Animator.SetInteger(IndexHash, @params.AnimIndex);
            Model.AimIKGoal.Add(aimIKVal);
            Entity.Animation.ResetTriggers("action");
            if(await ShouldExit(UniTask.WaitForSeconds(Duration * @params.AimRatio, cancellationToken: ct))) 
                return;
            
            if(!IsReloading) {
                Model.AimIKGoal.Remove(aimIKVal);
            }
            if(await ShouldExit(UniTask.WaitForSeconds(Duration * (1.0f - @params.AimRatio), cancellationToken: ct)))
                return;
            
            if(Interruption == InterruptionSource.None) {
                Completed();
            }
        }

        private void HandleWeaponHiding(bool val)
        {
            if(!enabled || !@params.HideWeapon || Controller.PlayerModel.EquippableModel == null)
                return;

            if(val) {
                Controller.PlayerModel.EquippableModel.IsVisible.Add(hideWeapon);
            } else {
                Controller.PlayerModel.EquippableModel.IsVisible.Remove(hideWeapon);
            }
        }
    }
}
