using Cysharp.Threading.Tasks;
using OathFramework.Persistence;
using Unity.Serialization.Json;

namespace OathFramework.EntitySystem
{
    public partial class Entity
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.Entity;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.Entity;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Abort;

        ComponentData IPersistableComponent.GetPersistenceData() => new Data {
            Health      = CurStats.health, 
            Stamina     = CurStats.stamina,
            StaminaTime = timeSinceStaminaUse,
            StaminaAcc  = staminaAccumulator
        };

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            Data unpacked       = (Data)data;
            CurStats.health     = unpacked.Health;
            CurStats.stamina    = unpacked.Stamina;
            timeSinceStaminaUse = unpacked.StaminaTime;
            staminaAccumulator  = unpacked.StaminaAcc;
            if(IsOwner) {
                netHealth.Value  = unpacked.Health;
                netStamina.Value = unpacked.Stamina;
            } else {
                SyncPersistenceOwnerRpc(unpacked.Health, unpacked.Stamina);
            }
            if(unpacked.Health == 0u) {
                _ = DelayDie();
            }
        }

        async UniTask DelayDie()
        {
            while(!NetInitComplete) {
                await UniTask.Yield();
            }
            Die(DamageValue.SyncDeath);
        }
        
        public class Data : ComponentData
        {
            public uint Health;
            public ushort Stamina;
            public float StaminaTime;
            public float StaminaAcc;
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKeyValue("hp", value.Health);
                context.Writer.WriteKeyValue("stamina", value.Stamina);
                context.Writer.WriteKeyValue("stamina_time", value.StaminaTime);
                context.Writer.WriteKeyValue("stamina_acc", value.StaminaAcc);
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data = new() {
                    Health      = (uint)context.SerializedValue.GetValue("hp").AsFloat(),
                    Stamina     = (ushort)context.SerializedValue.GetValue("stamina").AsFloat(),
                    StaminaTime = context.SerializedValue.GetValue("stamina_time").AsFloat(),
                    StaminaAcc  = context.SerializedValue.GetValue("stamina_acc").AsFloat()
                };
                return data;
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.Entity;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view) 
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value) 
                => context.SerializeValue((Data)value);
        }
    }
}
