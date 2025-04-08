using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Actions;
using OathFramework.Persistence;
using OathFramework.Utility;
using System;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.AbilitySystem
{
    [RequireComponent(typeof(Entity))]
    public partial class AbilityHandler : NetLoopComponent, 
        ILoopLateUpdate, IPersistableComponent, IEntityInitCallback, 
        IEntityDieCallback, IEntityStaggerCallback
    {
        [SerializeField] private UseAbility action;
        
        private LockableList<EntityAbility> abilities = new();
        private QList<EntityAbility> abilitiesCopy    = new();

        private QList<EntityAbility> syncData = new();
        private NetworkVariable<SyncData> netSyncData = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner,
            value: new SyncData(new QList<EntityAbility>())
        );

        private Ability queuedAbility;
        private AccessToken accessToken;
        
        public Entity Entity                     { get; protected set; }
        public AbilityHandlerCallbacks Callbacks { get; private set; }
        
        uint ILockableOrderedListElement.Order => 10_000;

        protected virtual void Awake()
        {
            Callbacks   = new AbilityHandlerCallbacks(this);
            Entity      = GetComponent<Entity>();
            accessToken = Callbacks.Access.GenerateAccessToken();
        }
        
        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityStaggerCallback)this);
            entity.Callbacks.Register((IEntityDieCallback)this);
        }
        
        void IEntityStaggerCallback.OnStagger(Entity entity, StaggerStrength strength, Entity instigator)
        {
            if(action != null) {
                entity.EntityModel.Animator.ResetTrigger(action.AnimNameHash);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(Entity.IsDummy || IsOwner)
                return;
            
            syncData = netSyncData.Value.Data;
            for(int i = 0; i < syncData.Count; i++) {
                SetAbility(syncData.Array[i], true, true);
            }
            netSyncData.OnValueChanged += OnSyncDataChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if(IsOwner)
                return;

            netSyncData.OnValueChanged -= OnSyncDataChanged;
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            foreach(EntityAbility ea in abilities.Current) {
                ea.Ability.Removed(Entity, !IsOwner, false);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach(EntityAbility ea in abilities.Current) {
                ea.Ability.Added(Entity, !IsOwner, false);
            }
        }
        
        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            syncData.Clear();
            abilities.Lock();
            foreach(EntityAbility ea in abilities.Current) {
                if(!ea.Ability.RemoveOnDeath)
                    continue;
                
                ea.Ability.Removed(entity, !IsOwner, false);
                abilities.Remove(ea);
            }
            abilities.Unlock();
        }
        
        private void OnSyncDataChanged(SyncData previous, SyncData current)
        {
            syncData = current.Data;
            abilitiesCopy.Clear();
            abilitiesCopy.AddRange(abilities.Current);
            
            // Process removed abilities.
            for(int i = 0; i < abilitiesCopy.Count; i++) {
                Ability ability = abilitiesCopy.Array[i].Ability;
                if(ability.AutoNetSync && !SyncDataContains(ability)) {
                    RemoveAbility(new EntityAbility(ability), true, false);
                }
            }
            
            // Process changed or added abilities.
            for(int i = 0; i < syncData.Count; i++) {
                EntityAbility eAbility = syncData.Array[i];
                if(eAbility.Ability == null)
                    continue;
                
                SetAbility(eAbility, true, false);
            }
            return;
            
            bool SyncDataContains(Ability ability)
            {
                for(int i = 0; i < syncData.Count; i++) {
                    EntityAbility eAbility = syncData.Array[i];
                    if(eAbility.Ability.Equals(ability))
                        return true;
                }
                return false;
            }
        }

        public virtual void LoopLateUpdate()
        {
            if(Entity.IsDummy || !Entity.IsOwner)
                return;
            
            abilities.Lock();
            bool changed = false;
            for(int i = 0; i < abilities.Count; i++) {
                EntityAbility eb = abilities[i];
                Ability a        = eb.Ability;
                if(!a.HasCooldown || !a.AutoCooldownHandling || Mathf.Approximately(eb.Cooldown, 0.0f))
                    continue;

                SetCooldown(a, Mathf.Clamp(eb.Cooldown - Time.deltaTime, 0.0f, a.GetMaxCooldown(Entity)), false);
                changed = true;
            }
            abilities.Unlock();
            if(changed) {
                UpdateSyncData();
            }
        }

        public bool TryGetEntityAbility(Ability ability, out EntityAbility entityAbility)
        {
            entityAbility = default;
            foreach(EntityAbility ea in abilities.Current) {
                if(ea.ID != ability.ID)
                    continue;

                entityAbility = ea;
                return true;
            }
            return false;
        }

        public bool TryGetEntityAbility(Ability ability, out int index, out EntityAbility entityAbility)
        {
            index         = -1;
            entityAbility = default;
            int count = abilities.Count;
            for(int i = 0; i < count; i++) {
                EntityAbility ea = abilities.Current[i];
                if(ea.ID != ability.ID)
                    continue;

                index         = i;
                entityAbility = ea;
                return true;
            }
            return false;
        }

        public bool HasAbility(Ability ability) 
            => TryGetEntityAbility(ability, out EntityAbility _);
        public bool IsActive(Ability ability)
            => TryGetEntityAbility(ability, out EntityAbility _) && ability.GetIsActive(Entity);
        public bool IsUsable(Ability ability)
            => TryGetEntityAbility(ability, out EntityAbility _) && ability.GetUsable(Entity);
        public float GetCooldown(Ability ability)
            => !TryGetEntityAbility(ability, out EntityAbility eb) ? -1.0f : eb.Cooldown;
        public float GetMaxCooldown(Ability ability)
            => !TryGetEntityAbility(ability, out EntityAbility _) ? -1.0f : ability.GetMaxCooldown(Entity);
        public float GetMaxChargeProgress(Ability ability)
            => !TryGetEntityAbility(ability, out EntityAbility _) ? -1.0f : ability.GetMaxChargeProgress(Entity);
        public byte GetMaxCharges(Ability ability)
            => !TryGetEntityAbility(ability, out EntityAbility _) ? (byte)0 : ability.GetMaxCharges(Entity);
        public byte GetCharges(Ability ability)
            => !TryGetEntityAbility(ability, out EntityAbility eb) ? (byte)0 : eb.Charges;
        public float GetChargeProgress(Ability ability)
            => !TryGetEntityAbility(ability, out EntityAbility eb) ? 0 : eb.ChargeProgress;

        public void SetCharges(Ability ability, byte charges, bool sync = true)
        {
            if(!TryGetEntityAbility(ability, out int index, out EntityAbility eb) || !eb.Ability.HasCharges)
                return;

            abilities.Current[index] = new EntityAbility(eb.Ability, eb.Cooldown, eb.ChargeProgress, charges);
            if(sync && ability.AutoNetSync && IsOwner) {
                UpdateSyncData();
            }
        }

        public void DecrementCharge(Ability ability, bool sync = true)
        {
            if(!TryGetEntityAbility(ability, out int index, out EntityAbility eb) || !eb.Ability.HasCharges || eb.Charges == 0)
                return;

            byte newCharges = (byte)Mathf.Clamp(eb.Charges - 1, 0, byte.MaxValue);
            abilities.Current[index] = new EntityAbility(eb.Ability, eb.Cooldown, eb.ChargeProgress, newCharges);
            if(sync && ability.AutoNetSync && IsOwner) {
                UpdateSyncData();
            }
            Callbacks.Access.OnAbilityChargeDecrement(accessToken, eb.Ability, !IsOwner);
        }

        public void SetChargeProgress(Ability ability, float chargeProgress, bool sync = true)
        {
            if(!TryGetEntityAbility(ability, out int index, out EntityAbility eb) || !eb.Ability.HasCharges)
                return;

            abilities.Current[index] = new EntityAbility(eb.Ability, eb.Cooldown, chargeProgress, eb.Charges);
            if(sync && ability.AutoNetSync && IsOwner) {
                UpdateSyncData();
            }
        }

        public void AddChargeProgress(float amt, Ability ability = null)
        {
            if(!IsOwner)
                return;
            
            if(ability != null) {
                if(!ability.HasCharges 
                   || (!ability.ChargeWhenActive && ability.GetIsActive(Entity)) 
                   || !TryGetEntityAbility(ability, out int index, out EntityAbility eb))
                    return;
                
                AddChargeProgressInternal(amt, eb, index);
                return;
            }
            for(int i = 0; i < abilities.Count; i++) {
                EntityAbility eb = abilities[i];
                if(!eb.Ability.HasCharges || (!eb.Ability.ChargeWhenActive && eb.Ability.GetIsActive(Entity)))
                    continue;
                
                AddChargeProgressInternal(amt, abilities[i], i);
            }
        }

        private void AddChargeProgressInternal(float amt, EntityAbility eb, int index)
        {
            bool changed    = false;
            float remaining = amt;
            Ability a       = eb.Ability;
            while(remaining > 0.0f && eb.Charges < GetMaxCharges(a)) {
                changed    = true;
                float add  = Mathf.Min(amt, GetMaxChargeProgress(a) - eb.ChargeProgress);
                remaining -= add;
                if(Mathf.Approximately(eb.ChargeProgress + add, GetMaxChargeProgress(a))) {
                    abilities.Current[index] = new EntityAbility(a, eb.Cooldown, 0.0f, (byte)(eb.Charges + 1));
                    // TODO: On ability charged callback and RPCs.
                } else {
                    abilities.Current[index] = new EntityAbility(a, eb.Cooldown, eb.ChargeProgress + add, eb.Charges);
                }
            }
            if(changed) {
                UpdateSyncData();
            }
        }
        
        public void SetCooldown(Ability ability, float cooldown, bool sync = true)
        {
            if(!TryGetEntityAbility(ability, out int index, out EntityAbility eb) || !eb.Ability.HasCooldown)
                return;

            abilities.Current[index] = new EntityAbility(eb.Ability, cooldown, eb.ChargeProgress, eb.Charges);
            if(sync && ability.AutoNetSync && IsOwner) {
                UpdateSyncData();
            }
        }

        public float GetCooldownRatio(Ability ability)
        {
            if(!TryGetEntityAbility(ability, out EntityAbility eb))
                return -1.0f;
            if(!eb.Ability.HasCooldown)
                return 1.0f;
            
            float max = ability.GetMaxCooldown(Entity);
            float cur = ability.GetCooldown(Entity);
            return max <= 0.0f || cur <= 0.0f ? 0.0f : cur / max;
        }
        
        public float GetChargeProgressRatio(Ability ability)
        {
            if(!TryGetEntityAbility(ability, out EntityAbility eb) || !eb.Ability.HasCooldown)
                return -1.0f;

            float max = ability.GetMaxChargeProgress(Entity);
            float cur = eb.ChargeProgress;
            return max <= 0.0f || cur <= 0.0f ? 0.0f : cur / max;
        }

        public void ActivateQueuedAbility()
        {
            if(ReferenceEquals(queuedAbility, null))
                return;
            
            ActivateAbilityInternal(queuedAbility, !IsOwner, false);
            if(IsOwner && ((IActionAbility)queuedAbility).SyncActivation) {
                ActivateAbilityImmediateNotOwnerRpc(queuedAbility.ID);
            }
            queuedAbility = null;
        }

        public void EndQueuedAbility()
        {
            if(ReferenceEquals(queuedAbility, null))
                return;
            
            ((IActionAbility)queuedAbility).OnCancelled(Entity, !IsOwner, false);
            Callbacks.Access.OnAbilityCancelled(accessToken, queuedAbility, !IsOwner);
            queuedAbility = null;
        }
        
        public void ActivateAbility(Ability ability)
        {
            if(ReferenceEquals(ability, null)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(ability)));
                }
                return;
            }
            if(ability is IActionAbility actionAbility) {
                if(!ActionAnimParamsManager.TryGet(actionAbility.ActionAnimParams, out ActionAnimParams @params)) {
                    if(Game.ExtendedDebug) {
                        Debug.LogError($"No {nameof(ActionAnimParams)} found for '{actionAbility.ActionAnimParams}'");
                    }
                    return;
                }
                
                queuedAbility = ability;
                actionAbility.OnInvoked(Entity, !IsOwner, false);
                Callbacks.Access.OnAbilityInvoked(accessToken, queuedAbility, !IsOwner);
                action.SetParams(this, @params);
                Entity.Actions.InvokeAction(action, !IsOwner);
                if(IsOwner) {
                    ActivateAbilityNotOwnerRpc(ability.ID);
                }
                return;
            }
            if(!IsOwner)
                return;
            
            if(!ActivateAbilityInternal(ability, false, false)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Entity does not have ability with ID '{ability.ID}'");
                }
                return;
            }
            ActivateAbilityNotOwnerRpc(ability.ID);
        }

        private bool ActivateAbilityInternal(Ability ability, bool auxOnly, bool lateJoin)
        {
            EntityAbility? eb = null;
            int index         = 0;
            foreach(EntityAbility a in abilities.Current) {
                if(ability.ID != a.ID) {
                    index++;
                    continue;
                }
                eb = a;
                break;
            }
            
            // EntityAbility could also be null if packet loss or lag occurs.
            if(eb == null)
                return false;

            EntityAbility unpacked = eb.Value;
            Ability foundAbility   = unpacked.Ability;
            foundAbility.Activate(Entity, auxOnly, lateJoin);
            Callbacks.Access.OnAbilityActivated(accessToken, foundAbility, !IsOwner);
            if(foundAbility.IsInstant) {
                foundAbility.Deactivate(Entity, auxOnly);
                Callbacks.Access.OnAbilityDeactivated(accessToken, foundAbility, !IsOwner);
            }
            if(!IsOwner)
                return true;

            if(foundAbility.AutoChargeDecrement) {
                abilities[index] = new EntityAbility(
                    foundAbility,
                    foundAbility.HasCooldown ? foundAbility.GetMaxCooldown(Entity) : 0.0f,
                    foundAbility.HasCooldown ? unpacked.ChargeProgress : 0.0f,
                    foundAbility.HasCharges ? (byte)Mathf.Clamp(unpacked.Charges - 1, 0, foundAbility.GetMaxCharges(Entity)) : (byte)1
                );
                Callbacks.Access.OnAbilityChargeDecrement(accessToken, foundAbility, !IsOwner);
            }
            UpdateSyncData();
            return true;
        }
        
        public void DeactivateAbility(Ability ability)
        {
            if(!IsOwner)
                return;
            
            if(ReferenceEquals(ability, null)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(ability)));
                }
                return;
            }
            if(!DeactivateAbilityInternal(ability, false)) {
                Debug.LogError($"Entity does not have ability with ID '{ability.ID}'");
                return;
            }
            DeactivateAbilityNotOwnerRpc(ability.ID);
        }

        private bool DeactivateAbilityInternal(Ability ability, bool auxOnly)
        {
            EntityAbility? eb    = null;
            foreach(EntityAbility a in abilities.Current) {
                if(ability.ID != a.ID)
                    continue;
                
                eb = a;
                break;
            }
            
            // EntityAbility could also be null if packet loss or lag occurs.
            if(eb == null)
                return false;

            EntityAbility unpacked = eb.Value;
            unpacked.Ability.Deactivate(Entity, auxOnly);
            Callbacks.Access.OnAbilityDeactivated(accessToken, unpacked.Ability, !IsOwner);
            return true;
        }

        public void AddAbility(EntityAbility eAbility, bool auxOnly, bool lateJoin)
        {
            if(eAbility.Ability == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(eAbility)));
                }
                return;
            }
            if(TryGetEntityAbility(eAbility.Ability, out int index, out EntityAbility _)) {
                abilities.Current[index] = eAbility;
                if(IsOwner && eAbility.Ability.AutoNetSync) {
                    UpdateSyncData();
                }
                return;
            }
            abilities.Add(eAbility);
            eAbility.Ability.Added(Entity, auxOnly, lateJoin);
            if(IsOwner && eAbility.Ability.AutoNetSync) {
                UpdateSyncData();
            }
        }

        public void SetAbility(EntityAbility eAbility, bool auxOnly, bool lateJoin)
        {
            if(eAbility.Ability == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(eAbility)));
                }
                return;
            }
            if(TryGetEntityAbility(eAbility.Ability, out int index, out EntityAbility _)) {
                abilities.Current[index] = eAbility;
                if(IsOwner && eAbility.Ability.AutoNetSync) {
                    UpdateSyncData();
                }
                return;
            }
            abilities.Add(eAbility);
            eAbility.Ability.Added(Entity, auxOnly, lateJoin);
            if(IsOwner && eAbility.Ability.AutoNetSync) {
                UpdateSyncData();
            }
        }

        public void RemoveAbility(EntityAbility eAbility, bool auxOnly, bool lateJoin)
        {
            if(eAbility.Ability == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(eAbility)));
                }
                return;
            }
            if(!TryGetEntityAbility(eAbility.Ability, out int index, out EntityAbility _))
                return;

            if(IsActive(eAbility.Ability)) {
                DeactivateAbilityInternal(eAbility.Ability, auxOnly);
            }
            abilities.RemoveAt(index);
            eAbility.Ability.Removed(Entity, auxOnly, lateJoin);
            if(IsOwner && eAbility.Ability.AutoNetSync) {
                UpdateSyncData();
            }
        }

        public void ClearAbilities(bool auxOnly)
        {
            abilitiesCopy.Clear();
            abilitiesCopy.AddRange(abilities.Current);
            abilities.Clear();
            abilities.Lock();
            for(int i = 0; i < abilitiesCopy.Count; i++) {
                EntityAbility eAbility = new(abilitiesCopy.Array[i].Ability);
                RemoveAbility(eAbility, auxOnly, false);
            }
            abilities.Unlock();
            abilitiesCopy.Clear();
            UpdateSyncData();
        }

        public void UpdateSyncData()
        {
            if(!IsOwner)
                return;
            
            syncData.Clear();
            foreach(EntityAbility eAbility in abilities.Current) {
                if(!eAbility.Ability.AutoNetSync)
                    continue;

                syncData.Add(eAbility);
            }
            netSyncData.Value = new SyncData(syncData);
            netSyncData.SetDirty(true);
        }
        
        private async UniTask SyncPersistenceTask(SyncData data)
        {
            await Entity.WaitForNetInitialization();
            int count = data.Data.Count;
            for(int i = 0; i < count; i++) {
                SetAbility(data.Data.Array[i], true, true);
            }
        }
        
        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        private void SyncPersistenceOwnerRpc(SyncData data)
        {
            _ = SyncPersistenceTask(data);
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Reliable)]
        private void ActivateAbilityImmediateNotOwnerRpc(ushort abilityID, RpcParams rpcParams = default)
        {
            if(!AbilityManager.TryGet(abilityID, out Ability ability))
                return;
            
            ActivateAbilityInternal(ability, true, false);
        }
        
        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Reliable)]
        private void ActivateAbilityNotOwnerRpc(ushort abilityID, RpcParams rpcParams = default)
        {
            if(!AbilityManager.TryGet(abilityID, out Ability ability))
                return;

            // Play animation for IActionAbility.
            if(ability is IActionAbility) {
                ActivateAbility(ability);
                return;
            }
            
            // Execute ability effects instantly, if it's not an IActionAbility.
            ActivateAbilityInternal(ability, true, false);
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Reliable)]
        private void DeactivateAbilityNotOwnerRpc(ushort abilityID, RpcParams rpcParams = default)
        {
            if(!AbilityManager.TryGet(abilityID, out Ability ability))
                return;
            
            DeactivateAbilityInternal(ability, true);
        }
        
        private struct SyncData : INetworkSerializable, IEquatable<SyncData>
        {
            public QList<EntityAbility> Data { get; private set; }
            
            public SyncData(QList<EntityAbility> data)
            {
                Data = data;
            }
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                Data ??= new QList<EntityAbility>();
                if(serializer.IsReader) {
                    FastBufferReader reader = serializer.GetFastBufferReader();
                    reader.ReadValueSafe(out byte len);
                    Data.Clear();
                    for(byte i = 0; i < len; i++) {
                        reader.ReadValueSafe(out ushort id);
                        reader.ReadValueSafe(out float cooldown);
                        reader.ReadValueSafe(out float chargeProgress);
                        reader.ReadValueSafe(out byte charges);
                        Data.Add(new EntityAbility(id, cooldown, chargeProgress, charges));
                    }
                } else {
                    FastBufferWriter writer = serializer.GetFastBufferWriter();
                    writer.WriteValueSafe((byte)Data.Count);
                    for(byte i = 0; i < Data.Count; i++) {
                        EntityAbility ability = Data.Array[i];
                        writer.WriteValueSafe(ability.ID);
                        writer.WriteValueSafe(ability.Cooldown);
                        writer.WriteValueSafe(ability.ChargeProgress);
                        writer.WriteValueSafe(ability.Charges);
                    }
                }
            }

            public bool Equals(SyncData other)
            {
                if(Data == null || Data.Count != other.Data.Count)
                    return false;

                for(int i = 0; i < Data.Count; i++) {
                    EntityAbility ability      = Data.Array[i];
                    EntityAbility otherAbility = other.Data.Array[i];
                    if(!ability.Equals(otherAbility) 
                       || !Mathf.Approximately(ability.Cooldown, otherAbility.Cooldown)
                       || !Mathf.Approximately(ability.ChargeProgress, otherAbility.ChargeProgress)
                       || ability.Charges != otherAbility.Charges)
                        return false;
                }
                return true;
            }
        }
    }

    public readonly struct EntityAbility : IEquatable<EntityAbility>
    {
        public Ability Ability      { get; }
        public float Cooldown       { get; }
        public float ChargeProgress { get; }
        public byte Charges         { get; }
        public string LookupKey     => Ability.LookupKey;
        public ushort ID            => Ability.ID;
        
        public EntityAbility(Ability ability)
        {
            Ability        = ability;
            Cooldown       = -1.0f;
            ChargeProgress = -1.0f;
            Charges        = byte.MaxValue;
        }
        
        public EntityAbility(Ability ability, Entity entity)
        {
            Ability        = ability;
            Cooldown       = 0.0f;
            ChargeProgress = 0.0f;
            Charges        = ability.GetMaxCharges(entity);
        }
        
        public EntityAbility(Ability ability, float cooldown, float chargeProgress, byte charges)
        {
            Ability        = ability;
            Cooldown       = cooldown;
            ChargeProgress = chargeProgress;
            Charges        = charges;
        }

        public EntityAbility(string abilityKey, float cooldown, float chargeProgress, byte charges)
        {
            Cooldown       = cooldown;
            ChargeProgress = chargeProgress;
            Charges        = charges;
            if(!AbilityManager.TryGet(abilityKey, out Ability ability)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(abilityKey)));
                }
                Ability = null;
                return;
            }
            Ability = ability;
        }

        public EntityAbility(ushort abilityID, float cooldown, float chargeProgress, byte charges)
        {
            Cooldown       = cooldown;
            ChargeProgress = chargeProgress;
            Charges        = charges;
            if(!AbilityManager.TryGet(abilityID, out Ability ability)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(abilityID)));
                }
                Ability = null;
                return;
            }
            Ability = ability;
        }

        public bool Equals(EntityAbility other) 
            => ID == other.ID 
               && Cooldown.Equals(other.Cooldown) 
               && ChargeProgress.Equals(other.ChargeProgress) 
               && Charges == other.Charges;

        public override bool Equals(object obj) => obj is EntityAbility other && Equals(other);
        public override int GetHashCode() => ID;
    }
}
