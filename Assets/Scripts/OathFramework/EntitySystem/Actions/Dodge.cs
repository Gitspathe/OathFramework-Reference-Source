using OathFramework.Data.StatParams;
using OathFramework.Utility;
using System.Threading;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public abstract class Dodge : Action
    {
        private QList<HitBox> tmpHitBoxes = new();
        private bool isInvincible;
        
        protected int IndexHash;
        protected int SpeedHash;
        protected int DirectionHash;
        protected int AnimLayerIndex;
        protected float CurTime;
        protected Vector3 CurDirection;
        
        public int AnimNameHash { get; private set; }
        
        [field: Header("Mechanical Params")]
        
        [field: SerializeField] public int IFrames               { get; private set; } = 30;
        [field: SerializeField] public int DurationFrames        { get; private set; } = 60;
        [field: SerializeField] public float UncontrollableRatio { get; private set; } = 0.9f;
        [field: SerializeField] public bool ApplyModifiers       { get; private set; } = true;
        [field: SerializeField] public bool AffectMelee          { get; private set; } = true;
        [field: SerializeField] public bool AffectProjectile     { get; private set; } = true;
        [field: SerializeField] public HitBoxGroups HitBoxGroups { get; private set; } = HitBoxGroups.Group1;

        [field: Space(10)]
        
        [field: SerializeField] public float Speed               { get; private set; } = 5.0f;
        [field: SerializeField] public AnimationCurve Motion     { get; private set; }
        
        [field: Header("Animation")]
        
        [field: SerializeField] public int AnimIndex            { get; private set; }
        [field: SerializeField] public float AnimDuration       { get; private set; } = 1.0f;
        [field: SerializeField] public string AnimatorDodge     { get; private set; } = "Dodge";
        [field: SerializeField] public string AnimatorSpeed     { get; private set; } = "DodgeSpeed";
        [field: SerializeField] public string AnimatorIndex     { get; private set; } = "DodgeIndex";
        [field: SerializeField] public string AnimatorDirection { get; private set; } = "DodgeDirection";
        
        [field: Space(5)]
        
        [field: SerializeField] public bool LerpAnimLayer            { get; private set; }
        [field: SerializeField] public string AnimLayerName          { get; private set; }
        [field: SerializeField] public float AnimLayerRatio          { get; private set; } = 0.9f;
        [field: SerializeField] public AnimationCurve AnimLayerCurve { get; private set; }

        public float InvincibleDuration {
            get {
                if(!ApplyModifiers)
                    return IFrames / 60.0f;
                
                return ((IFrames * Entity.CurStats.GetParam(DodgeIFramesMult.Instance)) 
                        + Entity.CurStats.GetParam(DodgeIFrames.Instance)) / 60.0f;
            }
        }

        public float Duration {
            get {
                if(!ApplyModifiers)
                    return DurationFrames / 60.0f;
                
                return (DurationFrames * Entity.CurStats.GetParam(DodgeDurationMult.Instance)) / 60.0f;
            }
        }
        
        public Transform CTransform { get; private set; }
        
        protected Animator Animator => Entity.EntityModel.Animator;

        public virtual void SetParams(Vector3 direction)
        {
            CurTime      = 0.001f;
            CurDirection = direction;
            Animator.SetFloat(SpeedHash, AnimDuration / Duration);
            if(LerpAnimLayer) {
                AnimLayerIndex = Animator.GetLayerIndex(AnimLayerName);
            }
            
            DodgeAnimDirection animDir;
            if(TestRelativeSide(direction, transform.forward)) {
                animDir = DodgeAnimDirection.Forward;
            } else if(TestRelativeSide(direction, transform.right)) {
                animDir = DodgeAnimDirection.Right;
            } else if(TestRelativeSide(direction, -transform.right)) {
                animDir = DodgeAnimDirection.Left;
            } else {
                animDir = DodgeAnimDirection.Back;
            }
            Animator.SetInteger(DirectionHash, (int)animDir);
        }
        
        protected override void OnInitialize()
        {
            CTransform    = Entity.transform;
            AnimNameHash  = Animator.StringToHash(AnimatorDodge);
            IndexHash     = Animator.StringToHash(AnimatorIndex);
            SpeedHash     = Animator.StringToHash(AnimatorSpeed);
            DirectionHash = Animator.StringToHash(AnimatorDirection);
        }

        protected override void OnStart(CancellationToken ct)
        {
            Entity.EntityModel.GetHitBoxes(HitBoxGroups, tmpHitBoxes);
            int count = tmpHitBoxes.Count;
            for(int i = 0; i < count; i++) {
                HitBox hitBox = tmpHitBoxes.Array[i];
                if(AffectMelee) {
                    hitBox.IgnoreMelee = true;
                }
                if(AffectProjectile) {
                    hitBox.IgnoreProjectile = true;
                }
            }
            isInvincible = true;
            tmpHitBoxes.Clear();
        }

        protected override void OnTick(float deltaTime)
        {
            CurTime    += deltaTime;
            float ratio = CurTime / Duration;
            if(IsOwner) {
                float speed = ApplyModifiers ? Speed * Entity.CurStats.GetParam(DodgeSpeedMult.Instance) : Speed;
                HandleMotion(CurDirection * (speed * Motion.Evaluate(ratio)));
            }

            if(isInvincible && CurTime > InvincibleDuration) {
                Entity.EntityModel.GetHitBoxes(HitBoxGroups, tmpHitBoxes);
                int count = tmpHitBoxes.Count;
                for(int i = 0; i < count; i++) {
                    tmpHitBoxes.Array[i].RestoreFlags();
                }
                tmpHitBoxes.Clear();
                isInvincible = false;
            }
            if(!LerpAnimLayer)
                return;
            
            float curvePos = CurTime / (Duration * AnimLayerRatio);
            float goal     = ratio > AnimLayerRatio ? 0.0f : AnimLayerCurve.Evaluate(curvePos);
            Animator.SetLayerWeight(AnimLayerIndex, goal);
        }

        protected override void OnEnd()
        {
            Entity.EntityModel.GetHitBoxes(HitBoxGroups, tmpHitBoxes);
            isInvincible = false;
            int count    = tmpHitBoxes.Count;
            for(int i = 0; i < count; i++) {
                tmpHitBoxes.Array[i].RestoreFlags();
            }
            tmpHitBoxes.Clear();
            if(!LerpAnimLayer)
                return;
            
            Animator.SetLayerWeight(AnimLayerIndex, 0.0f);
        }
        
        protected abstract void HandleMotion(Vector3 velocity);
        
        private bool TestRelativeSide(Vector3 position, Vector3 test)
        {
            Vector3 cPos  = CTransform.position;
            Vector3 myPos = new(cPos.x, 0.0f, cPos.z);
            Vector3 vec   = position.normalized;
            float ang     = Mathf.Acos(Vector3.Dot(test, vec)) * Mathf.Rad2Deg;
            return ang <= 45.0f;
        }
        
        public enum DodgeAnimDirection : byte
        {
            None,
            Forward,
            Right,
            Back,
            Left
        }
    }
}
