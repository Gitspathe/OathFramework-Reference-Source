using System.Collections.Generic;
using Unity.Serialization.Json;

namespace OathFramework.Persistence
{
    public partial class PersistentScene
    {
        public struct Data
        {
            public string ID;
            public Dictionary<string, PersistentObject.Data> ObjectData;
            public Dictionary<string, PersistentProxy.Data> ProxyData;

            public Data(PersistentScene scene)
            {
                ID         = scene.ID;
                ObjectData = new Dictionary<string, PersistentObject.Data>();
                ProxyData  = new Dictionary<string, PersistentProxy.Data>();
                
                // Existing proxies.
                foreach(KeyValuePair<string, PersistentProxy> pair in scene.Proxies) {
                    ProxyData.Add(pair.Key, new PersistentProxy.Data(pair.Value));
                }
                
                // Objects, and proxies which need to be generated.
                foreach(KeyValuePair<string, PersistentObject> pair in scene.Objects) {
                    if(pair.Value == null) {
                        // TODO: Deleted object??
                        continue;
                    }
                    
                    if(pair.Value.GenerateProxy && !ProxyData.ContainsKey(pair.Value.ID)) {
                        ProxyData.Add(pair.Key, new PersistentProxy.Data(pair.Value));
                        continue;
                    }
                    ObjectData.Add(pair.Key, new PersistentObject.Data(pair.Value));
                }
            }
        }

        public class DataAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                using JsonWriter.ObjectScope sceneScope = context.Writer.WriteObjectScope();
                context.Writer.WriteKeyValue("id", value.ID);
                context.Writer.WriteKey("proxies");
                using(context.Writer.WriteArrayScope()) {
                    foreach(KeyValuePair<string, PersistentProxy.Data> pair in value.ProxyData) {
                        context.SerializeValue(pair.Value);
                    }
                }
                
                context.Writer.WriteKey("objects");
                using(context.Writer.WriteArrayScope()) {
                    foreach(KeyValuePair<string, PersistentObject.Data> pair in value.ObjectData) {
                        context.SerializeValue(pair.Value);
                    }
                }
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data = new() {
                    ObjectData = new Dictionary<string, PersistentObject.Data>(), 
                    ProxyData  = new Dictionary<string, PersistentProxy.Data>()
                };
                SerializedValueView value    = context.SerializedValue;
                SerializedArrayView proxyArr = value.GetValue("proxies").AsArrayView();
                SerializedArrayView objArr   = value.GetValue("objects").AsArrayView();
                data.ID                      = value.GetValue("id").ToString();
                foreach(SerializedValueView proxy in proxyArr) {
                    string id = proxy.GetValue("id").ToString();
                    data.ProxyData.Add(id, context.DeserializeValue<PersistentProxy.Data>(proxy));
                }
                foreach(SerializedValueView obj in objArr) {
                    string id = obj.GetValue("id").ToString();
                    data.ObjectData.Add(id, context.DeserializeValue<PersistentObject.Data>(obj));
                }
                return data;
            }
        }
    }
}
