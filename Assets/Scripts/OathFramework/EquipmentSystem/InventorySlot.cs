using System;
using Unity.Netcode;

namespace OathFramework.EquipmentSystem
{
    public class InventorySlot
    {
        public Equippable Equippable;
        public ushort Ammo;

        public EquipmentSlot SlotID           { get; private set; }
        public string EquippableKey           => IsEmpty ? string.Empty : Equippable.EquippableKey;
        public ushort EquippableNetID         => IsEmpty ? (ushort)0 : Equippable.ID;
        public EquippableTypes EquippableType => IsEmpty ? EquippableTypes.None : Equippable.Type;
        public bool IsEmpty                   => Equippable == null;
        public bool UseBlocked                => IsEmpty || Ammo == 0;

        public bool IsRanged     => !IsEmpty && (Equippable.Type == EquippableTypes.Ranged || Equippable.Type == EquippableTypes.RangedMultiShot);
        public bool IsThrowing   => !IsEmpty && Equippable.Type == EquippableTypes.Grenade;
        public bool IsTemporary  => !IsEmpty && Equippable.Temporary;
        public bool HideOnEmpty  => !IsEmpty && Equippable.LoseOnEmpty;
        public bool IsReloadable => Equippable.GetRootStats() is IStatsReload;
        public bool HasAccuracy  => Equippable.GetRootStats() is IStatsAccuracy;
        public bool HasAmmoCount => Equippable.GetRootStats().AmmoCapacity > 0;

        public float SwapIKSuppressTime => Equippable.SwapIKSuppressTime;

        public bool ReloadBlocked {
            get {
                if(IsEmpty)
                    return true;

                if(HasAmmoCount && IsReloadable) {
                    return Ammo >= GetStatsAs<EquippableStats>().AmmoCapacity;
                }
                return false;
            }
        }

        public bool InterruptReloadBlocked {
            get {
                if(IsEmpty)
                    return true;
                
                if(IsReloadable) {
                    return !GetStatsInterface<IStatsReload>().ReloadInterrupt;
                }
                return false;
            }
        }
        
        public T GetStatsAs<T>() where T : EquippableStats                => Equippable.GetRootStats() as T;
        public T GetStatsInterface<T>() where T : class, IEquippableStats => Equippable.GetRootStats() as T;

        public InventorySlot(EquipmentSlot slot)
        {
            SlotID = slot;
        }

        public void Initialize(NetInventorySlot netInvSlot)
        {
            // TODO: Apply player stats.
            Equippable = netInvSlot.id == 0 ? null : EquippableManager.GetTemplate(netInvSlot.id);
            Ammo       = netInvSlot.ammo;
        }

        public void Clear()
        {
            Equippable = null;
            Ammo       = 0;
        }
    }

    [Serializable]
    public struct NetInventorySlot : INetworkSerializable, IEquatable<NetInventorySlot>
    {
        public string EquippableKey => EquippableManager.TryGetKey(id, out string str) ? str : string.Empty;

        public EquipmentSlot slot;
        public ushort id;
        public ushort ammo;

        public NetInventorySlot(EquipmentSlot slot)
        {
            this.slot = slot;
            id        = 0;
            ammo      = 0;
        }

        public NetInventorySlot(EquipmentSlot slot, ushort id, ushort ammo)
        {
            this.slot = slot;
            this.id   = id;
            this.ammo = ammo;
        }

        public EntityEquipment.Data.Node ToDataNode() => new() {
            Slot          = slot, 
            EquippableKey = EquippableKey, 
            Ammo          = ammo
        };

        public bool Equals(NetInventorySlot other) => ammo == other.ammo && id == other.id;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref ammo);
        }
    }
}
