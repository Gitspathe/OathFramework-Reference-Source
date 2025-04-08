using OathFramework.Effects;
using OathFramework.EntitySystem.States;
using OathFramework.Pooling;
using OathFramework.Utility;
using System;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem
{ 
    public struct DamageValue : INetworkSerializable
    {
        public ushort Amount;
        public Vector3 HitPosition;
        public DamageSource Source;
        public StaggerStrength StaggerStrength;
        public ushort StaggerAmount;
        public QList<EntitySpEvent> SpEvents;

        private DamageFlags flags;
        private NetworkBehaviourReference instigatorRef;
        private bool fromNetwork;

        public readonly DamageFlags Flags => flags;
        
        public bool HasInstigator {
            readonly get => (DamageFlags.HasInstigator & flags) != 0;
            set {
                if(value) {
                    flags |= DamageFlags.HasInstigator;
                } else {
                    flags &= ~DamageFlags.HasInstigator;
                }
            }
        }

        public bool HasPosition {
            readonly get => (DamageFlags.HasPosition & flags) != 0;
            set {
                if(value) {
                    flags |= DamageFlags.HasPosition;
                } else {
                    flags &= ~DamageFlags.HasPosition;
                }
            }
        }
        
        public bool IsCritical {
            readonly get => (DamageFlags.IsCritical & flags) != 0;
            set {
                if(value) {
                    flags |= DamageFlags.IsCritical;
                } else {
                    flags &= ~DamageFlags.IsCritical;
                }
            }
        }

        public bool BypassInstigatorCallbacks {
            readonly get => (DamageFlags.BypassInstigatorCallbacks & flags) != 0;
            set {
                if(value) {
                    flags |= DamageFlags.BypassInstigatorCallbacks;
                } else {
                    flags &= ~DamageFlags.BypassInstigatorCallbacks;
                }
            }
        }

        public bool HasSpEvents {
            readonly get => (DamageFlags.HasSpEvents & flags) != 0;
            set {
                if(value) {
                    flags |= DamageFlags.HasSpEvents;
                } else {
                    flags &= ~DamageFlags.HasSpEvents;
                }
            }
        }
        
        public readonly bool IsUnavoidableDeath => Source == DamageSource.DieCommand || Source == DamageSource.SyncDeath;

        public DamageValue(
            ushort amount,
            Vector3 position              = default,
            DamageSource source           = DamageSource.Undefined,
            StaggerStrength staggerType   = StaggerStrength.None,
            ushort staggerAmount          = 0,
            DamageFlags flags             = DamageFlags.None,
            Entity instigator             = null,
            QList<EntitySpEvent> spEvents = null)
        {
            Amount          = amount;
            Source          = source;
            StaggerStrength = staggerType;
            StaggerAmount   = staggerAmount;
            this.flags      = flags;
            HitPosition     = default;
            instigatorRef   = default;
            SpEvents        = spEvents;
            fromNetwork     = false;
            HasSpEvents     = spEvents != null && spEvents.Count > 0;
            HasInstigator   = !ReferenceEquals(instigator, null);
            HasPosition     = position != default;
            if(HasInstigator) { 
                instigatorRef = instigator;
            }
            if(HasPosition) {
                HitPosition = position;
            }
        }
        
        public bool IsFriendlyFire(Entity target) 
            => HasInstigator && instigatorRef.TryGet(out Entity e) && EntityTypes.AreFriends(e.Team, target.Team);
        
        public readonly bool GetInstigator(out Entity instigator)
        {
            NetworkBehaviourReference iCopy = instigatorRef; // Copy.
            return iCopy.TryGet(out instigator);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if(serializer.IsReader) {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out ushort amount);
                reader.ReadValueSafe(out DamageSource source);
                reader.ReadValueSafe(out StaggerStrength staggerStrength);
                reader.ReadValueSafe(out ushort staggerAmount);
                reader.ReadValueSafe(out DamageFlags lFlags);
                Amount          = amount;
                Source          = source;
                StaggerStrength = staggerStrength;
                StaggerAmount   = staggerAmount;
                flags           = lFlags;
                if(HasInstigator) {
                    reader.ReadValueSafe(out NetworkBehaviourReference lInstigatorRef);
                    instigatorRef = lInstigatorRef;
                }
                if(HasPosition) {
                    reader.ReadValueSafe(out Vector3 lHitPosition);
                    HitPosition = lHitPosition;
                }
                if(HasSpEvents) {
                    SpEvents = StaticObjectPool<QList<EntitySpEvent>>.Retrieve();
                    reader.ReadValueSafe(out byte spEventLen);
                    for(int i = 0; i < spEventLen; i++) {
                        reader.ReadValueSafe(out NetSpEvent spEvent);
                        SpEvents.Add(spEvent.SpEvent);
                    }
                }
                fromNetwork = true;
            } else {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Amount);
                writer.WriteValueSafe(Source);
                writer.WriteValueSafe(StaggerStrength);
                writer.WriteValueSafe(StaggerAmount);
                writer.WriteValueSafe(Flags);
                if(HasInstigator) {
                    writer.WriteValueSafe(instigatorRef);
                }
                if(HasPosition) {
                    writer.WriteValueSafe(HitPosition);
                }
                if(HasSpEvents) {
                    if(SpEvents.Count > byte.MaxValue) {
                        Debug.LogWarning($"Attempted to write more than {byte.MaxValue} SpEvents, excess are trimmed.");
                        SpEvents.Trim(byte.MaxValue);
                    }
                    writer.WriteValueSafe((byte)SpEvents.Count);
                    for(int i = 0; i < SpEvents.Count; i++) {
                        writer.WriteValueSafe(new NetSpEvent(SpEvents.Array[i]));
                    }
                }
                fromNetwork = false;
            }
        }

        public readonly void ReturnResources()
        {
            if(!fromNetwork)
                return;
            
            if(SpEvents != null && SpEvents.Count > 0) {
                SpEvents.Clear();
                StaticObjectPool<QList<EntitySpEvent>>.Return(SpEvents);
            }
        }

        public static DamageValue DieCommand => new(ushort.MaxValue, source: DamageSource.DieCommand);
        public static DamageValue Undefined  => new(0);
        public static DamageValue SyncDeath  => new(ushort.MaxValue, source: DamageSource.SyncDeath);
    }

    public struct HealValue : INetworkSerializable
    {
        public ushort Amount;
        public Vector3 HitPosition;
        public HealSource Source;
        public QList<EntitySpEvent> SpEvents;

        private HealFlags flags;
        private NetworkBehaviourReference instigatorRef;
        private bool hasInstigator;
        private bool fromNetwork;
        
        public readonly HealFlags Flags => flags;
        
        public bool HasInstigator {
            readonly get => (HealFlags.HasInstigator & flags) != 0;
            set {
                if(value) {
                    flags |= HealFlags.HasInstigator;
                } else {
                    flags &= ~HealFlags.HasInstigator;
                }
            }
        }

        public bool HasPosition {
            readonly get => (HealFlags.HasPosition & flags) != 0;
            set {
                if(value) {
                    flags |= HealFlags.HasPosition;
                } else {
                    flags &= ~HealFlags.HasPosition;
                }
            }
        }
        
        public bool IsCritical {
            readonly get => (HealFlags.IsCritical & flags) != 0;
            set {
                if(value) {
                    flags |= HealFlags.IsCritical;
                } else {
                    flags &= ~HealFlags.IsCritical;
                }
            }
        }

        public bool BypassInstigatorCallbacks {
            readonly get => (HealFlags.BypassInstigatorCallbacks & flags) != 0;
            set {
                if(value) {
                    flags |= HealFlags.BypassInstigatorCallbacks;
                } else {
                    flags &= ~HealFlags.BypassInstigatorCallbacks;
                }
            }
        }

        public bool HasSpEvents {
            readonly get => (HealFlags.HasSpEvents & flags) != 0;
            set {
                if(value) {
                    flags |= HealFlags.HasSpEvents;
                } else {
                    flags &= ~HealFlags.HasSpEvents;
                }
            }
        }
        
        public HealValue(
            ushort amount, 
            Vector3 position              = default,
            HealSource source             = HealSource.Undefined, 
            HealFlags flags               = HealFlags.None,
            Entity instigator             = null, 
            QList<EntitySpEvent> spEvents = null)
        {
            Amount        = amount;
            HitPosition   = position;
            Source        = source;
            this.flags    = flags;
            SpEvents      = spEvents;
            hasInstigator = !ReferenceEquals(instigator, null);
            instigatorRef = default;
            fromNetwork   = false;
            if(hasInstigator) {
                instigatorRef = instigator;
            }
        }

        public readonly bool GetInstigator(out Entity instigator)
        {
            NetworkBehaviourReference iCopy = instigatorRef; // Copy.
            return iCopy.TryGet(out instigator);
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if(serializer.IsReader) {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out ushort amount);
                reader.ReadValueSafe(out HealSource source);
                reader.ReadValueSafe(out HealFlags lFlags);
                Amount = amount;
                Source = source;
                flags  = lFlags;
                if(HasInstigator) {
                    reader.ReadValueSafe(out NetworkBehaviourReference lInstigatorRef);
                    instigatorRef = lInstigatorRef;
                }
                if(HasPosition) {
                    reader.ReadValueSafe(out Vector3 lHitPosition);
                    HitPosition = lHitPosition;
                }
                if(HasSpEvents) {
                    SpEvents = StaticObjectPool<QList<EntitySpEvent>>.Retrieve();
                    reader.ReadValueSafe(out byte spEventLen);
                    for(int i = 0; i < spEventLen; i++) {
                        reader.ReadValueSafe(out NetSpEvent spEvent);
                        SpEvents.Add(spEvent.SpEvent);
                    }
                }
                fromNetwork = true;
            } else {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Amount);
                writer.WriteValueSafe(Source);
                writer.WriteValueSafe(Flags);
                if(HasInstigator) {
                    writer.WriteValueSafe(instigatorRef);
                }
                if(HasPosition) {
                    writer.WriteValueSafe(HitPosition);
                }
                if(HasSpEvents) {
                    if(SpEvents.Count > byte.MaxValue) {
                        Debug.LogWarning($"Attempted to write more than {byte.MaxValue} SpEvents, excess are trimmed.");
                        SpEvents.Trim(byte.MaxValue);
                    }
                    writer.WriteValueSafe((byte)SpEvents.Count);
                    for(int i = 0; i < SpEvents.Count; i++) {
                        writer.WriteValueSafe(new NetSpEvent(SpEvents.Array[i]));
                    }
                }
                fromNetwork = false;
            }
        }
        
        public readonly void ReturnResources()
        {
            if(!fromNetwork)
                return;
            
            if(SpEvents != null && SpEvents.Count > 0) {
                SpEvents.Clear();
                StaticObjectPool<QList<EntitySpEvent>>.Return(SpEvents);
            }
        }
    }
    
    public struct HitEffectValue : INetworkSerializable
    {
        private ushort netID;
        
        public HitEffectValue(ushort netID)
        {
            this.netID = netID;
        }

        public HitEffectValue(string key)
        {
            if(HitEffectManager.TryGetHitEffectParams(key, out _, out netID))
                return;

            Debug.LogError($"Failed to retrieve {nameof(HitEffectParams)} for {key}");
            netID = 0;
        }

        public readonly void Deconstruct(out HitEffectParams @params)
        {
            @params = null;
            HitEffectManager.TryGetHitEffectParams(netID, out @params, out _);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref netID);
        }
    }

    [Flags]
    public enum DamageFlags : byte
    {
        None                      = 0,
        HasInstigator             = 1,
        HasPosition               = 2,
        IsCritical                = 4,
        BypassInstigatorCallbacks = 8,
        HasSpEvents               = 16,
        Reserved1                 = 32,
        Reserved2                 = 64,
        Reserved3                 = 128,
    }

    [Flags]
    public enum HealFlags : byte
    {
        None                      = 0,
        HasInstigator             = 1,
        HasPosition               = 2,
        IsCritical                = 4,
        BypassInstigatorCallbacks = 8,
        HasSpEvents               = 16,
        Reserved1                 = 32,
        Reserved2                 = 64,
        Reserved3                 = 128,
    }

    public enum DamageSource : byte
    {
        Undefined   = 0,
        Projectile  = 1,
        Melee       = 2,
        Explosion   = 3,
        Status      = 4,
        Environment = 5,
        SyncDeath   = 254,
        DieCommand  = 255
    }

    public enum HealSource : byte
    {
        Undefined = 0,
        Entity    = 1,
        Player    = 2
    }
    
    public enum StaggerStrength : byte
    {
        None      = 0,
        Low       = 1,
        Medium    = 2,
        High      = 3
    }

    public enum HitDirection : byte
    {
        None    = 0,
        Forward = 1,
        Right   = 2,
        Back    = 3,
        Left    = 4
    }
}
