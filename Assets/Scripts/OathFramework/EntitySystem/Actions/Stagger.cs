using Cysharp.Threading.Tasks;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public abstract class Stagger : Action
    {
        protected int NameHash;
        protected int IndexHash;
        protected int SpeedHash;
        protected int DirectionHash;
        protected int AnimLayerIndex;
        protected float CurTime;
        protected float AdjWaitTime;
        protected Vector3 HitPos;
        protected DamageValue LastDamageVal;
        
        [field: Header("Stagger Params")]

        [field: SerializeField] public int AnimIndex            { get; private set; }
        [field: SerializeField] public float WaitTime           { get; private set; }
        [field: SerializeField] public float UncontrollableTime { get; private set; }
        [field: SerializeField] public string AnimatorStagger   { get; private set; } = "Stagger";
        [field: SerializeField] public string AnimatorSpeed     { get; private set; } = "StaggerSpeed";
        [field: SerializeField] public string AnimatorIndex     { get; private set; } = "StaggerIndex";
        [field: SerializeField] public string AnimatorDirection { get; private set; } = "StaggerDirection";

        [field: Space(5)]
        
        [field: SerializeField] public bool LerpAnimLayer            { get; private set; }
        [field: SerializeField] public string AnimLayerName          { get; private set; }
        [field: SerializeField] public float AnimLayerRatio          { get; private set; } = 0.9f;

        [field: SerializeField] public AnimationCurve AnimLayerCurve { get; private set; }
        
        [field: Space(5)]
        
        [field: SerializeField] public float KnockBackSpeed     { get; private set; } = 2.0f;
        [field: SerializeField] public AnimationCurve KnockBack { get; private set; }
        
        public Transform CTransform { get; private set; }
        
        protected Animator Animator => Entity.EntityModel.Animator;
        
        public virtual void SetParams(float waitTime, Entity instigator)
        {
            AdjWaitTime   = waitTime;
            CurTime       = 0.001f;
            Animator.SetFloat(SpeedHash, WaitTime / AdjWaitTime);
            if(LerpAnimLayer) {
                AnimLayerIndex = Animator.GetLayerIndex(AnimLayerName);
            }

            HitPos = Vector3.zero;
            if(!ReferenceEquals(instigator, null)) {
                Vector3 instigatorPos = instigator.transform.position;
                HitPos = new Vector3(instigatorPos.x, 0.0f, instigatorPos.z);
            }
            
            HitDirection direction;
            if(TestRelativeSide(HitPos, transform.forward)) {
                direction = HitDirection.Forward;
            } else if(TestRelativeSide(HitPos, transform.right)) {
                direction = HitDirection.Right;
            } else if(TestRelativeSide(HitPos, -transform.forward)) {
                direction = HitDirection.Back;
            } else {
                direction = HitDirection.Left;
            }
            
            Animator.SetInteger(DirectionHash, (int)direction);
        }

        protected override void OnTick(float deltaTime)
        {
            CurTime += deltaTime;
            float ratio  = CurTime / AdjWaitTime;
            Vector3 cPos = CTransform.position;
            Vector3 posA = new(HitPos.x, 0.0f, HitPos.z);
            Vector3 posB = new(cPos.x, 0.0f, cPos.z);
            Vector3 vec  = (posB - posA).normalized;
            HandleKnockBack(vec * (KnockBackSpeed * KnockBack.Evaluate(ratio)));
            if(!LerpAnimLayer)
                return;
            
            float curvePos = CurTime / (AdjWaitTime * AnimLayerRatio);
            float goal     = ratio > AnimLayerRatio ? 0.0f : AnimLayerCurve.Evaluate(curvePos);
            Animator.SetLayerWeight(AnimLayerIndex, goal);
        }

        private bool TestRelativeSide(Vector3 position, Vector3 test)
        {
            Vector3 cPos  = CTransform.position;
            Vector3 myPos = new(cPos.x, 0.0f, cPos.z);
            Vector3 vec   = (position - myPos).normalized;
            float ang     = Mathf.Acos(Vector3.Dot(test, vec)) * Mathf.Rad2Deg;
            return ang <= 45.0f;
        }
        
        protected abstract void HandleKnockBack(Vector3 velocity);

        protected override void OnEnd()
        {
            if(!LerpAnimLayer)
                return;
            
            Animator.SetLayerWeight(AnimLayerIndex, 0.0f);
        }

        protected override void OnInitialize()
        {
            CTransform    = Entity.transform;
            NameHash      = Animator.StringToHash(AnimatorStagger);
            IndexHash     = Animator.StringToHash(AnimatorIndex);
            SpeedHash     = Animator.StringToHash(AnimatorSpeed);
            DirectionHash = Animator.StringToHash(AnimatorDirection);
        }
    }
}
