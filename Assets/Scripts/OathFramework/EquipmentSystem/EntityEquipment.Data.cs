using Cysharp.Threading.Tasks;
using OathFramework.EntitySystem;
using OathFramework.Persistence;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Serialization.Json;

namespace OathFramework.EquipmentSystem
{
    public partial class EntityEquipment
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.Equipment;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.Equipment;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Abort;

        ComponentData IPersistableComponent.GetPersistenceData()
        {
            Data data = new();
            data.Nodes.Add(netPrimarySlot.Value.ToDataNode());
            data.Nodes.Add(netSecondarySlot.Value.ToDataNode());
            return data;
        }

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            Data unpacked = (Data)data;
            Assign(netPrimarySlot, EquipmentSlot.Primary);
            Assign(netSecondarySlot, EquipmentSlot.Secondary);
            return;

            void Assign(NetworkVariable<NetInventorySlot> netInvSlot, EquipmentSlot slot)
            {
                Data.Node node = unpacked.Nodes[(int)slot - 1];
                _ = DelayedEquippableAssignment(node, netInvSlot);
            }
        }

        private async UniTask DelayedEquippableAssignment(Data.Node node, NetworkVariable<NetInventorySlot> netInvSlot)
        {
            await UniTask.Yield();
            if(IsOwner) {
                UpdateSlot(netInvSlot, node.EquippableKey, node.Ammo);
                return;
            }
            if(!EquippableManager.TryGetNetID(node.EquippableKey, out ushort netID))
                return;
            
            SetEquippableOwnerRpc(netInvSlot.Value.slot, netID, node.Ammo);
        }

        public class Data : ComponentData
        {
            public List<Node> Nodes = new();

            public struct Node
            {
                public EquipmentSlot Slot;
                public string EquippableKey;
                public ushort Ammo;

                public Node(EquipmentSlot slot, string equippableKey, ushort ammo)
                {
                    Slot          = slot;
                    EquippableKey = equippableKey;
                    Ammo          = ammo;
                }
            }
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKey("slots");
                using JsonWriter.ArrayScope arrScope = context.Writer.WriteArrayScope();
                foreach(Data.Node node in value.Nodes) {
                    using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                    context.Writer.WriteKeyValue("slot", (int)node.Slot);
                    context.Writer.WriteKeyValue("equippable", node.EquippableKey);
                    context.Writer.WriteKeyValue("ammo", node.Ammo);
                }
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data = new();
                foreach(SerializedValueView arrNode in context.SerializedValue.GetValue("slots").AsArrayView()) {
                    Data.Node node = new(
                        (EquipmentSlot)arrNode.GetValue("slot").AsInt32(),
                        arrNode.GetValue("equippable").ToString(),
                        (ushort)arrNode.GetValue("ammo").AsInt32()
                    );
                    data.Nodes.Add(node);
                }
                return data;
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.Equipment;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view) 
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
