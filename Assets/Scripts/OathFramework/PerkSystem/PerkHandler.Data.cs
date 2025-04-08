using OathFramework.EntitySystem;
using OathFramework.Persistence;
using OathFramework.Utility;
using Unity.Serialization.Json;

namespace OathFramework.PerkSystem
{
    public partial class PerkHandler
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.Perks;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.Perks;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Abort;

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }
        
        ComponentData IPersistableComponent.GetPersistenceData()
        {
            Data data = new();
            foreach(Perk perk in perks.Current) {
                if(!perk.AutoPersistenceSync)
                    continue;

                data.Perks.Add(perk);
            }
            return data;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            Data unpacked = (Data)data;
            if(IsOwner) {
                int count = unpacked.Perks.Count;
                for(int i = 0; i < count; i++) {
                    AddPerk(unpacked.Perks.Array[i], false, false);
                }
                return;
            }
            SyncPersistenceOwnerRpc(new SyncData(unpacked.Perks));
        }

        public class Data : ComponentData
        {
            public QList<Perk> Perks = new();
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKey("perks");
                using JsonWriter.ArrayScope arrScope = context.Writer.WriteArrayScope();
                int count = value.Perks.Count;
                for(int i = 0; i < count; i++) {
                    Perk perk = value.Perks.Array[i];
                    using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                    context.Writer.WriteKeyValue("id", perk.LookupKey);
                }
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data = new();
                foreach(SerializedValueView state in context.SerializedValue.GetValue("perks").AsArrayView()) {
                    string id = state.GetValue("id").ToString();
                    if(!PerkManager.TryGet(id, out Perk perk))
                        continue;
                    
                    data.Perks.Add(perk);
                }
                return data;
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.Perks;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view)
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
