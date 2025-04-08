using OathFramework.AbilitySystem;
using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.Core.Service;
using OathFramework.Data.StatParams;
using OathFramework.EquipmentSystem;
using OathFramework.Networking;
using OathFramework.PerkSystem;
using OathFramework.Pooling;
using OathFramework.UI;
using OathFramework.Utility;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.Players
{

    [RequireComponent(typeof(Entity), typeof(EntityEquipment), typeof(PlayerAnimation)), 
     RequireComponent(typeof(DodgeHandler), typeof(EntityAudio), typeof(ItemHandler)), 
     RequireComponent(typeof(QuickHealHandler))]
    public class PlayerController : NetLoopComponent, 
        ILoopLateUpdate, IPoolableComponent, IPlayerController, 
        IEntityInitCallback, IEntityTakeDamageCallback, IEntityDieCallback, 
        IEntityHealCallback, IEntityScoreKillCallback, IAudioSpatialCondition,
        ICameraControllerTarget
    {
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private float throwAngle       = 17.5f;
        [SerializeField] private float throwFloorHeight = 1.4f;
        
        public GameObject debugPlayerModelPrefab; // TODO: Replace with proper menu etc.

        private float aimSmoothenTime;
        private NetworkBindHelper bindHelper;
        private Vector2 movementVec;
        private Vector2 movementInput;

        private NetworkVariable<Vector3> aimTargetNetVar  = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );

        private NetworkVariable<Vector3> aimTargetUnclampedNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );
        
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;
        
        public bool IsDead => Entity.IsDead;
        
        public Transform CTransform           { get; private set; }
        public CharacterController Movement   { get; private set; }
        public Entity Entity                  { get; private set; }
        public EntityModel Model              { get; private set; }
        public EntityAnimation Animation      { get; private set; }
        public EntityEquipment Equipment      { get; private set; }
        public PlayerAbilityHandler Abilities { get; private set; }
        public PlayerPerkHandler Perks        { get; private set; }
        public EntityAudio Audio              { get; private set; }
        public DodgeHandler DodgeHandler      { get; private set; }
        public ItemHandler ItemHandler        { get; private set; }
        public QuickHealHandler QuickHeal     { get; private set; }
        public Transform AimTarget            { get; private set; }
        public Transform AimTargetUnclamped   { get; private set; }

        public PlayerModel PlayerModel         => Model as PlayerModel;
        public PlayerAnimation PlayerAnimation => Animation as PlayerAnimation;

        public Vector3 CurrentThrowForce              { get; private set; }
        public float TimeSinceMoving                  { get; private set; }
        public ActionBlockHandler ActionBlocks        { get; } = new();
        public ExtValue<float>.Handler MovementDampen { get; } = new();
        public ExtValue<float>.Handler AimDampen      { get; } = new();
        
        public ExtBool.Handler DodgeBlocked   { get; private set; }
        public ExtBool.Handler ItemUseBlocked { get; private set; }
        
        public bool IsStaggered             => Entity.Stagger.IsStaggered;
        public bool IsStaggerUncontrollable => Entity.Stagger.IsUncontrollable;
        public bool IsMovementDampened      => MovementDampen.Value > 0.01f;
        public bool IsAimDampened           => AimDampen.Value > 0.01f || aimSmoothenTime > 0.0f;
        
        public CameraController CameraController { get; set; }
        public PlayerControllerMode Mode         { get; private set; }
        public NetClient NetClient               { get; private set; }
        public PoolableGameObject PoolableGO     { get; set; }
        
        public static PlayerController Active { get; private set; }
        
        uint ILockableOrderedListElement.Order => 1_000;
        
        Transform ICameraControllerTarget.CamFollowTransform => transform;

        private void Awake()
        {
            CTransform       = transform;
            Entity           = GetComponent<Entity>();
            Equipment        = GetComponent<EntityEquipment>();
            Abilities        = GetComponent<PlayerAbilityHandler>();
            Perks            = GetComponent<PlayerPerkHandler>();
            Audio            = GetComponent<EntityAudio>();
            Movement         = GetComponent<CharacterController>();
            Animation        = GetComponent<PlayerAnimation>();
            DodgeHandler     = GetComponent<DodgeHandler>();
            ItemHandler      = GetComponent<ItemHandler>();
            QuickHeal        = GetComponent<QuickHealHandler>();
            Movement.enabled = false;
            bindHelper       = new NetworkBindHelper(this);
            
            DodgeBlocked     = ExtBool.Handler.CreateFunc(this, DodgeBlockedFunc);
            ItemUseBlocked   = ExtBool.Handler.CreateFunc(this, UseBlockedFunc);
            
            Audio.SpatialCondition = this;
            return;

            bool DodgeBlockedFunc(PlayerController t) 
                => t.IsStaggerUncontrollable
                   || ActionBlocks.Contains(ActionBlockers.Dodge)
                   || ActionBlocks.Contains(ActionBlockers.Stagger)
                   || ActionBlocks.Contains(ActionBlockers.AbilityUse)
                   || t.Entity.CurStats.stamina == 0;

            bool UseBlockedFunc(PlayerController t)
                => t.Equipment.IsReloading
                   || t.IsStaggerUncontrollable
                   || t.Entity.CurStats.GetParam(QuickHealCharges.Instance) == 0
                   || ActionBlocks.Contains(ActionBlockers.Dodge)
                   || ActionBlocks.Contains(ActionBlockers.Stagger)
                   || ActionBlocks.Contains(ActionBlockers.AbilityUse);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _ = BindTask(destroyCancellationToken);
        }
        
        public void OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityTakeDamageCallback)this);
            entity.Callbacks.Register((IEntityDieCallback)this);
            entity.Callbacks.Register((IEntityTakeDamageCallback)this);
            entity.Callbacks.Register((IEntityScoreKillCallback)this);
        }

        private async UniTask BindTask(CancellationToken ct)
        {
            NetClient netClient;
            while(!PlayerManager.TryGetPlayerFromNetID(OwnerClientId, out netClient) || !bindHelper.IsFinished) {
                await UniTask.Yield(cancellationToken: ct);
            }

            NetClient          = netClient;
            AimTargetUnclamped = IsOwner
                ? GameObject.Find("TargetMouseUnclamped").transform
                : new GameObject($"TargetMouseUnclamped ({OwnerClientId})").transform;
            AimTarget          = IsOwner 
                ? GameObject.Find("TargetMouse").transform 
                : new GameObject($"TargetMouse ({OwnerClientId})").transform;

            if(IsOwner) {
                //UIScript.Instance.HideDeathUI();
            }

            // TODO: Replace with proper system.
            // ^ Placeholder code for spawning the player model.
            SetupModel(debugPlayerModelPrefab);
            
            ChangeMode(IsOwner ? PlayerControllerMode.Playing : PlayerControllerMode.None);
            GameServices.PlayerSpawn.OnPlayerGOSpawned(this, netClient);
            Movement.enabled = true;
            bindHelper.TriggerBoundCallback();
        }

        public void LoopLateUpdate()
        {
            audioListener.transform.rotation = Quaternion.identity;
            if(IsDead || !Entity.NetInitComplete)
                return;
            
            if(!IsOwner) {
                AimTarget.position          = aimTargetNetVar.Value;
                AimTargetUnclamped.position = aimTargetUnclampedNetVar.Value;
                UpdateThrowForce();
                return;
            }
            aimSmoothenTime -= Time.deltaTime;
            Move();
            Turn();
            UpdateThrowForce();
        }

        private void Move()
        {
            float speed      = Entity.CurStats.speed;
            float mult       = IsMovementDampened ? 1.0f - MovementDampen.Value : 1.0f;
            movementVec      = Vector3.Lerp(movementVec, movementInput, 16.0f * Time.deltaTime);
            Vector3 moveVec  = new Vector3(movementVec.y, 0.0f, -movementVec.x) * mult;
            Vector3 movement = movementVec.magnitude > 0.01f ? speed * Time.deltaTime * moveVec : default;
            float movementM  = movement.magnitude;
            if(movementM > 0.01f) {
                TimeSinceMoving = 0.0f;
            } else {
                TimeSinceMoving += Time.deltaTime;
            }
            
            Movement.Move(movement);
            if(!Movement.isGrounded) {
                Movement.Move(Physics.gravity * Time.deltaTime);
            }
            PlayerAnimation.UpdateMovement(moveVec);
        }

        private void Turn()
        {
            Vector3 targetDir     = AimTarget.position - CTransform.position;
            Quaternion target     = Quaternion.LookRotation(targetDir);
            Quaternion targetLook = Quaternion.Euler(0.0f, target.eulerAngles.y, 0.0f);
            bool hasTarget        = !ReferenceEquals(AimTarget, null);
            bool hasTargetUC      = !ReferenceEquals(AimTargetUnclamped, null);
            if(IsAimDampened) {
                float lerp          = 12.5f * (1.0f - AimDampen.Value) * Time.deltaTime;
                CTransform.rotation = Quaternion.Slerp(CTransform.rotation, targetLook, lerp);
            } else {
                CTransform.rotation = targetLook;
            }
            aimTargetNetVar.Value          = hasTarget ? AimTarget.position : default;
            aimTargetUnclampedNetVar.Value = hasTargetUC ? AimTargetUnclamped.position : default;
        }

        private void UpdateThrowForce()
        {
            if(!Equipment.CurrentSlot.IsThrowing || !(Model is IEntityModelThrow mThrow)) {
                CurrentThrowForce = Vector3.zero;
                return;
            }

            Vector3 dest = new(AimTargetUnclamped.position.x, throwFloorHeight, AimTargetUnclamped.position.z);
            CurrentThrowForce = CalculateThrowForce(mThrow.ThrowOffsetTransform.position, dest, throwAngle);
        }
        
        private Vector3 CalculateThrowForce(Vector3 source, Vector3 target, float angle)
        {
            Vector3 direction     = target - source;
            float h               = direction.y;
            direction.y           = 0.0f;
            float distance        = direction.magnitude;
            float radianAngle     = angle * Mathf.Deg2Rad;
            float gravity         = Mathf.Abs(Physics.gravity.y);
            float velocitySquared = (gravity * distance * distance) / (2f * (distance * Mathf.Tan(radianAngle) - h));
            if(velocitySquared < 0)
                return Vector3.zero;

            float velocity         = Mathf.Sqrt(velocitySquared);
            Vector3 velocityVector = direction.normalized * (velocity * Mathf.Cos(radianAngle));
            velocityVector.y       = velocity * Mathf.Sin(radianAngle);
            return velocityVector;
        }

        public void ControlsSetMovement(Vector3 movement)
        {
            movementInput = movement;
        }

        public void SetupModel(GameObject prefab)
        {
            if(PlayerModel != null) {
                Destroy(PlayerModel.gameObject);
            }
            GameObject go = Instantiate(prefab, transform);
            Model         = go.GetComponent<PlayerModel>();
            PlayerModel.InitPlayerModel(this);
        }

        public void ChangeMode(PlayerControllerMode mode)
        {
            Mode = mode;
            if(mode != PlayerControllerMode.None) {
                Active = this;
            }
            if(audioListener != null) {
                audioListener.enabled = mode != PlayerControllerMode.None;
            }
            if(mode == PlayerControllerMode.None)
                return;

            CameraController.Instance.SetTarget(this);
            HUDScript.AttachedPlayer         = this;
        }

        public bool TryDoDodge(Vector3? directionOverride = null)
        {
            if(DodgeBlocked)
                return false;
            
            Vector3 direction = directionOverride ?? new Vector3(movementInput.y, 0.0f, -movementInput.x);
            if(direction == Vector3.zero) {
                direction = -transform.forward.normalized;
                direction = new Vector3(direction.x, 0.0f, direction.z);
            }
            DodgeHandler.DoDodge(direction.normalized);
            return true;
        }

        public void AddAimSmoothen(float time)
        {
            aimSmoothenTime = time;
        }

        void IEntityTakeDamageCallback.OnDamage(Entity entity, bool fromRpc, in DamageValue val)
        {

        }

        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            Movement.enabled = false;
            if(HUDScript.AttachedPlayer == this) {
                HUDScript.AttachedPlayer = null;
            }
            if(Mode != PlayerControllerMode.None) {
                ChangeMode(PlayerControllerMode.None);
            }
            PlayerSpawnService.Instance.OnPlayerDeath(NetClient);
        }

        void IEntityHealCallback.OnHeal(Entity entity, bool fromRpc, in HealValue val)
        {

        }

        void IEntityScoreKillCallback.OnScoredKill(Entity entity, IEntity other, in DamageValue lastDamageVal, float ratio)
        {

        }

        void IPoolableComponent.OnRetrieve()
        {
            Movement.enabled = true;
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            
        }

        bool IAudioSpatialCondition.GetAudioSpatial() 
            => Mode != PlayerControllerMode.Playing && Mode != PlayerControllerMode.Spectating;
    }

    public enum PlayerControllerMode { None, Playing, Spectating, }
}
