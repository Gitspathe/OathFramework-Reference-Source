using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Persistence;
using OathFramework.Utility;
using System;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.PerkSystem
{
    [RequireComponent(typeof(Entity))]
    public partial class PerkHandler : NetLoopComponent, 
        IPersistableComponent, IEntityInitCallback, IEntityDieCallback
    {
        private LockableList<Perk> perks = new();
        private QList<Perk> perksCopy    = new();
        
        private QList<Perk> syncData = new();
        private NetworkVariable<SyncData> netSyncData = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner,
            value: new SyncData(new QList<Perk>())
        );
        
        protected Entity Entity;
        
        uint ILockableOrderedListElement.Order => 10_000;

        protected virtual void Awake()
        {
            Entity = GetComponent<Entity>();
        }
        
        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityDieCallback)this);
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(Entity.IsDummy || IsOwner)
                return;
            
            syncData = netSyncData.Value.Data;
            for(int i = 0; i < syncData.Count; i++) {
                AddPerk(syncData.Array[i], true, true);
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
            foreach(Perk p in perks.Current) {
                p.Added(Entity, !IsOwner, false);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach(Perk p in perks.Current) {
                p.Removed(Entity, !IsOwner, false);
            }
        }
        
        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            perks.Lock();
            foreach(Perk p in perks.Current) {
                if(!p.RemoveOnDeath)
                    continue;
                
                p.Removed(Entity, !IsOwner, false);
                perks.Remove(p);
            }
            perks.Unlock();
        }
        
        private void OnSyncDataChanged(SyncData previous, SyncData current)
        {
            syncData = current.Data;
            perksCopy.Clear();
            perksCopy.AddRange(perks.Current);
            
            // Process removed perks.
            for(int i = 0; i < perksCopy.Count; i++) {
                Perk perk = perksCopy.Array[i];
                if(perk.AutoNetSync && !SyncDataContains(perk)) {
                    RemovePerk(perk, true, false);
                }
            }
            
            // Process added perks.
            for(int i = 0; i < syncData.Count; i++) {
                Perk perk = syncData.Array[i];
                AddPerk(perk, true, false);
            }
            return;
            
            bool SyncDataContains(Perk perk)
            {
                for(int i = 0; i < syncData.Count; i++) {
                    Perk other = syncData.Array[i];
                    if(other.Equals(perk))
                        return true;
                }
                return false;
            }
        }

        public bool HasPerk(Perk perk)
        {
            foreach(Perk p in perks.Current) {
                if(p.Equals(perk))
                    return true;
            }
            return false;
        }
        
        public void AddPerk(Perk perk, bool auxOnly, bool lateJoin)
        {
            if(perk == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(perk)));
                }
                return;
            }
            if(HasPerk(perk))
                return;
            
            perks.Add(perk);
            perk.Added(Entity, auxOnly, lateJoin);
            if(IsOwner && perk.AutoNetSync) {
                UpdateSyncData();
            }
        }

        public void RemovePerk(Perk perk, bool auxOnly, bool lateJoin)
        {
            if(perk == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(perk)));
                }
                return;
            }
            if(!HasPerk(perk))
                return;
            
            perks.Remove(perk);
            perk.Removed(Entity, auxOnly, lateJoin);
            if(IsOwner && perk.AutoNetSync) {
                UpdateSyncData();
            }
        }

        public void ClearPerks(bool auxOnly)
        {
            perksCopy.Clear();
            perksCopy.AddRange(perks.Current);
            perks.Lock();
            for(int i = 0; i < perksCopy.Count; i++) {
                RemovePerk(perksCopy.Array[i], auxOnly, false);
            }
            perks.Unlock();
            perks.Clear();
            perksCopy.Clear();
            UpdateSyncData();
        }

        public void UpdateSyncData()
        {
            if(!IsOwner)
                return;
            
            syncData.Clear();
            foreach(Perk perk in perks.Current) {
                if(!perk.AutoNetSync)
                    continue;

                syncData.Add(perk);
            }
            netSyncData.Value = new SyncData(syncData);
            netSyncData.SetDirty(true);
        }
        
        private async UniTask SyncPersistenceTask(SyncData data)
        {
            await Entity.WaitForNetInitialization();
            int count = data.Data.Count;
            for(int i = 0; i < count; i++) {
                AddPerk(data.Data.Array[i], true, true);
            }
        }
        
        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        private void SyncPersistenceOwnerRpc(SyncData data)
        {
            _ = SyncPersistenceTask(data);
        }
        
        private struct SyncData : INetworkSerializable, IEquatable<SyncData>
        {
            public QList<Perk> Data { get; private set; }
            
            public SyncData(QList<Perk> data)
            {
                Data = data;
            }
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                Data ??= new QList<Perk>();
                if(serializer.IsReader) {
                    FastBufferReader reader = serializer.GetFastBufferReader();
                    reader.ReadValueSafe(out byte len);
                    Data.Clear();
                    for(byte i = 0; i < len; i++) {
                        reader.ReadValueSafe(out ushort id);
                        if(!PerkManager.TryGet(id, out Perk perk))
                            continue;
                        
                        Data.Add(perk);
                    }
                } else {
                    FastBufferWriter writer = serializer.GetFastBufferWriter();
                    writer.WriteValueSafe((byte)Data.Count);
                    for(byte i = 0; i < Data.Count; i++) {
                        Perk perk = Data.Array[i];
                        writer.WriteValueSafe(perk.ID);
                    }
                }
            }

            public bool Equals(SyncData other)
            {
                if(Data == null || Data.Count != other.Data.Count)
                    return false;

                for(int i = 0; i < Data.Count; i++) {
                    Perk perk      = Data.Array[i];
                    Perk otherPerk = other.Data.Array[i];
                    if(!perk.Equals(otherPerk))
                        return false;
                }
                return true;
            }
        }
    }
}
