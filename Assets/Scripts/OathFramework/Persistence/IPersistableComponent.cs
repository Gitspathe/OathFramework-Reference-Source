using Unity.Serialization.Json;

namespace OathFramework.Persistence
{
    public abstract class ComponentData { }

    public abstract class ComponentAdapter
    {
        public abstract string ID { get; }
        public abstract void OnInitialize();
        public abstract ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view);
        public abstract void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value);
    }
    
    public interface IPersistableComponent
    {
        uint Order                                    { get; }
        string PersistableID                          { get; }
        VerificationFailAction VerificationFailAction { get; }
        ComponentData GetPersistenceData();
        bool Verify(ComponentData data, PersistentProxy proxy);
        void ApplyPersistenceData(ComponentData data, PersistentProxy proxy);
    }

    public enum VerificationFailAction { Continue, Abort }
}
