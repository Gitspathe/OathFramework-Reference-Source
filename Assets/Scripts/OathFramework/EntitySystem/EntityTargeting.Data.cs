using OathFramework.Persistence;
using Unity.Serialization.Json;

namespace OathFramework.EntitySystem
{
    public partial class EntityTargeting
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.Targeting;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.Targeting;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Continue;

        ComponentData IPersistableComponent.GetPersistenceData() => new Data { CurrentTarget = CurrentTarget };

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            Data unpacked = (Data)data;
            ChangeTarget(unpacked.CurrentTarget);
        }

        public class Data : ComponentData
        {
            public Entity CurrentTarget;
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                string target = "";
                if(value.CurrentTarget != null) {
                    PersistenceManager.TryGetObjectID(value.CurrentTarget, out target);
                }
                context.Writer.WriteKeyValue("cur_target", target ?? "");
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data        = new();
                string targetStr = context.SerializedValue.GetValue("cur_target").ToString();
                if(!string.IsNullOrEmpty(targetStr)) {
                    PersistenceManager.TryGetObjectBehaviour(targetStr, out data.CurrentTarget);
                }
                return data;
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.Targeting;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view) 
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
