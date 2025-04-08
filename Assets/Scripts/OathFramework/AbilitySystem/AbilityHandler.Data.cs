using OathFramework.EntitySystem;
using OathFramework.Persistence;
using OathFramework.Utility;
using Unity.Serialization.Json;

namespace OathFramework.AbilitySystem
{
    public partial class AbilityHandler
    {
        string IPersistableComponent.PersistableID                          => PersistenceLookup.Name.Abilities;
        uint IPersistableComponent.Order                                    => PersistenceLookup.LoadOrder.Abilities;
        VerificationFailAction IPersistableComponent.VerificationFailAction => VerificationFailAction.Abort;

        ComponentData IPersistableComponent.GetPersistenceData()
        {
            Data data = new();
            foreach(EntityAbility ability in abilities.Current) {
                if(!ability.Ability.AutoPersistenceSync)
                    continue;

                data.Abilities.Add(ability);
            }
            return data;
        }

        bool IPersistableComponent.Verify(ComponentData data, PersistentProxy proxy)
        {
            return true;
        }

        void IPersistableComponent.ApplyPersistenceData(ComponentData data, PersistentProxy proxy)
        {
            Data unpacked = (Data)data;
            if(IsOwner) {
                int count = unpacked.Abilities.Count;
                for(int i = 0; i < count; i++) {
                    SetAbility(unpacked.Abilities.Array[i], false, false);
                }
                return;
            }
            SyncPersistenceOwnerRpc(new SyncData(unpacked.Abilities));
        }

        public class Data : ComponentData
        {
            public QList<EntityAbility> Abilities = new();
        }

        public class JsonAdapter : IJsonAdapter<Data>
        {
            public void Serialize(in JsonSerializationContext<Data> context, Data value)
            {
                context.Writer.WriteKey("abilities");
                using JsonWriter.ArrayScope arrScope = context.Writer.WriteArrayScope();
                int count = value.Abilities.Count;
                for(int i = 0; i < count; i++) {
                    EntityAbility state = value.Abilities.Array[i];
                    using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                    context.Writer.WriteKeyValue("id", state.Ability.LookupKey);
                    context.Writer.WriteKeyValue("cooldown", state.Cooldown);
                    context.Writer.WriteKeyValue("charge_progress", state.ChargeProgress);
                    context.Writer.WriteKeyValue("charges", state.Charges);
                }
            }

            public Data Deserialize(in JsonDeserializationContext<Data> context)
            {
                Data data = new();
                foreach(SerializedValueView state in context.SerializedValue.GetValue("abilities").AsArrayView()) {
                    string id        = state.GetValue("id").ToString();
                    float cooldown   = state.GetValue("cooldown").AsFloat();
                    float chargeProg = state.GetValue("charge_progress").AsFloat();
                    byte charges     = (byte)state.GetValue("charges").AsInt32();
                    data.Abilities.Add(new EntityAbility(id, cooldown, chargeProg, charges));
                }
                return data;
            }
        }

        public class Adapter : ComponentAdapter
        {
            public override string ID => PersistenceLookup.Name.Abilities;
            public override void OnInitialize() => JsonSerialization.AddGlobalAdapter(new JsonAdapter());
            public override ComponentData Deserialize<T>(in JsonDeserializationContext<T> context, SerializedValueView view)
                => context.DeserializeValue<Data>(view);
            public override void Serialize<T>(in JsonSerializationContext<T> context, ComponentData value)
                => context.SerializeValue((Data)value);
        }
    }
}
