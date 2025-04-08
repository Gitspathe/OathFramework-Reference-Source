using OathFramework.Persistence;
using Unity.Serialization.Json;

namespace OathFramework.EntitySystem
{
    public partial class StaggerHandler
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.Stagger;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.Stagger;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Continue;

        ComponentData IPersistableComponent.GetPersistenceData() => new Data {
            CurrentPoise       = CurrentPoise,
            CurrentPoiseReset  = CurrentPoiseReset,
            StaggerTime        = StaggerTime,
            UncontrollableTime = UncontrollableTime
        };

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            Data unpacked      = (Data)data;
            CurrentPoise       = unpacked.CurrentPoise;
            CurrentPoiseReset  = unpacked.CurrentPoiseReset;
            StaggerTime        = unpacked.StaggerTime;
            UncontrollableTime = unpacked.UncontrollableTime;
        }

        public class Data : ComponentData
        {
            public float CurrentPoise;
            public float CurrentPoiseReset;
            public float StaggerTime;
            public float UncontrollableTime;
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKeyValue("poise", value.CurrentPoise);
                context.Writer.WriteKeyValue("reset", value.CurrentPoiseReset);
                context.Writer.WriteKeyValue("stagger_t", value.StaggerTime);
                context.Writer.WriteKeyValue("no_control_t", value.UncontrollableTime);
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                return new Data {
                    CurrentPoise       = context.SerializedValue.GetValue("poise").AsFloat(),
                    CurrentPoiseReset  = context.SerializedValue.GetValue("reset").AsFloat(),
                    StaggerTime        = context.SerializedValue.GetValue("stagger_t").AsFloat(),
                    UncontrollableTime = context.SerializedValue.GetValue("no_control_t").AsFloat()
                };
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.Stagger;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view) 
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
