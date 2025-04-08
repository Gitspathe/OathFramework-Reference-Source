using OathFramework.Effects;
using OathFramework.Persistence;
using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{
    public partial class StdBulletData
    {
        public class Adapter : IJsonAdapter<StdBulletData>
        {
            public void Serialize(in JsonSerializationContext<StdBulletData> context, StdBulletData value)
            {
                string source = "";
                if(value.Source != null) {
                    PersistenceManager.TryGetObjectID(value.Source as Entity, out source);
                }

                using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
                context.Writer.WriteKeyValue("source", source);
                context.Writer.WriteKeyValue("equippableID", value.EquippableID);
                context.Writer.WriteKeyValue("vfx", value.VFX.Key);
                context.SerializeValue("color", value.EffectColor);
                context.Writer.WriteKeyValue("base_damage", value.BaseDamage);
                context.Writer.WriteKeyValue("speed", value.Speed);
                context.Writer.WriteKeyValue("penetration", value.Penetration);
                context.Writer.WriteKeyValue("min_distance", value.MinDistance);
                context.Writer.WriteKeyValue("max_distance", value.MaxDistance);
                context.Writer.WriteKeyValue("stagger_strength", (int)value.StaggerStrength);
                context.Writer.WriteKeyValue("stagger_amount", value.StaggerAmount);
                context.SerializeValue("distance_mod", value.DistanceMod);
                context.Writer.WriteKey("effect_overrides");
                using(context.Writer.WriteArrayScope()) {
                    foreach(HitEffectInfo hitEffectInfo in value.EffectOverrides) {
                        context.SerializeValue(hitEffectInfo);
                    }
                }
            }

            public StdBulletData Deserialize(in JsonDeserializationContext<StdBulletData> context)
            {
                SerializedValueView value = context.SerializedValue;

                EffectParams vfx = null;
                if(EffectManager.TryGetParams(value.GetValue("vfx").ToString(), out EffectParams foundVFX)) {
                    vfx = foundVFX;
                }

                ParticleSystem.MinMaxGradient color = context.DeserializeValue<ParticleSystem.MinMaxGradient>(value.GetValue("color"));
                
                Entity source             = null;
                string sourceStr          = value.GetValue("source").ToString();
                if(!string.IsNullOrEmpty(sourceStr)) {
                    PersistenceManager.TryGetObjectBehaviour(sourceStr, out source);
                }

                List<HitEffectInfo> overrides = new();
                SerializedArrayView arr       = value.GetValue("effect_overrides").AsArrayView();
                foreach(SerializedValueView val in arr) {
                    overrides.Add(context.DeserializeValue<HitEffectInfo>(val));
                }

                StdBulletData data = Retrieve();
                data.SetData(
                    source,
                    (ushort)value.GetValue("equippable_id").AsFloat(),
                    vfx,
                    color,
                    (ushort)value.GetValue("base_damage").AsFloat(),
                    value.GetValue("speed").AsFloat(),
                    value.GetValue("penetration").AsFloat(),
                    value.GetValue("min_distance").AsFloat(),
                    value.GetValue("max_distance").AsFloat(),
                    (StaggerStrength)value.GetValue("stagger_strength").AsInt32(),
                    (ushort)value.GetValue("stagger_amount").AsFloat(),
                    context.DeserializeValue<AnimationCurve>(value.GetValue("distance_mod")),
                    overrides.Count == 0 ? null : overrides
                );
                return data;
            }
        }
    }
}
