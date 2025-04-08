using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;

namespace OathFramework.Persistence
{
    public partial class PersistentProxy
    {
        public struct Data
        {
            public string ID;
            public string SpawnID;
            public bool Enabled;
            public bool IsGlobal;
            public TransformData Transform;
            public Dictionary<string, ComponentData> Components;
            
            public Data(PersistentProxy owner)
            {
                ID         = owner.ID;
                SpawnID    = owner.SpawnID;
                Enabled    = owner.gameObject.activeSelf;
                IsGlobal   = owner.IsGlobal;
                Transform  = new TransformData(owner.transform);
                Components = new Dictionary<string, ComponentData>();
                foreach(KeyValuePair<string, ComponentData> pair in owner.Components) {
                    Components.Add(pair.Key, pair.Value);
                }
            }

            public Data(PersistentObject obj)
            {
                ID         = obj.ID;
                SpawnID    = obj.SpawnID;
                Enabled    = obj.gameObject.activeSelf;
                IsGlobal   = obj.Type == PersistentObject.ObjectType.ProxyGlobal;
                Transform  = new TransformData(obj.transform);
                Components = new Dictionary<string, ComponentData>();
                foreach(KeyValuePair<string, IPersistableComponent> pair in obj.Persistables) {
                    Components.Add(pair.Key, pair.Value.GetPersistenceData());
                }
            }
        }
        
        public class Adapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                context.Writer.WriteKeyValue("id", value.ID);
                context.Writer.WriteKeyValue("spawn_id", value.SpawnID);
                context.Writer.WriteKeyValue("enabled", value.Enabled);
                context.Writer.WriteKeyValue("global", value.IsGlobal);
                context.Writer.WriteKey("transform");
                context.SerializeValue(value.Transform);
                context.Writer.WriteKey("components");
                
                using JsonWriter.ArrayScope arrScope = context.Writer.WriteArrayScope();
                foreach(KeyValuePair<string, ComponentData> pair in value.Components) {
                    if(!PersistenceManager.TryGetComponentAdapter(pair.Key, out ComponentAdapter adapter)) {
                        Debug.LogError($"Failed to serialize Component '{pair.Key}' on object with ID '{value.ID}'");
                        continue;
                    }
                    using JsonWriter.ObjectScope compScope = context.Writer.WriteObjectScope();
                    context.Writer.WriteKeyValue("id", pair.Key);
                    adapter.Serialize(context, pair.Value);
                }
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data                 = new() { Components = new Dictionary<string, ComponentData>() };
                SerializedValueView value = context.SerializedValue;
                SerializedArrayView cArr  = value.GetValue("components").AsArrayView();
                data.ID                   = value.GetValue("id").ToString();
                data.SpawnID              = value.GetValue("spawn_id").ToString();
                data.Enabled              = value.GetValue("enabled").AsBoolean();
                data.IsGlobal             = value.GetValue("global").AsBoolean();
                data.Transform            = context.DeserializeValue<TransformData>(value.GetValue("transform"));
                foreach(SerializedValueView componentVal in cArr) {
                    string id = componentVal.GetValue("id").ToString();
                    if(!PersistenceManager.TryGetComponentAdapter(id, out ComponentAdapter adapter)) {
                        Debug.LogError($"Missing adapter for persistable component '{id}'");
                        continue;
                    }
                    data.Components.Add(id, adapter.Deserialize(in context, componentVal));
                }
                return data;
            }
        }
    }
}
