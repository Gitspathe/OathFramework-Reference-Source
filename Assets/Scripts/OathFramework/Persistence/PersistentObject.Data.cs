using System;
using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;

namespace OathFramework.Persistence
{
    public partial class PersistentObject
    {
        public struct Data
        {
            public string ID;
            public string SpawnID;
            public ObjectType Type;
            public bool Enabled;
            public bool Networked;
            public bool Pooled;
            public TransformData Transform;
            public Dictionary<string, ComponentData> ComponentData;

            public string TypeString {
                get {
                    switch(Type) {
                        case ObjectType.Global:
                            return "global";
                        case ObjectType.Scene:
                            return "scene";
                        case ObjectType.Prefab:
                            return "prefab";
                        case ObjectType.ProxyGlobal:
                            return "proxy_global";
                        case ObjectType.ProxyPrefab:
                            return "proxy_prefab";
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                set {
                    switch(value) {
                        case "global": {
                            Type = ObjectType.Global;
                        } break;
                        case "scene": {
                            Type = ObjectType.Scene;
                        } break;
                        case "prefab": {
                            Type = ObjectType.Prefab;
                        } break;
                        case "proxy_global": {
                            Type = ObjectType.ProxyGlobal;
                        } break;
                        case "proxy_prefab": {
                            Type = ObjectType.ProxyPrefab;
                        } break;
                    }
                }
            }
            
            public Data(PersistentObject owner)
            {
                ID            = owner.ID;
                SpawnID       = owner.Type == ObjectType.Global ? null : owner.SpawnID;
                Type          = owner.Type;
                Enabled       = owner.gameObject.activeSelf;
                Networked     = owner.IsNetworked;
                Pooled        = owner.IsPooled;
                Transform     = new TransformData(owner.transform);
                ComponentData = new Dictionary<string, ComponentData>();
                foreach(KeyValuePair<string, IPersistableComponent> pair in owner.Persistables) {
                    ComponentData.Add(pair.Key, pair.Value.GetPersistenceData());
                }
            }
        }
        
        public class DataAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                context.Writer.WriteKeyValue("id", value.ID);
                context.Writer.WriteKeyValue("spawn_id", value.SpawnID);
                context.Writer.WriteKeyValue("type", value.TypeString);
                context.Writer.WriteKeyValue("enabled", value.Enabled);
                context.Writer.WriteKeyValue("networked", value.Networked);
                context.Writer.WriteKeyValue("pooled", value.Pooled);
                context.Writer.WriteKey("transform");
                context.SerializeValue(value.Transform);
                context.Writer.WriteKey("components");
                
                using JsonWriter.ArrayScope arrScope = context.Writer.WriteArrayScope();
                foreach(KeyValuePair<string, ComponentData> pair in value.ComponentData) {
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
                Data data                 = new() { ComponentData = new Dictionary<string, ComponentData>() };
                SerializedValueView value = context.SerializedValue;
                SerializedArrayView cArr  = value.GetValue("components").AsArrayView();
                data.ID                   = value.GetValue("id").ToString();
                data.SpawnID              = value.GetValue("spawn_id").ToString();
                data.TypeString           = value.GetValue("type").ToString();
                data.Enabled              = value.GetValue("enabled").AsBoolean();
                data.Networked            = value.GetValue("networked").AsBoolean();
                data.Pooled               = value.GetValue("pooled").AsBoolean();
                data.Transform            = context.DeserializeValue<TransformData>(value.GetValue("transform"));
                foreach(SerializedValueView componentVal in cArr) {
                    string id = componentVal.GetValue("id").ToString();
                    if(!PersistenceManager.TryGetComponentAdapter(id, out ComponentAdapter adapter)) {
                        Debug.LogError($"Missing adapter for persistable component '{id}'");
                        continue;
                    }
                    data.ComponentData.Add(id, adapter.Deserialize(in context, componentVal));
                }
                return data;
            }
        }
    }
}
