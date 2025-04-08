using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Persistence;
using OathFramework.Utility;
using System;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.States
{
    public sealed partial class FlagHandler : NetworkBehaviour, IPersistableComponent
    {
        private Entity entity;
        private LockableHashSet<EntityFlag> flags = new();
        private QList<EntityFlag> flagsCopy       = new();
        
        private QList<EntityFlag> syncData = new();
        private NetworkVariable<SyncData> netSyncData = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner,
            value: new SyncData(new QList<EntityFlag>())
        );
        
        private void Awake()
        {
            entity = GetComponent<Entity>();
        }
        
        public override void OnNetworkSpawn()
        {
            if(entity.IsDummy || IsOwner)
                return;

            syncData = netSyncData.Value.Data;
            for(int i = 0; i < syncData.Count; i++) {
                SetFlag(syncData.Array[i], true);
            }
            netSyncData.OnValueChanged += OnSyncDataChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            if(IsOwner)
                return;

            netSyncData.OnValueChanged -= OnSyncDataChanged;
        }
        
        private void OnSyncDataChanged(SyncData previous, SyncData current)
        {
            syncData = current.Data;
            flagsCopy.Clear();
            flagsCopy.AddRange(flags.Current);
            
            // Process removed flags.
            for(int i = 0; i < flagsCopy.Count; i++) {
                Flag flag = flagsCopy.Array[i].Flag;
                if(flag.NetSync && !SyncDataContains(flag)) {
                    SetFlag(new EntityFlag(flag), false);
                }
            }
            
            // Process added flags.
            for(int i = 0; i < syncData.Count; i++) {
                EntityFlag flag = syncData.Array[i];
                if(flag.Flag == null)
                    continue;
                
                SetFlag(flag, true);
            }
            return;
            
            bool SyncDataContains(Flag flag)
            {
                for(int i = 0; i < syncData.Count; i++) {
                    EntityFlag syncFlag = syncData.Array[i];
                    if(syncFlag.Flag == null)
                        continue;
                    if(syncFlag.Flag.ID == flag.ID)
                        return true;
                }
                return false;
            }
        }

        public bool HasFlag(Flag flag)
        {
            foreach(EntityFlag eFlag in flags.Current) {
                if(eFlag.ID == flag.ID)
                    return true;
            }
            return false;
        }
        
        public void SetFlag(EntityFlag flag, bool val)
        {
            if(flag.Flag == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(flag)));
                }
                return;
            }

            if(val) {
                AddFlag(flag);
            } else {
                RemoveFlag(flag);
            }
        }
        
        public void AddFlags(QList<EntityFlag> flags)
        {
            int count = flags.Count;
            for(int i = 0; i < count; i++) {
                AddFlag(flags.Array[i]);
            }
        }
        
        public void RemoveFlags(QList<EntityFlag> flags)
        {
            int count = flags.Count;
            for(int i = 0; i < count; i++) {
                RemoveFlag(flags.Array[i]);
            }
        }

        public void AddFlag(EntityFlag flag)
        {
            if(flag.Flag == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(flag)));
                }
                return;
            }

            if(flags.Add(flag)) {
                UpdateSyncData();
            }
        }

        public void RemoveFlag(EntityFlag flag)
        {
            if(flag.Flag == null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(flag)));
                }
                return;
            }
            if(flags.Remove(flag)) {
                UpdateSyncData();
            }
        }
        
        public void UpdateSyncData()
        {
            if(!IsOwner)
                return;
            
            syncData.Clear();
            foreach(EntityFlag flag in flags.Current) {
                if(!flag.Flag.NetSync)
                    continue;

                syncData.Add(flag);
            }
            netSyncData.Value = new SyncData(syncData);
            netSyncData.SetDirty(true);
        }

        private async UniTask SyncPersistenceTask(SyncData data)
        {
            await entity.WaitForNetInitialization();
            int count = data.Data.Count;
            for(int i = 0; i < count; i++) {
                SetFlag(data.Data.Array[i], true);
            }
        }

        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        private void SyncPersistenceOwnerRpc(SyncData data)
        {
            _ = SyncPersistenceTask(data);
        }
        
        private struct SyncData : INetworkSerializable, IEquatable<SyncData>
        {
            public QList<EntityFlag> Data { get; private set; }

            public SyncData(QList<EntityFlag> data)
            {
                Data = data;
            }
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                Data ??= new QList<EntityFlag>();
                if(serializer.IsReader) {
                    FastBufferReader reader = serializer.GetFastBufferReader();
                    reader.ReadValueSafe(out byte len);
                    Data.Clear();
                    for(byte i = 0; i < len; i++) {
                        reader.ReadValueSafe(out ushort id);
                        Data.Add(new EntityFlag(id));
                    }
                } else {
                    FastBufferWriter writer = serializer.GetFastBufferWriter();
                    writer.WriteValueSafe((byte)Data.Count);
                    for(byte i = 0; i < Data.Count; i++) {
                        EntityFlag entityFlag = Data.Array[i];
                        writer.WriteValueSafe(entityFlag.Flag.ID);
                    }
                }
            }

            public bool Equals(SyncData other)
            {
                if(Data == null || Data.Count != other.Data.Count)
                    return false;

                for(int i = 0; i < Data.Count; i++) {
                    EntityFlag entityFlag      = Data.Array[i];
                    EntityFlag otherEntityFlag = other.Data.Array[i];
                    if(!entityFlag.Equals(otherEntityFlag))
                        return false;
                }
                return true;
            }
        }
    }

    public readonly struct EntityFlag : IEquatable<EntityFlag>
    {
        public Flag Flag        { get; }
        public string LookupKey => Flag.LookupKey;
        public ushort ID        => Flag.ID;
        
        public EntityFlag(Flag flag)
        {
            Flag = flag;
        }

        public EntityFlag(string key)
        {
            if(!FlagManager.TryGet(key, out Flag flag)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(key)));
                }
                Flag = null;
                return;
            }
            Flag = flag;
        }

        public EntityFlag(ushort id)
        {
            if(!FlagManager.TryGet(id, out Flag flag)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError(new NullReferenceException(nameof(id)));
                }
                Flag = null;
                return;
            }
            Flag = flag;
        }

        public bool Equals(EntityFlag other) => ID == other.ID;
    }
}
