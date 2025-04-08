using OathFramework.Persistence;
using OathFramework.Utility;
using Unity.Serialization.Json;

namespace OathFramework.EntitySystem.States
{
    public sealed partial class StateHandler
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.EntityEffects;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.EntityEffects;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Abort;

        ComponentData IPersistableComponent.GetPersistenceData()
        {
            Data data = new();
            foreach(EntityState state in states.Current) {
                if(!state.State.PersistenceSync)
                    continue;

                data.States.Add(state);
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
            if(IsOwner) {
                int count = unpacked.States.Count;
                for(int i = 0; i < count; i++) {
                    SetState(unpacked.States.Array[i], false, false);
                }
                ApplyStats();
                return;
            }
            SyncPersistenceOwnerRpc(new SyncData(unpacked.States));
        }

        public class Data : ComponentData
        {
            public QList<EntityState> States = new();
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKey("states");
                using JsonWriter.ArrayScope arrScope = context.Writer.WriteArrayScope();
                int count = value.States.Count;
                for(int i = 0; i < count; i++) {
                    EntityState state = value.States.Array[i];
                    using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                    context.Writer.WriteKeyValue("id", state.State.LookupKey);
                    context.Writer.WriteKeyValue("value", state.Value);
                    context.Writer.WriteKeyValue("duration", state.Duration);
                }
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data = new();
                foreach(SerializedValueView state in context.SerializedValue.GetValue("states").AsArrayView()) {
                    string id      = state.GetValue("id").ToString();
                    ushort val     = (ushort)state.GetValue("value").AsInt32();
                    float duration = state.GetValue("duration").AsFloat();
                    data.States.Add(new EntityState(id, val, duration));
                }
                return data;
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.EntityEffects;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view)
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
