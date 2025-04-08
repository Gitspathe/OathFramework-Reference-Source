using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.Data.StatParams;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{
    public class WeaponPumpEffect : LoopComponent, 
        ILoopUpdate, IWeaponModelInit, IEquipmentUseCallback, 
        IEntityInitCallback, IEntityStaggerCallback
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        [SerializeField] private CasingsEffect casingsEffect;
        [SerializeField] private Transform hand;
        [SerializeField] private Transform from;
        [SerializeField] private Transform to;
        [SerializeField] private AnimationCurve motionCurve;
        [SerializeField] private AudioParams sound;
        [SerializeField] private float soundDelay = 0.25f;
        [SerializeField] private float duration = 0.5f;

        private bool playedSound;
        private bool isActive;
        private float curTime;
        private AudioOverrides spatialOverride;
        private IEquipmentUserController owner;
        
        uint ILockableOrderedListElement.Order => 11_000;

        private void Awake()
        {
            spatialOverride = AudioOverrides.NoSpatialBlend;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            isActive    = false;
            playedSound = false;
            if(owner == null)
                return;
            
            owner.Entity.Callbacks.Unregister((IEntityStaggerCallback)this);
            owner.Equipment.Callbacks.Unregister((IEquipmentUseCallback)this);
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            if(!isActive)
                return;

            curTime        += Time.deltaTime * owner.Entity.CurStats.GetParam(AttackSpeedMult.Instance);
            float curvePos  = motionCurve.Evaluate(curTime / duration);
            Vector3 fromPos = from.position;
            Vector3 toPos   = to.position;
            Vector3 pos     = curvePos == 0.0f ? fromPos : fromPos + ((toPos - fromPos) * curvePos);
            hand.position   = pos;
            if(!ReferenceEquals(sound, null) && !playedSound && curTime > soundDelay) {
                playedSound              = true;
                AudioOverrides overrides = null;
                if(owner is PlayerController player) {
                    overrides = player.Mode != PlayerControllerMode.None ? null : spatialOverride;
                }
                AudioPool.Retrieve(transform.position, sound, overrides: overrides);
                if(!ReferenceEquals(casingsEffect, null)) {
                    casingsEffect.Emit();
                }
            }
            if(curTime < duration)
                return;

            isActive                = false;
            playedSound             = false;
            hand.transform.position = from.position;
        }

        void IWeaponModelInit.OnInitialized(IEquipmentUserController owner)
        {
            this.owner = owner;
            owner.Equipment.Callbacks.Register((IEquipmentUseCallback)this);
            owner.Entity.Callbacks.Register((IEntityStaggerCallback)this);
        }

        void IEquipmentUseCallback.OnEquipmentUse(EntityEquipment equipment, Equippable equippable, int ammo)
        {
            //if(ammo <= 0)
            //    return;
            
            isActive = true;
            curTime  = 0.0f;
        }

        void IEntityInitCallback.OnEntityInitialize(Entity entity) { }
        
        void IEntityStaggerCallback.OnStagger(Entity entity, StaggerStrength strength, Entity instigator)
        {
            isActive    = false;
            playedSound = false;
        }
    }
}
