using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Persistence;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.States
{
    public sealed partial class StateHandler : NetLoopComponent, 
        ILoopLateUpdate, IPersistableComponent, IEntityInitCallback,
        IEntityDieCallback
    {
        private Entity entity;
        private LockableList<EntityState> states = new();
        private QList<EntityState> statesCopy    = new();
        private bool initialized;

        private QList<EntityState> syncData = new();
        private NetworkVariable<SyncData> netSyncData = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner,
            value: new SyncData(new QList<EntityState>())
        );
        
        uint ILockableOrderedListElement.Order => 10_000;
        
        private void Awake()
        {
            entity = GetComponent<Entity>();
        }
        
        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityDieCallback)this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(entity.IsDummy || IsOwner)
                return;

            syncData = netSyncData.Value.Data;
            for(int i = 0; i < syncData.Count; i++) {
                SetState(syncData.Array[i], applyStats: false, sendOwnerRpc: false);
            }
            ApplyStats();
            netSyncData.OnValueChanged += OnSyncDataChanged;
            initialized = true;
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
            foreach(EntityState es in states.Current) {
                es.State.Applied(entity, !IsOwner, es.Value, 0);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach(EntityState es in states.Current) {
                es.State.Removed(entity, false, 0, es.Value);
            }
        }
        
        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            syncData.Clear();
            states.Lock();
            foreach(EntityState es in states.Current) {
                if(!es.State.RemoveOnDeath)
                    continue;
                
                es.State.Removed(entity, false, 0, es.Value);
                states.Remove(es);
            }
            states.Unlock();
        }
        
        private void OnSyncDataChanged(SyncData previous, SyncData current)
        {
            syncData = current.Data;
            statesCopy.Clear();
            statesCopy.AddRange(states.Current);
            
            // Process removed states.
            for(int i = 0; i < statesCopy.Count; i++) {
                State state = statesCopy.Array[i].State;
                if(state.NetSync && !SyncDataContains(state)) {
                    RemoveState(new EntityState(state, ushort.MaxValue), resetDuration: false, applyStats: false, sendOwnerRpc: false);
                }
            }
            
            // Process changed or added states.
            for(int i = 0; i < syncData.Count; i++) {
                EntityState eState = syncData.Array[i];
                if(eState.State == null)
                    continue;
                
                SetState(eState, applyStats: false, sendOwnerRpc: false);
            }
            ApplyStats();
            return;
            
            bool SyncDataContains(State state)
            {
                for(int i = 0; i < syncData.Count; i++) {
                    EntityState eState = syncData.Array[i];
                    if(eState.State.ID == state.ID)
                        return true;
                }
                return false;
            }
        }
        
        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(entity.IsDummy || !IsOwner)
                return;
            
            states.Lock();
            for(int i = 0; i < states.Count; i++) {
                EntityState eState = states[i];
                if(eState.State.MaxDuration == null)
                    continue;
                
                states[i] = new EntityState(eState.State, eState.Value, eState.Duration - Time.deltaTime);
                if(eState.Duration > 0.0f)
                    continue;

                if(eState.State.RemoveAllValOnExpire) {
                    RemoveState(new EntityState(eState.State, ushort.MaxValue), false, sendOwnerRpc: false);
                } else {
                    RemoveState(new EntityState(eState.State, 1), sendOwnerRpc: false);
                }
            }
            states.Unlock();
        }

        public bool HasState(State state)
        {
            foreach(EntityState eState in states.Current) {
                if(eState.State.ID == state.ID)
                    return true;
            }
            return false;
        }

        public bool TryGetDuration(State state, out float duration)
        {
            duration = -1.0f;
            if(state?.MaxDuration == null)
                return false;
            
            foreach(EntityState eState in states.Current) {
                if(eState.State.ID == state.ID) {
                    duration = eState.Duration;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetValue(State state, out ushort value)
        {
            value = 0;
            if(ReferenceEquals(state, null))
                return false;
            
            foreach(EntityState eState in states.Current) {
                if(eState.State.ID == state.ID) {
                    value = eState.Value;
                    return true;
                }
            }
            return false;
        }

        public void ApplyStats(bool resetCurrent = false)
        {
            states.Current.Sort((x, y) => x.State.Order.CompareTo(y.State.Order));
            uint oldHealth    = entity.CurStats.health;
            ushort oldStamina = entity.CurStats.stamina;
            entity.BaseStats.CopyTo(entity.CurStats, resetCurrent);
            states.Lock();
            foreach(EntityState eState in states.Current) {
                eState.State.ApplyStatChanges(entity, !initialized, eState.Value);
            }
            states.Unlock();
            entity.CurStats.health = resetCurrent ? entity.CurStats.maxHealth : oldHealth;
            entity.CurStats.stamina = resetCurrent ? entity.CurStats.maxStamina : oldStamina;
        }

        public void ClearStates(bool resetCurrent = false)
        {
            statesCopy.AddRange(states.Current);
            states.Clear();
            states.Lock();
            while(statesCopy.Count > 0) {
                EntityState eState = new(statesCopy.Array[statesCopy.Count - 1].State, ushort.MaxValue);
                RemoveState(eState, false, sendOwnerRpc: false);
            }
            states.Unlock();
            statesCopy.Clear();
            ApplyStats(resetCurrent);
            UpdateSyncData();
        }
        
        public void SetState(
            EntityState eState,
            bool resetCurrent = false,
            bool applyStats   = true, 
            bool sendOwnerRpc = true)
        {
            if(eState.State == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(eState)));
                }
                return;
            }

            if(!entity.IsDummy && !IsOwner && sendOwnerRpc) {
                SetStateOwnerRpc(eState, resetCurrent, applyStats);
                return;
            }
            
            ushort value = (ushort)Math.Clamp(eState.Value, (ushort)0, eState.State.MaxValue);
            if(value == 0) {
                RemoveState(new EntityState(eState.State, ushort.MaxValue), true, resetCurrent, applyStats, sendOwnerRpc);
                return;
            }
            for(int i = 0; i < states.Count; i++) {
                if(states[i].State.ID != eState.State.ID)
                    continue;

                ushort oldVal = states[i].Value;
                if(oldVal == eState.Value)
                    return;
                
                states[i] = new EntityState(states[i].State, value, eState.Duration);
                if(applyStats) {
                    ApplyStats(resetCurrent);
                }
                CallApplied(oldVal);
                return;
            }

            states.Add(new EntityState(eState.State, value, eState.Duration));
            if(applyStats) {
                ApplyStats(resetCurrent);
            }
            CallApplied(null);
            return;

            void CallApplied(ushort? originalVal)
            {
                states.Lock();
                eState.State.Applied(entity, !initialized, value, originalVal);
                states.Unlock();
                UpdateSyncData();
            }
        }
        
        public void AddStates(List<EntityState> newStates, bool resetCurrent = false, bool applyStats = true)
        {
            foreach(EntityState pair in newStates) {
                AddState(pair, applyStats: false);
            }
            if(applyStats) {
                ApplyStats(resetCurrent);
            }
        }

        public void AddState(
            EntityState eState, 
            bool resetCurrent = false, 
            bool applyStats   = true, 
            bool sendOwnerRpc = true)
        {
            if(eState.State == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(eState)));
                }
                return;
            }

            if(!entity.IsDummy && !IsOwner && sendOwnerRpc) {
                AddStateOwnerRpc(eState, resetCurrent, applyStats);
                return;
            }
            
            ushort value = (ushort)Math.Clamp(eState.Value, (ushort)0, eState.State.MaxValue);
            for(int i = 0; i < states.Count; i++) {
                if(states[i].State.ID != eState.State.ID)
                    continue;

                State foundState = states[i].State;
                ushort oldVal    = states[i].Value;
                ushort newVal    = (ushort)Math.Clamp(oldVal + value, 0, foundState.MaxValue);
                states[i]        = new EntityState(foundState, newVal, eState.Duration);
                if(applyStats) { 
                    ApplyStats(resetCurrent);
                }
                CallApplied(oldVal);
                return;
            }

            states.Add(new EntityState(eState.State, value, eState.Duration));
            if(applyStats) { 
                ApplyStats(resetCurrent);
            }
            CallApplied(null);
            return;
            
            void CallApplied(ushort? originalVal)
            {
                states.Lock();
                eState.State.Applied(entity, !initialized, value, originalVal);
                states.Unlock();
                UpdateSyncData();
            }
        }

        public void RemoveState(
            EntityState eState,
            bool resetDuration = true,
            bool resetCurrent  = false,
            bool applyStats    = true, 
            bool sendOwnerRpc  = true)
        {
            if(eState.State == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(eState)));
                }
                return;
            }

            if(!entity.IsDummy && !IsOwner && sendOwnerRpc) {
                RemoveStateOwnerRpc(eState, resetDuration, resetCurrent, applyStats);
                return;
            }
            
            float duration = eState.Duration;
            if(resetDuration && eState.State.MaxDuration != null) {
                duration = eState.State.MaxDuration.Value;
            }
            
            ushort value = (ushort)Math.Clamp((int)eState.Value, 0, ushort.MaxValue);
            for(int i = 0; i < states.Count; i++) {
                if(states[i].State.ID != eState.State.ID)
                    continue;

                ushort oldVal = states[i].Value;
                ushort newVal = (ushort)Math.Clamp(states[i].Value - value, 0, ushort.MaxValue);
                if(newVal <= 0) {
                    states.RemoveAt(i);
                    if(applyStats) { 
                        ApplyStats(resetCurrent);
                    }
                    CallRemoved(oldVal);
                    return;
                }
                
                states[i] = new EntityState(states[i].State, newVal, duration);
                if(applyStats) { 
                    ApplyStats(resetCurrent);
                }
                CallRemoved(oldVal);
                return;
            }
            return;
            
            void CallRemoved(ushort originalVal)
            {
                states.Lock();
                eState.State.Removed(entity, !initialized, value, originalVal);
                states.Unlock();
                UpdateSyncData();
            }
        }
        
        public void UpdateSyncData()
        {
            if(!IsOwner || Game.State == GameState.Quitting)
                return;
            
            syncData.Clear();
            foreach(EntityState eState in states.Current) {
                if(!eState.State.NetSync)
                    continue;

                syncData.Add(eState);
            }
            netSyncData.Value = new SyncData(syncData);
            if(enabled) {
                netSyncData.SetDirty(true);
            }
        }

        private async UniTask SyncPersistenceTask(SyncData data)
        {
            await entity.WaitForNetInitialization();
            int count = data.Data.Count;
            for(int i = 0; i < count; i++) {
                SetState(data.Data.Array[i], false, false);
            }
            ApplyStats();
        }

        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        private void SetStateOwnerRpc(EntityState eState, bool resetCurrent, bool applyStats)
        {
            SetState(eState, resetCurrent, applyStats, false);
        }

        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        private void AddStateOwnerRpc(EntityState eState, bool resetCurrent = false, bool applyStats = true)
        {
            AddState(eState, resetCurrent, applyStats, false);
        }

        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        private void RemoveStateOwnerRpc(EntityState eState, bool resetDuration = true, bool resetCurrent = false, bool applyStats = true)
        {
            RemoveState(eState, resetDuration, resetCurrent, applyStats, false);
        }
        
        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        private void SyncPersistenceOwnerRpc(SyncData data)
        {
            _ = SyncPersistenceTask(data);
        }

        private struct SyncData : INetworkSerializable, IEquatable<SyncData>
        {
            public QList<EntityState> Data { get; private set; }
            
            public SyncData(QList<EntityState> data)
            {
                Data = data;
            }
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                Data ??= new QList<EntityState>();
                if(serializer.IsReader) {
                    FastBufferReader reader = serializer.GetFastBufferReader();
                    reader.ReadValueSafe(out byte len);
                    Data.Clear();
                    for(byte i = 0; i < len; i++) {
                        reader.ReadValueSafe(out ushort id);
                        reader.ReadValueSafe(out ushort val);
                        reader.ReadValueSafe(out float dur);
                        Data.Add(new EntityState(id, val, dur));
                    }
                } else {
                    FastBufferWriter writer = serializer.GetFastBufferWriter();
                    writer.WriteValueSafe((byte)Data.Count);
                    for(byte i = 0; i < (byte)Data.Count; i++) {
                        EntityState state = Data.Array[i];
                        writer.WriteValueSafe(state.ID);
                        writer.WriteValueSafe(state.Value);
                        writer.WriteValueSafe(state.Duration);
                    }
                }
            }

            public bool Equals(SyncData other)
            {
                if(Data == null || Data.Count != other.Data.Count)
                    return false;

                for(int i = 0; i < Data.Count; i++) {
                    EntityState state      = Data.Array[i];
                    EntityState otherState = other.Data.Array[i];
                    if(!state.Equals(otherState) || !Mathf.Approximately(state.Duration, otherState.Duration))
                        return false;
                }
                return true;
            }
        }
    }

    public struct EntityState : INetworkSerializable, IEquatable<EntityState>
    {
        public State State      { get; private set; }
        public ushort Value     { get; private set; }
        public float Duration   { get; private set; }
        public string LookupKey => State.LookupKey;
        public ushort ID        => State.ID;

        public EntityState(State state, ushort? value = null, float duration = -1.0f)
        {
            State    = state;
            Value    = value ?? state.MaxValue;
            Duration = Mathf.Approximately(duration, -1.0f) ? state.MaxDuration ?? -1.0f : duration;
        }

        public EntityState(string stateKey, ushort? value = null, float duration = -1.0f)
        {
            Value    = 0;
            Duration = duration;
            if(!StateManager.TryGet(stateKey, out State state)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(stateKey)));
                }
                State = null;
                return;
            }
            Value    = value ?? state.MaxValue;
            Duration = Mathf.Approximately(duration, -1.0f) ? state.MaxDuration ?? -1.0f : duration;
            State    = state;
        }

        public EntityState(ushort stateID, ushort? value = null, float duration = -1.0f)
        {
            Value    = 0;
            Duration = duration;
            if(!StateManager.TryGet(stateID, out State state)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(stateID)));
                }
                State = null;
                return;
            }
            Value    = value ?? state.MaxValue;
            Duration = Mathf.Approximately(duration, -1.0f) ? state.MaxDuration ?? -1.0f : duration;
            State    = state;
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if(serializer.IsReader) {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out ushort id);
                reader.ReadValueSafe(out ushort val);
                reader.ReadValueSafe(out float dur);
                State    = StateManager.Get(id);
                Value    = val;
                Duration = dur;
            } else {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(ID);
                writer.WriteValueSafe(Value);
                writer.WriteValueSafe(Duration);
            }
        }

        public bool Equals(EntityState other) => ID == other.ID && Value == other.Value;
        public override bool Equals(object obj) => obj is EntityState other && Equals(other);
        public override int GetHashCode() => ID;
    }
}
