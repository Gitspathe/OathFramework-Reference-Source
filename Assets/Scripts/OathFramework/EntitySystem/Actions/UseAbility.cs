using OathFramework.AbilitySystem;
using OathFramework.Data.StatParams;
using OathFramework.EquipmentSystem;
using System.Threading;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public abstract class UseAbility : Action
    {
        protected int InterruptHash;
        protected int IndexHash;
        protected int SpeedHash;
        protected int AnimUpperLayerIndex;
        protected int AnimLowerLayerIndex;
        protected float CurTime;
        protected float speedMult;
        protected ActionAnimParams @params;
        protected ItemHandler itemHandler;
        protected AbilityHandler abilityHandler;
        
        public int AnimNameHash { get; private set; }
        
        [field: Header("Animation")]
        
        [field: SerializeField] public string AnimatorUse        { get; private set; } = "Action";
        [field: SerializeField] public string AnimatorInterrupt  { get; private set; } = "ActionInterrupt";
        [field: SerializeField] public string AnimatorSpeed      { get; private set; } = "ActionSpeed";
        [field: SerializeField] public string AnimatorIndex      { get; private set; } = "ActionIndex";
        [field: SerializeField] public string AnimUpperLayerName { get; private set; } = "ActionUpperBody";
        [field: SerializeField] public string AnimLowerLayerName { get; private set; } = "ActionLowerBody";

        public float Duration => @params.BaseDuration / speedMult;

        public Transform CTransform { get; private set; }
        
        protected Animator Animator => Entity.EntityModel.Animator;

        public virtual void SetParams(ItemHandler handler, ActionAnimParams @params, float speedMult = 1.0f)
        {
            CurTime        = 0.001f;
            itemHandler    = handler;
            abilityHandler = null;
            SetParamsInternal(@params, speedMult);
        }
        
        public virtual void SetParams(AbilityHandler handler, ActionAnimParams @params, float speedMult = 1.0f)
        {
            CurTime        = 0.001f;
            abilityHandler = handler;
            itemHandler    = null;
            SetParamsInternal(@params, speedMult);
        }

        protected virtual void SetParamsInternal(ActionAnimParams @params, float speedMult = 1.0f)
        {
            this.@params   = @params;
            this.speedMult = 1.0f * speedMult;
            if(this.@params.ApplyModifiers) {
                this.speedMult *= Entity.CurStats.GetParam(ItemUseSpeedMult.Instance);
            }
            this.speedMult = Mathf.Clamp(this.speedMult, 0.01f, 10.0f);
            Animator.SetFloat(SpeedHash, @params.AnimDuration / Duration);
            if(@params.LerpUpperAnimLayer) {
                AnimUpperLayerIndex = Animator.GetLayerIndex(AnimUpperLayerName);
            }
            if(@params.LerpLowerAnimLayer) {
                AnimLowerLayerIndex = Animator.GetLayerIndex(AnimLowerLayerName);
            }
        }
        
        protected override void OnInitialize()
        {
            CTransform    = Entity.transform;
            AnimNameHash  = Animator.StringToHash(AnimatorUse);
            InterruptHash = Animator.StringToHash(AnimatorInterrupt);
            IndexHash     = Animator.StringToHash(AnimatorIndex);
            SpeedHash     = Animator.StringToHash(AnimatorSpeed);
        }

        protected override void OnStart(CancellationToken ct)
        {
            
        }

        protected override void OnTick(float deltaTime)
        {
            CurTime    += deltaTime;
            float ratio = CurTime / Duration;
            if(@params.LerpUpperAnimLayer) {
                float curvePos = CurTime / (Duration * @params.AnimUpperLayerRatio);
                float goal     = ratio > @params.AnimUpperLayerRatio ? 0.0f : @params.AnimUpperLayerCurve.Evaluate(curvePos);
                Animator.SetLayerWeight(AnimUpperLayerIndex, goal);
            } else {
                Animator.SetLayerWeight(AnimUpperLayerIndex, 0.0f);
            }
            
            if(@params.LerpLowerAnimLayer) {
                float curvePos = CurTime / (Duration * @params.AnimLowerLayerRatio);
                float goal     = ratio > @params.AnimLowerLayerRatio ? 0.0f : @params.AnimLowerLayerCurve.Evaluate(curvePos);
                Animator.SetLayerWeight(AnimLowerLayerIndex, goal);
            } else {
                Animator.SetLayerWeight(AnimLowerLayerIndex, 0.0f);
            }
        }

        protected override void OnEnd()
        {
#if UNITY_EDITOR
            if(abilityHandler == null && itemHandler == null)
                return;
#endif
            
            if(Interruption != InterruptionSource.None) {
                Animator.SetTrigger(InterruptHash);
            }
            if(!ReferenceEquals(itemHandler, null)) {
                itemHandler.EndItemUse();
            }
            if(!ReferenceEquals(abilityHandler, null)) {
                abilityHandler.EndQueuedAbility();
            }
            if(ReferenceEquals(@params, null))
                return;
            
            Animator.SetLayerWeight(AnimUpperLayerIndex, 0.0f);
            Animator.SetLayerWeight(AnimLowerLayerIndex, 0.0f);
        }
    }
}
