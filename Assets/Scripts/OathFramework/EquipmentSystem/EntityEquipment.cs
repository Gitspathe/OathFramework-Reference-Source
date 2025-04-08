using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using OathFramework.Core;
using OathFramework.Data.StatParams;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.EntitySystem.Projectiles;
using OathFramework.Networking;
using OathFramework.Persistence;
using OathFramework.Progression;
using OathFramework.Utility;
using System;
using System.Threading;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable IdentifierTypo

namespace OathFramework.EquipmentSystem
{
    [RequireComponent(typeof(ProjectileProvider))]
    public partial class EntityEquipment : NetLoopComponent, 
        ILoopLateUpdate, INetworkBindHelperNode, IPersistableComponent, 
        IEntityInitCallback, IEntityDieCallback, IEntityStaggerCallback
    {
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;

        private AccessToken token;
        private Executor executor;
        private CancellationTokenSource swapCts;
        private float swapTime;
        private byte localCurrentSlot;
        private EquipmentSlot lastSlot;
        private Dictionary<Equippable, QList<CopyableModelPlug.CopyData>> modelPlugCopies = new();
        
        private InventorySlot emptySlot     = new(EquipmentSlot.None);
        private InventorySlot primarySlot   = new(EquipmentSlot.Primary);
        private InventorySlot secondarySlot = new(EquipmentSlot.Secondary);
        private InventorySlot meleeSlot     = new(EquipmentSlot.Melee);
        private InventorySlot specialSlot1  = new(EquipmentSlot.Special1);
        private InventorySlot specialSlot2  = new(EquipmentSlot.Special2);
        private InventorySlot specialSlot3  = new(EquipmentSlot.Special3);

        private NetworkVariable<NetInventorySlot> netPrimarySlot = new(
            new NetInventorySlot(EquipmentSlot.Primary),
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );

        private NetworkVariable<NetInventorySlot> netSecondarySlot = new(
            new NetInventorySlot(EquipmentSlot.Secondary),
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );

        private NetworkVariable<NetInventorySlot> netMeleeSlot = new(
            new NetInventorySlot(EquipmentSlot.Melee),
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<NetInventorySlot> netSpecialSlot1 = new(
            new NetInventorySlot(EquipmentSlot.Special1),
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<NetInventorySlot> netSpecialSlot2 = new(
            new NetInventorySlot(EquipmentSlot.Special2),
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<NetInventorySlot> netSpecialSlot3 = new(
            new NetInventorySlot(EquipmentSlot.Special3),
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<byte> netCurrentSlot = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );
        
        public bool IsSwapping  => swapTime > 0.0f;
        public bool IsReloading { get; private set; }
        
        public EquippableThirdPersonModel ThirdPersonEquippableModel    { get; private set; }
        public List<EquippableHolsteredModel> HolsteredEquippableModels { get; private set; } = new();

        public float UseCooldown          { get; private set; }
        public float CurRecoilDelay       { get; private set; }
        public float CurRecoil            { get; private set; }
        public float CurAntiInterruptTime { get; private set; }
        public bool PendingFinishReload   { get; private set; }
        
        public NetworkBindHelper Binder            { get; set; }
        public IEquipmentUserController Controller { get; private set; }
        public IEntityModelEquipment Model         { get; private set; }
        public ProjectileProvider Projectiles      { get; private set; }
        public EquipmentCallbacks Callbacks        { get; } = new();
        public Stats CurEntityStats                => Controller.Entity.CurStats;
        public InventorySlot CurrentSlot           => GetInventorySlot((EquipmentSlot)localCurrentSlot);
        public InventorySlot[] InventorySlotArray  { get; private set; }
        
        [field: SerializeField] public float ProjectileSpawnY { get; private set; } = 2.0f;

        public ExtBool.Handler HandlerUseBlocked      { get; private set; }
        public ExtBool.Handler UseBlocked             { get; private set; }
        public ExtBool.Handler ReloadBlocked          { get; private set; }
        public ExtBool.Handler InterruptReloadBlocked { get; private set; }
        public ExtBool.Handler SwapBlocked            { get; private set; }

        private bool IsDead => Controller.Entity.IsDead;
        
        uint ILockableOrderedListElement.Order => 10_000;

        private void Awake()
        {
            executor           = new Executor(this);
            token              = Callbacks.Access.GenerateAccessToken();
            Controller         = GetComponent<IEquipmentUserController>();
            Projectiles        = GetComponent<ProjectileProvider>();
            InventorySlotArray = new[] { primarySlot, secondarySlot, meleeSlot, specialSlot1, specialSlot2, specialSlot3 };

            HandlerUseBlocked      = ExtBool.Handler.CreateFunc(this, HandlerUseBlockFlag);
            UseBlocked             = ExtBool.Handler.CreateFunc(this, UseBlockFlag);
            ReloadBlocked          = ExtBool.Handler.CreateFunc(this, ReloadBlockFlag);
            InterruptReloadBlocked = ExtBool.Handler.CreateFunc(this, InterruptReloadBlockFlag);
            SwapBlocked            = ExtBool.Handler.CreateBasic();
            return;

            bool InterruptReloadBlockFlag(EntityEquipment t)
                => t.IsSwapping
                   || !t.IsReloading
                   || t.UseCooldown > 0.0f
                   || t.CurAntiInterruptTime > 0.0f
                   || t.CurrentSlot.InterruptReloadBlocked 
                   || !t.CurrentSlot.IsReloadable;

            bool ReloadBlockFlag(EntityEquipment t)
                => t.IsReloading
                   || t.IsSwapping
                   || t.CurrentSlot.ReloadBlocked
                   || !t.CurrentSlot.IsReloadable
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.Dodge)
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.Stagger)
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.AbilityUse);

            bool HandlerUseBlockFlag(EntityEquipment t)
                => (t.IsReloading && t.InterruptReloadBlocked)
                   || t.IsSwapping
                   || t.UseCooldown > 0.0f
                   || t.CurrentSlot.IsEmpty
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.Dodge)
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.Stagger)
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.AbilityUse);
            
            bool UseBlockFlag(EntityEquipment t)
                => t.IsReloading
                   || t.IsSwapping
                   || t.UseCooldown > 0.0f
                   || t.CurrentSlot.IsEmpty
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.Dodge)
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.Stagger)
                   || t.Controller.ActionBlocks.Contains(ActionBlockers.AbilityUse);
        }
        
        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityDieCallback)this);
            entity.Callbacks.Register((IEntityStaggerCallback)this);
        }

        void IEntityStaggerCallback.OnStagger(Entity entity, StaggerStrength strength, Entity instigator)
        {
            EndReloadInternal();
        }

        public void LoopLateUpdate()
        {
            if(Controller.Entity.IsDummy || IsDead || Game.IsQuitting || Game.State != GameState.InGame)
                return;

            swapTime             = Mathf.Clamp(swapTime - Time.deltaTime, -1.0f, 9999.0f);
            UseCooldown          = Mathf.Clamp(UseCooldown - Time.deltaTime, -1.0f, 9999.0f);
            CurAntiInterruptTime = Mathf.Clamp(CurAntiInterruptTime - Time.unscaledDeltaTime, -1.0f, 9999.0f);
            executor.ProcessEquippable();
        }

        public void UpdateHeldWeapons(in PlayerBuildData buildData)
        {
            if(!IsOwner)
                return;

            UpdateSlot(netPrimarySlot, buildData.equippable1);
            UpdateSlot(netSecondarySlot, buildData.equippable2);
        }
        
        private void UpdateSlot(NetworkVariable<NetInventorySlot> slot, string equippable, ushort? ammo = null)
        {
            if(string.IsNullOrEmpty(equippable) || !EquippableManager.TryGetNetID(equippable, out ushort id)) {
                slot.Value = new NetInventorySlot(slot.Value.slot, 0, 0);
                return;
            }
            slot.Value = new NetInventorySlot(slot.Value.slot, id, ammo ?? GetTemplateAmmo(equippable));
        }
        
        private static ushort GetTemplateAmmo(string equip)
        {
            Equippable e = EquippableManager.GetTemplate(equip);
            return ReferenceEquals(e, null) ? (ushort)0 : e.GetRootStats().AmmoCapacity;
        }

        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            executor.OnDie();
            RemoveEquippedModel();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            netPrimarySlot.OnValueChanged   += (prev, cur) => OnNetInventorySlotChanged(primarySlot, prev, cur);
            netSecondarySlot.OnValueChanged += (prev, cur) => OnNetInventorySlotChanged(secondarySlot, prev, cur);
            netMeleeSlot.OnValueChanged     += (prev, cur) => OnNetInventorySlotChanged(meleeSlot, prev, cur);
            netSpecialSlot1.OnValueChanged  += (prev, cur) => OnNetInventorySlotChanged(specialSlot1, prev, cur);
            netSpecialSlot2.OnValueChanged  += (prev, cur) => OnNetInventorySlotChanged(specialSlot2, prev, cur);
            netSpecialSlot3.OnValueChanged  += (prev, cur) => OnNetInventorySlotChanged(specialSlot3, prev, cur);
            if(!IsOwner) {
                netCurrentSlot.OnValueChanged += OnNetEquipmentModelChanged;
            }
            Binder.OnNetworkSpawnCallback(this);
        }
        
        void INetworkBindHelperNode.OnBound()
        {
            Model = Controller.Model as IEntityModelEquipment;
            if(Model == null) {
                Debug.LogError($"{nameof(Model)} is not an {nameof(IEntityModelEquipment)})");
                Destroy(this);
                return;
            }

            if(Controller is IPlayerController pController) {
                UpdateHeldWeapons(in pController.NetClient.Data.CurrentBuild);
            }
            primarySlot.Initialize(netPrimarySlot.Value);
            secondarySlot.Initialize(netSecondarySlot.Value);
            meleeSlot.Initialize(netMeleeSlot.Value);
            specialSlot1.Initialize(netSpecialSlot1.Value);
            specialSlot2.Initialize(netSpecialSlot2.Value);
            specialSlot3.Initialize(netSpecialSlot3.Value);
            InstantSwap(IsOwner ? EquipmentSlot.Primary : (EquipmentSlot)netCurrentSlot.Value);
        }

        private async UniTask SwapTask(InventorySlot currentSlot, InventorySlot nextSlot, CancellationToken ct)
        {
            if(!currentSlot.IsEmpty && !IsSpecialSlot(currentSlot.SlotID)) {
                lastSlot = currentSlot.SlotID;
            }
            if(IsOwner) {
                netCurrentSlot.Value = (byte)nextSlot.SlotID;
            }
            if(!nextSlot.IsEmpty) {
                nextSlot.Equippable.ApplySounds(Controller.Audio);
            }
            StoreModelPlugCopyData();
            float duration     = 0.0f;
            float nextSwapTime = 0.0f;
            if(!currentSlot.IsEmpty) {
                duration = currentSlot.GetStatsAs<EquippableStats>().PutAwayTime / CurEntityStats.GetParam(SwapSpeedMult.Instance);
            }
            if(!nextSlot.IsEmpty) {
                nextSwapTime = nextSlot.GetStatsAs<EquippableStats>().TakeOutTime / CurEntityStats.GetParam(SwapSpeedMult.Instance);
            }
            Model.AnimPutAway();
            currentSlot.Equippable?.PutAway(this);
            swapTime             = duration + nextSwapTime;
            CurAntiInterruptTime = -1.0f;
            PendingFinishReload  = false;
            if(await UniTask.WaitForSeconds(duration, cancellationToken: ct).SuppressCancellationThrow())
                return;
            
            SetupEquippedModel(
                nextSlot.IsEmpty ? string.Empty : nextSlot.Equippable.EquippableKey,
                !currentSlot.IsEmpty ? currentSlot.SwapIKSuppressTime : 0.1f
            );
            SetupHolsteredModels();
            AssignModelPlugCopyData();
            nextSlot.Equippable?.TakenOut(this);
        }
        
        private void InstantSwap(EquipmentSlot slot)
        {
            StoreModelPlugCopyData();
            InventorySlot currentSlot = CurrentSlot;
            InventorySlot invSlot     = GetInventorySlot(slot);
            if(!currentSlot.IsEmpty && !IsSpecialSlot(currentSlot.SlotID)) {
                lastSlot = invSlot.SlotID;
            }
            if(IsOwner) {
                netCurrentSlot.Value = (byte)invSlot.SlotID;
            }
            currentSlot.Equippable?.PutAway(this);
            localCurrentSlot = (byte)invSlot.SlotID;
            if(!CurrentSlot.IsEmpty) {
                CurrentSlot.Equippable.UnregisterFromProjectileProvider(Projectiles);
            }
            if(!invSlot.IsEmpty) {
                invSlot.Equippable.ApplySounds(Controller.Audio);
                invSlot.Equippable.RegisterToProjectileProvider(Projectiles);
                invSlot.Equippable.TakenOut(this);
            }
            CurAntiInterruptTime = -1.0f;
            PendingFinishReload  = false;
            SetupEquippedModel(
                invSlot.IsEmpty ? string.Empty : invSlot.Equippable.EquippableKey,
                !currentSlot.IsEmpty ? currentSlot.SwapIKSuppressTime : 0.1f
            );
            SetupHolsteredModels();
            AssignModelPlugCopyData();
            Callbacks.Access.OnSwap(token, this, currentSlot.IsEmpty ? null : currentSlot.Equippable, invSlot.IsEmpty ? null : invSlot.Equippable);
        }

        private void OnNetInventorySlotChanged(InventorySlot slot, NetInventorySlot previous, NetInventorySlot current)
        {
            if(!Binder.IsFinished)
                return;
            
            SetEquippableForSlot(slot.SlotID, current.EquippableKey, false);
            SetAmmoForSlot(slot.SlotID, current.ammo, false);
            if(current.EquippableKey != previous.EquippableKey) {
                SetupHolsteredModels();
            }
        }

        private void OnNetEquipmentModelChanged(byte previous, byte current) => SetCurrentSlot((EquipmentSlot)current);

        private NetworkVariable<NetInventorySlot> GetNetInventorySlot(EquipmentSlot slot)
        {
            switch(slot) {
                case EquipmentSlot.Primary:
                    return netPrimarySlot;
                case EquipmentSlot.Secondary:
                    return netSecondarySlot;
                case EquipmentSlot.Melee:
                    return netMeleeSlot;
                case EquipmentSlot.Special1:
                    return netSpecialSlot1;
                case EquipmentSlot.Special2:
                    return netSpecialSlot2;
                case EquipmentSlot.Special3:
                    return netSpecialSlot3;

                default:
                case EquipmentSlot.None:
                    return null;
            }
        }

        public InventorySlot GetInventorySlot(EquipmentSlot slot)
        {
            switch(slot) {
                case EquipmentSlot.Primary:
                    return primarySlot;
                case EquipmentSlot.Secondary:
                    return secondarySlot;
                case EquipmentSlot.Melee:
                    return meleeSlot;
                case EquipmentSlot.Special1:
                    return specialSlot1;
                case EquipmentSlot.Special2:
                    return specialSlot2;
                case EquipmentSlot.Special3:
                    return specialSlot3;

                default:
                case EquipmentSlot.None:
                    return emptySlot;
            }
        }

        public void SwapEquipmentSlot(SlotCycleDirection direction)
        {
            if(!IsOwner)
                return;

            EquipmentSlot changeSlot = CurrentSlot.SlotID;
            EquipmentSlot nextCheck  = CurrentSlot.SlotID;
            while(true) {
                if(direction == SlotCycleDirection.Down && nextCheck == EquipmentSlot.Special3) {
                    nextCheck = EquipmentSlot.None;
                } else if(direction == SlotCycleDirection.Up && nextCheck == EquipmentSlot.None) {
                    nextCheck = EquipmentSlot.Special3;
                } else {
                    nextCheck = direction == SlotCycleDirection.Down ? nextCheck + 1 : nextCheck - 1;
                }
                if(nextCheck == changeSlot)
                    break;
                
                InventorySlot next = GetInventorySlot(nextCheck);
                if(next.IsEmpty || next.IsTemporary)
                    continue;

                // Found a slot which has a valid equippable.
                SwapEquipmentSlot(nextCheck);
                break;
            }

            // If we went through the inventory without finding another valid equippable, equip the primary slot.
            if(nextCheck == changeSlot) {
                SwapEquipmentSlot(EquipmentSlot.Primary);
            }
        }

        public bool SwapEquipmentSlot(EquipmentSlot slot, bool swapIfAlreadyEquipped = true)
        {
            InventorySlot iSlot = GetInventorySlot(slot);
            if(!IsOwner || (iSlot == CurrentSlot && !swapIfAlreadyEquipped))
                return false;
            
            if(iSlot.IsEmpty) {
                SetCurrentSlot(slot);
                return false;
            }
            SetCurrentSlot(slot);
            return true;
        }

        public void Use(EquipmentSlot slot = EquipmentSlot.None)
        {
            if(!IsOwner)
                return;
            
            if(slot == EquipmentSlot.None) {
                slot = CurrentSlot.SlotID;
            }
            if(CurrentSlot.IsEmpty)
                return;
            
            if(CurrentSlot.IsReloadable && CurrentSlot.Ammo == 0 && !ReloadBlocked) {
                BeginReload();
                return;
            }
            if(IsReloading && !InterruptReloadBlocked && !PendingFinishReload) {
                if(CurrentSlot.GetStatsInterface<IStatsReload>()?.ReloadEndTime > 0.0001f) {
                    Model.SetFinishReloadAnim(true);
                } else {
                    EndReload();
                }
                PendingFinishReload = true;
                return;
            }
            if(UseBlocked)
                return;

            if(!IsServer) {
                UseInternal(slot);
            }
            UseServerRpc(slot);
        }

        private void UseInternal(EquipmentSlot slot)
        {
            if(slot != CurrentSlot.SlotID) {
                InstantSwap(slot);
            }
            
            executor.UseEquippable();
            Callbacks.Access.OnUse(token, this, CurrentSlot.Equippable, CurrentSlot.Ammo);
        }

        public void LateUse(EquipmentSlot slot = EquipmentSlot.None)
        {
            if(!IsOwner)
                return;
            
            if(slot == EquipmentSlot.None) {
                slot = CurrentSlot.SlotID;
            }
            if(CurrentSlot.IsEmpty)
                return;

            if(!IsServer) {
                LateUseInternal(slot);
            }
            LateUseServerRpc(slot);
        }

        private void LateUseInternal(EquipmentSlot slot)
        {
            if(slot != CurrentSlot.SlotID) {
                InstantSwap(slot);
            }
            
            executor.UseEquippableLate();
        }

        public void Reload()
        {
            if(!IsOwner)
                return;

            if(!IsServer) {
                ReloadInternal();
            }
            ReloadServerRpc();
        }

        public void BeginReload()
        {
            if(!IsOwner || ReloadBlocked)
                return;

            if(!IsServer) {
                BeginReloadInternal();
            }
            BeginReloadServerRpc();
        }

        private void BeginReloadInternal()
        {
            if(CurrentSlot.IsEmpty)
                return;

            IsReloading          = true;
            IStatsReload reload  = CurrentSlot.GetStatsInterface<IStatsReload>();
            float reloadTime     = reload.GetFullReloadTime(mult: CurEntityStats.GetParam(ReloadSpeedMult.Instance));
            float reloadSpeed    = reload.GetFullReloadClipTime() / reloadTime;
            CurAntiInterruptTime = reload.AntiReloadInterrupt;
            Model.SetReloadAnim(true, reloadSpeed);
            Callbacks.Access.OnBeginReload(token, this, CurrentSlot.Equippable);
        }

        public void EndReload()
        {
            if(!IsOwner)
                return;

            if(!IsServer) {
                EndReloadInternal();
            }
            EndReloadServerRpc();
        }

        private void EndReloadInternal()
        {
            IStatsReload iReload = CurrentSlot.GetStatsInterface<IStatsReload>();
            PendingFinishReload  = false;
            IsReloading          = false;
            CurAntiInterruptTime = -1.0f;
            UseCooldown          = 0.0f;
            if(iReload != null) {
                UseCooldown = iReload.ReloadUseDelay;
            }
            Model.SetFinishReloadAnim(false);
            Model.SetReloadAnim(false, 1.0f);
        }

        private void ReloadInternal()
        {
            EquippableStats stats = CurrentSlot.GetStatsAs<EquippableStats>();
            IStatsReload reload   = CurrentSlot.GetStatsInterface<IStatsReload>();
            bool single           = reload.ReloadIndividually;
            ushort curClip        = CurrentSlot.Ammo;
            ushort clip           = single ? (ushort)(curClip + 1) : stats.AmmoCapacity;
            Callbacks.Access.OnReload(token, this, CurrentSlot.Equippable, clip - curClip);
            if(!IsOwner) {
                if(!single || clip < stats.AmmoCapacity)
                    return;

                if(reload.ReloadEndTime > 0.0001f) {
                    Model.SetFinishReloadAnim(true);
                } else {
                    EndReload();
                }
                return;
            }
            
            GetNetInventorySlot(CurrentSlot.SlotID).Value = new NetInventorySlot(
                CurrentSlot.SlotID,
                CurrentSlot.Equippable.ID,
                clip
            );
            CurrentSlot.Ammo = clip;
            if(!single || clip < stats.AmmoCapacity)
                return;

            if(reload.ReloadEndTime > 0.0001f) {
                Model.SetFinishReloadAnim(true);
            } else {
                EndReload();
            }
        }

        public void SetupEquippedModel(string equippable, float suppressIKTime)
        {
            if(Model == null)
                return;
            
            RemoveEquippedModel();
            if(string.IsNullOrEmpty(equippable)) {
                Model.SetupEquippedWeapon(null, 0.1f, !IsReloading);
                return;
            }
            
            var model = EquippableManager.CloneModel<EquippableThirdPersonModel>(Controller, equippable, ModelPerspective.ThirdPerson);
            if(model != null) {
                ThirdPersonEquippableModel = model;
            }
            Model.SetupEquippedWeapon(model, suppressIKTime, !IsReloading);
        }

        public void ClearHolsteredModels()
        {
            foreach(EquippableHolsteredModel model in HolsteredEquippableModels) {
                Destroy(model.gameObject);
            }
            HolsteredEquippableModels.Clear();
        }

        public void SetupHolsteredModels()
        {
            ClearHolsteredModels();
            if(Model == null)
                return;
            
            foreach(InventorySlot slot in InventorySlotArray) {
                if(slot == CurrentSlot || slot.IsEmpty)
                    continue;

                var equip = slot.Equippable.EquippableKey;
                var model = EquippableManager.CloneModel<EquippableHolsteredModel>(Controller, equip, ModelPerspective.Holstered);
                if(model != null) {
                    HolsteredEquippableModels.Add(model);
                }
                Model.SetupHolsteredModel(model);
            }
        }

        private void RemoveEquippedModel()
        {
            if(ThirdPersonEquippableModel != null) {
                Destroy(ThirdPersonEquippableModel.gameObject);
            }
        }
        
        private void AssignModelPlugCopyData()
        {
            foreach(KeyValuePair<Equippable,QList<CopyableModelPlug.CopyData>> copy in modelPlugCopies) {
                if(!ReferenceEquals(ThirdPersonEquippableModel, null) && ThirdPersonEquippableModel.EquippableTemplate.ID == copy.Key.ID) {
                    ThirdPersonEquippableModel.Sockets.ApplyCopyablePlugs(copy.Value);
                    continue;
                }

                foreach(EquippableHolsteredModel model in HolsteredEquippableModels) {
                    if(!ReferenceEquals(model.Sockets, null) && model.EquippableTemplate.ID == copy.Key.ID) {
                        model.Sockets.ApplyCopyablePlugs(copy.Value);
                    }
                }
            }
        }
        
        public void ClearVFX(ushort effectID, ModelPlugRemoveBehavior removeBehavior = ModelPlugRemoveBehavior.None)
        {
            ThirdPersonEquippableModel.ClearVFX(effectID, removeBehavior);
            foreach(EquippableHolsteredModel model in HolsteredEquippableModels) {
                model.ClearVFX(effectID, removeBehavior);
            }
        }

        private void StoreModelPlugCopyData()
        {
            modelPlugCopies.Clear();
            if(ThirdPersonEquippableModel != null && !ReferenceEquals(ThirdPersonEquippableModel.Sockets, null)) {
                modelPlugCopies.Add(ThirdPersonEquippableModel.EquippableTemplate, GetModelPlugCopyData(ThirdPersonEquippableModel));
            }
            foreach(EquippableHolsteredModel model in HolsteredEquippableModels) {
                if(model != null && !ReferenceEquals(model.Sockets, null)) {
                    modelPlugCopies.Add(model.EquippableTemplate, GetModelPlugCopyData(model));
                }
            }
        }

        private QList<CopyableModelPlug.CopyData> GetModelPlugCopyData(EquippableModel model)
        {
            // todo: optimise.
            
            QList<CopyableModelPlug.CopyData> copyData = new();
            if(model == null || ReferenceEquals(model.Sockets, null))
                return copyData;
            
            model.Sockets.GetCopyablePlugs(copyData);
            return copyData;
        }

        public void SetCurrentSlot(EquipmentSlot slot)
        {
            InventorySlot currentSlot = CurrentSlot;
            InventorySlot nextSlot    = GetInventorySlot(slot);
            localCurrentSlot          = (byte)slot;
            UseCooldown               = 0.0f;
            IsReloading               = false;
            if(!CurrentSlot.IsEmpty) {
                CurrentSlot.Equippable.UnregisterFromProjectileProvider(Projectiles);
            }
            
            Model.SetReloadAnim(false, 1.0f);
            swapCts?.Cancel();
            swapCts?.Dispose();
            swapCts = new CancellationTokenSource();
            _       = SwapTask(currentSlot, nextSlot, swapCts.Token);
            if(!nextSlot.IsEmpty) {
                nextSlot.Equippable.RegisterToProjectileProvider(Projectiles);
            }
            Callbacks.Access.OnSwap(token, this, currentSlot.IsEmpty ? null : currentSlot.Equippable, nextSlot.IsEmpty ? null : nextSlot.Equippable);
        }

        public void SetEquippableForSlot(EquipmentSlot slot, string newEquippable, bool syncNetVar = true)
        {
            if(slot == EquipmentSlot.None)
                return;
            
            InventorySlot iSlot = GetInventorySlot(slot);
            string currentEquippable = CurrentSlot.IsEmpty 
                ? string.Empty
                : CurrentSlot.Equippable.EquippableKey;
            iSlot.Equippable = newEquippable == string.Empty
                ? null 
                : EquippableManager.GetTemplate(newEquippable);

            if(IsOwner && syncNetVar && iSlot.Equippable != null) {
                GetNetInventorySlot(slot).Value = new NetInventorySlot(
                    slot,
                    iSlot.Equippable.ID,
                    iSlot.Ammo
                );
            }
            if(iSlot == CurrentSlot && iSlot.IsEmpty) {
                // New item is null.
                SetCurrentSlot(EquipmentSlot.None);
            } else if(iSlot == CurrentSlot && currentEquippable != newEquippable) {
                // New item is not null/empty.
                SetCurrentSlot(slot);
            }
        }

        public void SetAmmoForSlot(EquipmentSlot slot, ushort ammo, bool syncNetVar = true)
        {
            if(slot == EquipmentSlot.None)
                return;

            InventorySlot iSlot = GetInventorySlot(slot);
            iSlot.Ammo          = ammo;
            if(IsOwner && syncNetVar) {
                GetNetInventorySlot(slot).Value = new NetInventorySlot(
                    iSlot.SlotID,
                    iSlot.Equippable.ID,
                    ammo
                );
            }
        }

        public void SwapToPrevious()
        {
            SwapEquipmentSlot(lastSlot);
        }

        private bool IsSpecialSlot(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.Special1 || slot == EquipmentSlot.Special2 || slot == EquipmentSlot.Special3;
        }
        
        public bool TryGetEquippable(string key, out Equippable equippable)
        {
            equippable = null;
            foreach(InventorySlot i in InventorySlotArray) {
                if(i.Equippable.EquippableKey != key)
                    continue;

                equippable = i.Equippable;
                return true;
            }
            return false;
        }

        public bool TryGetEquippable(ushort id, out Equippable equippable)
        {
            equippable = null;
            foreach(InventorySlot i in InventorySlotArray) {
                if(i.IsEmpty || i.Equippable.ID != id)
                    continue;

                equippable = i.Equippable;
                return true;
            }
            return false;
        }
        
        // ReSharper disable UnusedParameter.Local
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void UseServerRpc(EquipmentSlot slot, RpcParams rpcParams = default)
        {
            UseInternal(slot);
            UseClientRpc(slot);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void LateUseServerRpc(EquipmentSlot slot, RpcParams rpcParams = default)
        {
            LateUseInternal(slot);
            LateUseClientRpc(slot);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void BeginReloadServerRpc(RpcParams rpcParams = default)
        {
            BeginReloadInternal();
            BeginReloadClientRpc();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
        private void EndReloadServerRpc(RpcParams rpcParams = default)
        {
            EndReloadInternal();
            EndReloadClientRpc();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void ReloadServerRpc(RpcParams rpcParams = default)
        {
            ReloadInternal();
            ReloadClientRpc();
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void UseClientRpc(EquipmentSlot slot, RpcParams rpcParams = default)
        {
            if(IsOwner || IsServer)
                return;

            UseInternal(slot);
        }
        
        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void LateUseClientRpc(EquipmentSlot slot, RpcParams rpcParams = default)
        {
            if(IsOwner || IsServer)
                return;

            LateUseInternal(slot);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void BeginReloadClientRpc(RpcParams rpcParams = default)
        {
            if(IsOwner || IsServer)
                return;

            BeginReloadInternal();
        }
        
        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        private void EndReloadClientRpc(RpcParams rpcParams = default)
        {
            if(IsOwner || IsServer)
                return;

            EndReloadInternal();
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void ReloadClientRpc(RpcParams rpcParams = default)
        {
            if(IsOwner || IsServer)
                return;

            ReloadInternal();
        }
        
        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Unreliable)]
        private void SetEquippableOwnerRpc(EquipmentSlot slot, ushort equippable, ushort ammo)
        {
            if(!EquippableManager.TryGetKey(equippable, out string key))
                return;
            
            switch(slot) {
                case EquipmentSlot.Primary:
                    UpdateSlot(netPrimarySlot, key, ammo);
                    break;
                case EquipmentSlot.Secondary:
                    UpdateSlot(netSecondarySlot, key, ammo);
                    break;
                case EquipmentSlot.Melee:
                    UpdateSlot(netMeleeSlot, key, ammo);
                    break;
                case EquipmentSlot.Special1:
                    UpdateSlot(netSpecialSlot1, key, ammo);
                    break;
                case EquipmentSlot.Special2:
                    UpdateSlot(netSpecialSlot2, key, ammo);
                    break;
                case EquipmentSlot.Special3:
                    UpdateSlot(netSpecialSlot3, key, ammo);
                    break;
                
                case EquipmentSlot.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
            }
        }
        
        // ReSharper restore UnusedParameter.Local
        
    }
    
    public enum SlotCycleDirection { Up, Down }

    public enum EquipmentSlot : byte
    {
        None      = 0,
        Primary   = 1,
        Secondary = 2,
        Melee     = 3,
        Special1  = 4,
        Special2  = 5,
        Special3  = 6
    }

}
