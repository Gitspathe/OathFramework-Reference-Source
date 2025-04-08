using OathFramework.Persistence;
using OathFramework.Progression;
using Unity.Serialization.Json;
using UnityEngine;

namespace OathFramework.EntitySystem.Players
{
    public class PlayerPersistenceBinder : MonoBehaviour, IPersistableComponent
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.PlayerBinder;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.PlayerBinder;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Abort;

        private PlayerController controller;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        ComponentData IPersistableComponent.GetPersistenceData()
        {
            return new Data {
                UniqueID  = controller.NetClient.UniqueID,
                Index = controller.NetClient.Index,
                BuildData = controller.NetClient.Data.CurrentBuild
            };
        }

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            if(proxy != null) // Assigned elsewhere with proxy.
                return;
            
            Data unpacked = (Data)data;
            controller.NetClient.Data.SetBuild(unpacked.BuildData, true);
        }
        
        public class Data : ComponentData
        {
            public string UniqueID;
            public byte Index;
            public PlayerBuildData BuildData;
        }
        
        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKeyValue("uid", value.UniqueID);
                context.Writer.WriteKeyValue("index", value.Index);
                context.Writer.WriteKey("build");
                context.SerializeValue(value.BuildData);
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                return new Data {
                    UniqueID  = context.SerializedValue.GetValue("uid").ToString(),
                    Index = (byte)context.SerializedValue.GetValue("index").AsInt32(),
                    BuildData = context.DeserializeValue<PlayerBuildData>(context.SerializedValue.GetValue("build"))
                };
            }
        }
        
        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.PlayerBinder;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view) 
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
