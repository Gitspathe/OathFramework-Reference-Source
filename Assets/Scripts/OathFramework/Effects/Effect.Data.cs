using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Persistence;
using Unity.Netcode;
using Unity.Serialization.Json;
using UnityEngine;

namespace OathFramework.Effects
{
    public partial class Effect
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.Effect;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.Effect;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Continue;

        ComponentData IPersistableComponent.GetPersistenceData()
        {
            Data data = new() {
                Duration      = CurDuration, 
                IsDissipating = IsDissipating,
                Dissipation   = Dissipation
            };
            if(Game.ExtendedDebug && Sockets != null) {
                Debug.LogWarning($"Saved Effect '{name}' which is attached to a model socket. This is unsupported.");
            }
            return data;
        }

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            Data unpacked = (Data)data;
            SetParams(unpacked);
            if(TryGetComponent(out NetEffect ne) && ne.IsOwner) {
                ne.SyncPersistenceNotOwnerRpc(unpacked);
            }
        }

        public class Data : ComponentData, INetworkSerializable
        {
            public float Duration;
            public bool IsDissipating;
            public float Dissipation;
            public ushort ExtraData;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                if(serializer.IsReader) {
                    FastBufferReader reader = serializer.GetFastBufferReader();
                    reader.ReadValueSafe(out Duration);
                    reader.ReadValueSafe(out IsDissipating);
                    reader.ReadValueSafe(out Dissipation);
                    reader.ReadValueSafe(out ExtraData);
                } else {
                    FastBufferWriter writer = serializer.GetFastBufferWriter();
                    writer.WriteValueSafe(Duration);
                    writer.WriteValueSafe(IsDissipating);
                    writer.WriteValueSafe(Dissipation);
                    writer.WriteValueSafe(ExtraData);
                }
            }

            public bool Equals(Data other) => Mathf.Approximately(Duration, other.Duration) 
                                              && IsDissipating == other.IsDissipating 
                                              && Mathf.Approximately(Dissipation, other.Dissipation);
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKeyValue("duration", value.Duration);
                context.Writer.WriteKeyValue("is_dissipating", value.IsDissipating);
                context.Writer.WriteKeyValue("dissipation", value.Dissipation);
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data = new() {
                    Duration      = context.SerializedValue.GetValue("duration").AsFloat(),
                    IsDissipating = context.SerializedValue.GetValue("is_dissipating").AsBoolean(), 
                    Dissipation   = context.SerializedValue.GetValue("dissipation").AsFloat()
                };
                return data;
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.Effect;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view)
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
