using OathFramework.Persistence;
using OathFramework.Utility;
using Unity.Serialization.Json;

namespace OathFramework.EntitySystem.States
{
    public sealed partial class FlagHandler
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.Flags;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.Flags;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Abort;

        // TODO: Sync flags which have been removed. (dictionary check?)
        
        ComponentData IPersistableComponent.GetPersistenceData()
        {
            Data data = new();
            foreach(EntityFlag flag in flags.Current) {
                if(!flag.Flag.PersistenceSync)
                    continue;

                data.Flags.Add(flag);
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
                int count = unpacked.Flags.Count;
                for(int i = 0; i < count; i++) {
                    SetFlag(unpacked.Flags.Array[i], true);
                }
                return;
            }
            SyncPersistenceOwnerRpc(new SyncData(unpacked.Flags));
        }

        public class Data : ComponentData
        {
            public QList<EntityFlag> Flags = new();
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKey("flags");
                using JsonWriter.ArrayScope arrScope = context.Writer.WriteArrayScope();
                int count = value.Flags.Count;
                for(int i = 0; i < count; i++) {
                    using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                    context.Writer.WriteKeyValue("id", value.Flags.Array[i].LookupKey);
                }
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data = new();
                foreach(SerializedValueView effect in context.SerializedValue.GetValue("flags").AsArrayView()) {
                    string id = effect.GetValue("id").ToString();
                    data.Flags.Add(new EntityFlag(id));
                }
                return data;
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.Flags;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view)
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
