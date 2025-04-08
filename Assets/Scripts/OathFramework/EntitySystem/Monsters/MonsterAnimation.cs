using OathFramework.Core;
using OathFramework.Pooling;
using OathFramework.Utility;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.Monsters
{
    public class MonsterAnimation : EntityAnimation,
        IPoolableComponent, IEntityStaggerCallback
    {
        [SerializeField] private float moveSpeedLerp = 5.0f;
        
        private int moveParam;
        private int attackParam;
        private float curMoveSpeed;
        private NetworkVariable<int> moveSpeedNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );

        [field: SerializeField] public string AnimMoveSpeed { get; private set; } = "MoveSpeed";
        [field: SerializeField] public string AnimAttack    { get; private set; } = "Attack";
        
        public override int UpdateOrder        => GameUpdateOrder.AIProcessing;
        uint ILockableOrderedListElement.Order => 100;

        protected override void Awake()
        {
            base.Awake();
            moveParam   = Animator.StringToHash(AnimMoveSpeed);
            attackParam = Animator.StringToHash(AnimAttack);
        }

        public override void LoopUpdate()
        {
            if(!IsOwner) {
                curMoveSpeed = moveSpeedNetVar.Value;
            }
            
            EntityModel model = Controller.Entity.EntityModel;
            if(ReferenceEquals(model, null))
                return;
            
            Animator animator = model.Animator;
            float curParam    = animator.GetFloat(moveParam);
            animator.SetFloat(moveParam, Mathf.Lerp(curParam, curMoveSpeed, moveSpeedLerp * Time.deltaTime));
        }

        public void SetMoveSpeed(int speed)
        {
            if(!IsOwner)
                return;

            moveSpeedNetVar.Value = speed;
            curMoveSpeed          = speed;
        }
        
        void IEntityStaggerCallback.OnStagger(Entity entity, StaggerStrength strength, Entity instigator)
        {
            EntityModel model = Controller.Entity.EntityModel;
            if(ReferenceEquals(model, null))
                return;
            
            Animator animator = model.Animator;
            animator.ResetTrigger(attackParam);
        }

        public override void OnRetrieve()
        {
            SetMoveSpeed(0);
        }

        public override void OnReturn(bool initialization) { }
    }

}
