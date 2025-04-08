using OathFramework.Persistence;
using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{
    public partial class RaycastProjectile
    {
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.RaycastProjectile;
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.RaycastProjectile;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Abort;

        ComponentData IPersistableComponent.GetPersistenceData() => new Data {
            Targets = targets,
            StdBulletData = data,
            IsActive = active, 
            CurDistance = curDistance
        };

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            Data unpacked = (Data)data;
            if(!unpacked.IsActive) {
                ReturnProjectile();
                return;
            }
            
            Initialize(
                true,
                unpacked.Targets,
                unpacked.StdBulletData,
                0.01f
            );
            curDistance = unpacked.CurDistance;
        }

        public class Data : ComponentData
        {
            public EntityTeams[] Targets;
            public StdBulletData StdBulletData;
            public bool IsActive;
            public float CurDistance;
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKeyValue("active", value.IsActive);
                if(!value.IsActive)
                    return;
                
                context.Writer.WriteKeyValue("cur_distance", value.CurDistance);
                context.Writer.WriteKey("targets");
                using(context.Writer.WriteArrayScope()) {
                    foreach(EntityTeams team in value.Targets) {
                        context.Writer.WriteValue((int)team);
                    }
                }
                context.SerializeValue("raycast_data", value.StdBulletData);
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                SerializedValueView value = context.SerializedValue;
                bool isActive             = value.GetValue("active").AsBoolean();
                if(!isActive)
                    return new Data();

                float dist              = value.GetValue("cur_distance").AsFloat();
                SerializedArrayView arr = value.GetValue("targets").AsArrayView();
                List<EntityTeams> teams = new();
                foreach(SerializedValueView val in arr) {
                    teams.Add((EntityTeams)val.AsInt32());
                }
                StdBulletData rData = context.DeserializeValue<StdBulletData>(value.GetValue("raycast_data"));
                return new Data {
                    Targets = teams.ToArray(), 
                    StdBulletData = rData, 
                    IsActive = true, 
                    CurDistance = dist
                };
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.RaycastProjectile;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view)
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
