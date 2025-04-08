using OathFramework.EntitySystem;
using Unity.Serialization.Json;

namespace OathFramework.Effects
{
    public partial class HitEffectInfo
    {
        public class JsonAdapter : IJsonAdapter<HitEffectInfo>
        {
            public void Serialize(in JsonSerializationContext<HitEffectInfo> context, HitEffectInfo value)
            {
                using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
                context.Writer.WriteKeyValue("materials", (uint)value.materials);
                context.Writer.WriteKeyValue("params", value.hitEffectParams);
            }

            public HitEffectInfo Deserialize(in JsonDeserializationContext<HitEffectInfo> context)
            {
                return new HitEffectInfo {
                    materials = (HitSurfaceMaterial)context.SerializedValue.GetValue("materials").AsUInt64(),
                    hitEffectParams = context.SerializedValue.GetValue("params").ToString()
                };
            }
        }
    }
}
