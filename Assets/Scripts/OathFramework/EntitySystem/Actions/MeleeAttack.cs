using Sirenix.OdinInspector;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public abstract partial class MeleeAttack : Action
    {
        protected int NameHash;
        protected int IndexHash;
        
        [field: Header("Attack Params")]

        [field: SerializeField] public int AttackAnimIndex     { get; private set; }
        [field: SerializeField] public string AnimatorAttack   { get; private set; } = "Attack";
        [field: SerializeField] public string AnimatorIndex    { get; private set; } = "AttackIndex";
        
        [field: Space(5)]
        
        [field: SerializeField] public float Duration          { get; private set; } = 1.0f;

        [field: Space(5)]
        
        [field: SerializeField, ValueDropdown("GetEditorEffectBoxes")] 
        public int[] EffectBoxes                               { get; private set; }
        
        [field: SerializeField] public bool HasUnion           { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(HasUnion))]
        public EffectBoxUnion Union                            { get; private set; }
        
        [field: SerializeField] public float DamageMult        { get; private set; } = 1.0f;
        [field: SerializeField] public StaggerStrength Stagger { get; private set; }
        [field: SerializeField] public ushort StaggerAmount    { get; private set; }
        
        public float Progress { get; protected set; }
        
        protected Animator Animator => Entity.EntityModel.Animator;
        
        protected override void OnInitialize()
        {
            NameHash  = Animator.StringToHash(AnimatorAttack);
            IndexHash = Animator.StringToHash(AnimatorIndex);
        }
        
        protected override void OnTick(float deltaTime)
        {
            Progress = Mathf.Clamp(Progress + (Time.deltaTime / Duration), 0.0f, 1.0f);
        }
        
        protected override void OnEnd()
        {
            Progress = 0.0f;
            EffectBoxController effectBoxController = Entity.EntityModel.EfxBoxController;
            if(EffectBoxes != null) {
                effectBoxController.DeactivateEffectBoxes();
            }
            if(HasUnion && !ReferenceEquals(Union, null)) {
                Union.ResetUnion();
            }
        }
    }
}
