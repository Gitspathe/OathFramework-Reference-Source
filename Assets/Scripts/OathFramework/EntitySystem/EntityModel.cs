using OathFramework.AbilitySystem;
using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.EquipmentSystem;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EntitySystem
{

    [RequireComponent(typeof(ModelSocketHandler))]
    public abstract class EntityModel : LoopComponent, 
        IModelSockets, IFootstepSource, IPoolableComponent, 
        IEntityInitCallback, IEntityDieCallback
    {
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;
        public Entity Entity                        { get; private set; }
        public Animator Animator                    { get; private set; }
        public EffectBoxController EfxBoxController { get; private set; }
        public FootstepControllerBase Footstep      { get; private set; }
        public EntityAudio Audio                    { get; private set; }
        public ModelSocketHandler Sockets           { get; private set; }
        public List<HitBox> hitBoxes                { get; private set; } = new();
        public virtual bool FootstepSpatialized     => true;
        bool IFootstepSource.Spatialized            => FootstepSpatialized;
        
        [field: SerializeField] public List<SkinnedMeshRenderer> Meshes { get; private set; }

        public PoolableGameObject PoolableGO { get; set; }
        
        uint ILockableOrderedListElement.Order => 10_000;

        protected virtual void Awake()
        {
            Animator         = GetComponent<Animator>();
            EfxBoxController = GetComponent<EffectBoxController>();
            Footstep         = GetComponent<FootstepControllerBase>();
            Sockets          = GetComponent<EntityModelSocketHandler>();
        }
        
        public virtual void OnEntityInitialize(Entity entity)
        {
            if(entity == Entity) {
                InitializeHitBoxes();
                return;
            }
            entity.OnModelSpawned();
            Entity             = entity;
            Entity.EntityModel = this;
            Audio              = Entity.GetComponent<EntityAudio>();
            FindHitBoxes(transform);
            InitializeHitBoxes();
            if(EfxBoxController != null) {
                EfxBoxController.Initialize(entity);
            }
            if(Footstep != null) {
                Footstep.Initialize(this);
            }
            entity.Callbacks.Register((IEntityDieCallback)this);
            Entity.Animation.UpdateCullParam();
        }

        public void SetUpdateOffscreen(bool val)
        {
            foreach(SkinnedMeshRenderer mesh in Meshes) {
                mesh.updateWhenOffscreen = val;
            }
        }

        private void FindHitBoxes(Transform modelTransform)
        {
            hitBoxes.Clear();
            foreach(HitBox hitBox in modelTransform.GetComponentsInChildren<HitBox>()) {
                hitBoxes.Add(hitBox);
            }
        }

        private void InitializeHitBoxes()
        {
            foreach(HitBox hitBox in hitBoxes) {
                hitBox.Initialize(Entity);
            }
        }
        
        public void GetHitBoxes(QList<HitBox> hitBoxes)                      => GetHitBoxes(null, hitBoxes);
        public void GetHitBoxes(HitBoxGroups groups, QList<HitBox> hitBoxes) => GetHitBoxes((HitBoxFlags)groups, hitBoxes);

        public void GetHitBoxes(HitBoxFlags? flags, QList<HitBox> hitBoxes)
        {
            if(flags == null) {
                hitBoxes.AddRange(this.hitBoxes);
                return;
            }
            foreach(HitBox hitBox in this.hitBoxes) {
                if(hitBox.HasFlags(flags.Value)) {
                    hitBoxes.Add(hitBox);
                }
            }
        }

        public void TriggerEffects()
        {
            if(Entity.TryGetComponent(out ItemHandler itemHandler) && itemHandler.TriggerEffects())
                return;
            if(!Entity.TryGetComponent(out AbilityHandler abilityHandler))
                return;
            
            abilityHandler.ActivateQueuedAbility();
        }
        
        public void ModelStartLoop(string id)
        {
            if(ReferenceEquals(Audio, null))
                return;
            
            Audio.PlayAudioLoop(id);
        }

        public void ModelPlaySound(string id)
        {
            if(ReferenceEquals(Audio, null))
                return;
            
            Audio.PlayAudioClip(id);
        }

        public virtual void OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            foreach(HitBox hitBox in hitBoxes) {
                hitBox.OnDeath();
            }
            gameObject.SetActive(false);
        }

        public virtual void OnRetrieve()
        {
            InitializeHitBoxes();
            gameObject.SetActive(true);
        }

        public virtual void OnReturn(bool initialization)
        {
            if(initialization)
                return;
            
            foreach(HitBox hitBox in hitBoxes) {
                hitBox.RestoreFlags();
            }
        }
    }
    
    public readonly struct AimIKGoalParams
    {
        public float Weight    { get; }
        public float LerpSpeed { get; }

        public bool IsInstant => LerpSpeed > 999.99f;

        public AimIKGoalParams(float weight, float lerpSpeed = 1000.0f)
        {
            Weight    = weight;
            LerpSpeed = lerpSpeed;
        }
    }

    public interface IEntityModelFeature
    {
    }

    public interface IEntityModelEquippables : IEntityModelFeature
    {
        void ChangeEquippableAnimationSet(EquippableAnimSets animSet);
        void SetupHolsteredModel(EquippableHolsteredModel model);
        void SetupEquippedModel(EquippableThirdPersonModel model);
        void SetupEquippedWeapon(EquippableThirdPersonModel weaponModel, float suppressIKTime, bool updateAimIK = true);
        void AnimPutAway();
    }

    public interface IEntityModelThrow : IEntityModelFeature
    {
        PreviewTrajectory TrajectoryArc { get; set; }
        Transform ThrowOffsetTransform  { get; }
        
        void SetAimAnim(bool val);
        void PlayThrow();
        Vector3 GetThrowStrength();
    }

    public interface IEntityModelReload : IEntityModelFeature
    {
        void SetFinishReloadAnim(bool val);
        void SetReloadAnim(bool reload, float reloadSpeed);
        void ModelReloadAmmo();
        void ModelEndReload();
    }
    
    public interface IEntityModelEquipment : IEntityModelEquippables, IEntityModelReload, IEntityModelThrow { }
}
