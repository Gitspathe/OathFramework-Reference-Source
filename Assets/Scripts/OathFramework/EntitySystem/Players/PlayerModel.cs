using Cysharp.Threading.Tasks;
using UnityEngine;
using RootMotion.FinalIK;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.EquipmentSystem;
using OathFramework.Utility;
using System.Threading;

namespace OathFramework.EntitySystem.Players
{

    [RequireComponent(typeof(Animator), typeof(BipedIK), typeof(ModelRecoilHandler))]
    public class PlayerModel : EntityModel, 
        ILoopUpdate, IEntityModelEquipment
    {
        [field: Header("General")]
        [field: SerializeField] public string AnimSetParam          { get; private set; } = "AnimSet";
        [field: SerializeField] public string AnimMoveXParam        { get; private set; } = "MovementX";
        [field: SerializeField] public string AnimMoveYParam        { get; private set; } = "MovementY";
        [field: SerializeField] public string AnimReloadSpeedParam  { get; private set; } = "ReloadSpeedMult";
        [field: SerializeField] public string AnimReloadParam       { get; private set; } = "Reload";
        [field: SerializeField] public string AnimFinishReloadParam { get; private set; } = "FinishReload";
        [field: SerializeField] public string AnimAimParam          { get; private set; }
        
        [field: Header("Throwing")]
        [field: SerializeField] public Transform ThrowOffsetTransform { get; private set; }
        [field: SerializeField] public string AnimThrowParam          { get; private set; } = "Throw";

        private PlayerController controller;
        private Transform aimTarget;
        private float aimIKLerpWeightGoal;
        private float aimIKLerpSpeed;
        private float curAimIKDelay;
        private float curHeadIKSuppressTime;
        private int animSetHash;
        private int animMoveXHash;
        private int animMoveYHash;
        private int animReloadSpeedHash;
        private int animReloadHash;
        private int animFinishReloadHash;
        private int animAimHash;
        private int animThrowHash;
        private CancellationTokenSource restoreAimIkCts;
        private ExtValue<AimIKGoalParams> suppressGoalParamsSwap;
        private ExtValue<AimIKGoalParams> suppressGoalParamsReload;

        public ExtValue<AimIKGoalParams>.Handler AimIKGoal { get; private set; }

        public ModelRecoilHandler RecoilHandler           { get; private set; }
        public EquippableThirdPersonModel EquippableModel { get; private set; }
        public BipedIK BipedIK                            { get; private set; }
        public IKSolverAim AimIK                          => BipedIK.solvers.aim;
        public IKSolverLimb RightHandIK                   => BipedIK.solvers.rightHand;
        public IKSolverLimb LeftHandIK                    => BipedIK.solvers.leftHand;
        
        PreviewTrajectory IEntityModelThrow.TrajectoryArc { get; set; }

        Transform IEntityModelThrow.ThrowOffsetTransform => ThrowOffsetTransform;

        public override bool FootstepSpatialized => controller.Mode == PlayerControllerMode.None;

        protected override void Awake()
        {
            base.Awake();
            BipedIK                  = GetComponent<BipedIK>();
            RecoilHandler            = GetComponent<ModelRecoilHandler>();
            animSetHash              = Animator.StringToHash(AnimSetParam);
            animMoveXHash            = Animator.StringToHash(AnimMoveXParam);
            animMoveYHash            = Animator.StringToHash(AnimMoveYParam);
            animReloadSpeedHash      = Animator.StringToHash(AnimReloadSpeedParam);
            animReloadHash           = Animator.StringToHash(AnimReloadParam);
            animFinishReloadHash     = Animator.StringToHash(AnimFinishReloadParam);
            animAimHash              = Animator.StringToHash(AnimAimParam);
            animThrowHash            = Animator.StringToHash(AnimThrowParam);
            AimIKGoal                = new ExtValue<AimIKGoalParams>.Handler(new AimIKGoalParams(1.0f, 10.0f));
            suppressGoalParamsSwap   = new ExtValue<AimIKGoalParams>(10, new AimIKGoalParams(0.0f, 25.0f));
            suppressGoalParamsReload = new ExtValue<AimIKGoalParams>(100, new AimIKGoalParams(0.0f, 25.0f));
        }
        
        public void InitPlayerModel(PlayerController controller)
        {
            this.controller = controller;
            aimTarget       = controller.AimTarget;
            AimIK.target    = aimTarget;
            SetupEquippedWeapon(null, 0.0f);
            base.OnEntityInitialize(controller.Entity);
        }

        public void LoopUpdate()
        {
            if(!BipedIK.enabled) {
                AimIK.IKPositionWeight       = 0.0f;
                RightHandIK.IKPositionWeight = 0.0f;
                RightHandIK.IKRotationWeight = 0.0f;
                LeftHandIK.IKPositionWeight  = 0.0f;
                LeftHandIK.IKRotationWeight  = 0.0f;
                return;
            }
            
            HandleHeadIK();
            
            // Smoothen IK when weapon is taken out, to prevent weird animation errors.
            curAimIKDelay -= Time.deltaTime;
            if(curAimIKDelay > 0.0f)
                return;

            UpdateIKSolver(AimIK);
            UpdateIKSolver(RightHandIK, true);
            UpdateIKSolver(LeftHandIK, true);
        }

        private void HandleHeadIK()
        {
            // Assuming 3 bones for the spine, with index 1 and 2 being closer to the head.
            if(curHeadIKSuppressTime > 0.0f) {
                AimIK.bones[1].weight = Mathf.Lerp(AimIK.bones[1].weight, 0.0f, 15.0f * Time.deltaTime);
                AimIK.bones[2].weight = Mathf.Lerp(AimIK.bones[2].weight, 0.0f, 15.0f * Time.deltaTime);
                curHeadIKSuppressTime -= Time.deltaTime;
            } else {
                AimIK.bones[1].weight = Mathf.Lerp(AimIK.bones[1].weight, 1.0f, 5.0f * Time.deltaTime);
                AimIK.bones[2].weight = Mathf.Lerp(AimIK.bones[2].weight, 1.0f, 5.0f * Time.deltaTime);
            }
        }

        private void UpdateIKSolver(IKSolverTrigonometric solver, bool updateRotation)
        {
            if(solver.target == null) {
                solver.IKPositionWeight = 0.0f;
                if(updateRotation) {
                    solver.IKRotationWeight = 0.0f;
                }
                return;
            }
            float curWeight  = solver.IKPositionWeight;
            float goal       = AimIKGoal.Value.Weight;
            float nextWeight = AimIKGoal.Value.IsInstant 
                ? AimIKGoal.Value.Weight 
                : Mathf.Lerp(curWeight, goal, AimIKGoal.Value.LerpSpeed * Time.deltaTime);
            
            solver.IKPositionWeight = nextWeight;
            if(updateRotation) {
                solver.IKRotationWeight = nextWeight;
            }
        }

        private void UpdateIKSolver(IKSolverHeuristic solver)
        {
            if(solver is IKSolverAim aim && aim.transform == null) {
                solver.IKPositionWeight = 0.0f;
                return;
            }
            
            float curWeight  = solver.IKPositionWeight;
            float goal       = solver.target == null ? 0.0f : AimIKGoal.Value.Weight;
            float nextWeight = AimIKGoal.Value.IsInstant 
                ? AimIKGoal.Value.Weight 
                : Mathf.Lerp(curWeight, goal, AimIKGoal.Value.LerpSpeed * Time.deltaTime);
            
            solver.IKPositionWeight = nextWeight;
        }
        
        public void SuppressHeadIK(float duration)
        {
            curHeadIKSuppressTime = duration;
        }
        
        public void ChangeEquippableAnimationSet(EquippableAnimSets animSet)
        {
            Animator.ResetTrigger(animThrowHash);
            Animator.SetInteger(animSetHash, (int)animSet);
        }
        
        public void SetupHolsteredModel(EquippableHolsteredModel model)
        {
            // ?
        }

        public void SetupEquippedModel(EquippableThirdPersonModel model)
        {
            EquippableModel = model;
        }

        public void SetupEquippedWeapon(EquippableThirdPersonModel weaponModel, float suppressIKTime, bool updateAimIK = true)
        {
            EquippableModel = weaponModel;
            if(weaponModel == null) {
                BipedIK.enabled            = false;
                BipedIK.solvers.aim.target = null;
                ChangeEquippableAnimationSet(EquippableAnimSets.Unarmed);
                return;
            }

            BipedIK.enabled    = true;
            AimIK.transform    = weaponModel.AimStartPoint;
            AimIK.target       = aimTarget;
            RightHandIK.target = weaponModel.RightHandGoal;
            LeftHandIK.target  = weaponModel.LeftHandGoal;
            if(updateAimIK) {
                restoreAimIkCts?.Cancel();
                restoreAimIkCts?.Dispose();
                restoreAimIkCts = new CancellationTokenSource();
                _ = DelayedAimIKReset(restoreAimIkCts.Token, suppressIKTime);
            }
            ChangeEquippableAnimationSet(weaponModel.AnimSet);
        }
        
        private async UniTask DelayedAimIKReset(CancellationToken cancelToken, float duration)
        {
            if(await UniTask.WaitForSeconds(duration, cancellationToken: cancelToken).SuppressCancellationThrow())
                return;
            
            AimIKGoal.Remove(suppressGoalParamsSwap);
        }

        public void AnimPutAway()
        {
            Animator.SetInteger(animSetHash, 0);
            AimIKGoal.Add(suppressGoalParamsSwap);
            SuppressHeadIK(0.4f);
        }

        public void SetFinishReloadAnim(bool val)
        {
            Animator.SetBool(animFinishReloadHash, val);
        }

        public void SetReloadAnim(bool reload, float reloadSpeed)
        {
            Animator.SetFloat(animReloadSpeedHash, reloadSpeed);
            if(reload) {
                AimIKGoal.Add(suppressGoalParamsReload);
            } else {
                AimIKGoal.Remove(suppressGoalParamsReload);
            }
            Animator.SetBool(animReloadHash, reload);
        }

        public void SetMovementAnim(Vector3 movementVec)
        {
            Vector3 inverse = controller.CTransform.InverseTransformDirection(movementVec);
            float curX      = Animator.GetFloat(animMoveXHash);
            float curY      = Animator.GetFloat(animMoveYHash);
            Animator.SetFloat(animMoveXHash, Mathf.Lerp(curX, inverse.x, 10.0f * Time.deltaTime));
            Animator.SetFloat(animMoveYHash, Mathf.Lerp(curY, inverse.z, 10.0f * Time.deltaTime));
        }

        public void ModelReloadAmmo()
        {
            controller.PlayerAnimation.ReloadAmmo();
        }

        public void ModelEndReload()
        {
            controller.PlayerAnimation.EndReload();
        }

        public void SetAimAnim(bool val)
        {
            Animator.SetBool(animAimHash, val);
        }

        public void PlayThrow()
        {
            Animator.SetTrigger(animThrowHash);
        }

        Vector3 IEntityModelThrow.GetThrowStrength() => controller.CurrentThrowForce;

        public void ModelLateEquippableUse()
        {
            controller.Equipment.LateUse();
        }

        public override void OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            base.OnDie(entity, in lastDamageVal);
            Animator.ResetTrigger(AnimThrowParam);
        }
    }

}
