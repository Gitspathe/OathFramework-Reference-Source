using System;
using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;
using MinMaxGradient = UnityEngine.ParticleSystem.MinMaxGradient;

namespace OathFramework.Persistence
{
    public class AnimationCurveAdapter : IJsonAdapter<AnimationCurve>
    {
        public void Serialize(in JsonSerializationContext<AnimationCurve> context, AnimationCurve value)
        {
            using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
            context.Writer.WriteKey("keyframes");
            using(context.Writer.WriteArrayScope()) {
                foreach(Keyframe keyFrame in value.keys) {
                    using JsonWriter.ObjectScope obj2Scope = context.Writer.WriteObjectScope();
                    context.Writer.WriteKeyValue("value", keyFrame.value);
                    context.Writer.WriteKeyValue("time", keyFrame.time);
                    context.Writer.WriteKeyValue("in_tangent", keyFrame.inTangent);
                    context.Writer.WriteKeyValue("out_tangent", keyFrame.outTangent);
                    context.Writer.WriteKeyValue("in_weight", keyFrame.inWeight);
                    context.Writer.WriteKeyValue("out_weight", keyFrame.outWeight);
                }
            }
        }

        public AnimationCurve Deserialize(in JsonDeserializationContext<AnimationCurve> context)
        {
            List<Keyframe> frames     = new();
            SerializedValueView value = context.SerializedValue;
            SerializedArrayView arr   = value.GetValue("keyframes").AsArrayView();
            foreach(SerializedValueView val in arr) {
                frames.Add(new Keyframe(
                    val.GetValue("value").AsFloat(), 
                    val.GetValue("time").AsFloat(),
                    val.GetValue("in_tangent").AsFloat(),
                    val.GetValue("out_tangent").AsFloat(),
                    val.GetValue("in_weight").AsFloat(),
                    val.GetValue("out_weight").AsFloat()
                ));
            }
            return new AnimationCurve(frames.ToArray());
        }
    }

    public class ColorAdapter : IJsonAdapter<Color>
    {
        public void Serialize(in JsonSerializationContext<Color> context, Color value)
        {
            using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
            context.Writer.WriteKeyValue("r", value.r);
            context.Writer.WriteKeyValue("g", value.g);
            context.Writer.WriteKeyValue("b", value.b);
            context.Writer.WriteKeyValue("a", value.a);
        }

        public Color Deserialize(in JsonDeserializationContext<Color> context)
        {
            SerializedValueView  value = context.SerializedValue;
            SerializedObjectView obj   = value.AsObjectView();
            float r = obj.GetValue("r").AsFloat();
            float g = obj.GetValue("g").AsFloat();
            float b = obj.GetValue("b").AsFloat();
            float a = obj.GetValue("a").AsFloat();
            return new Color(r, g, b, a);
        }
    }

    public class MinMaxGradientAdapter : IJsonAdapter<MinMaxGradient>
    {
        public void Serialize(in JsonSerializationContext<MinMaxGradient> context, MinMaxGradient value)
        {
            using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
            context.Writer.WriteKeyValue("mode", (int)value.mode);
            switch(value.mode) {
                case ParticleSystemGradientMode.Color: {
                    context.SerializeValue("color", value.color);
                } break;
                case ParticleSystemGradientMode.Gradient: {
                    context.SerializeValue("color", value.gradient);
                } break;
                case ParticleSystemGradientMode.TwoColors: {
                    context.SerializeValue("color_1", value.colorMax);
                    context.SerializeValue("color_2", value.colorMin);
                } break;
                case ParticleSystemGradientMode.TwoGradients: {
                    context.SerializeValue("color_1", value.gradientMax);
                    context.SerializeValue("color_2", value.gradientMin);
                } break;
                case ParticleSystemGradientMode.RandomColor: {
                    context.SerializeValue("color_1", value.colorMax);
                    context.SerializeValue("color_2", value.colorMin);
                } break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public MinMaxGradient Deserialize(in JsonDeserializationContext<MinMaxGradient> context)
        {
            SerializedValueView value = context.SerializedValue;
            SerializedObjectView obj  = value.AsObjectView();
            
            MinMaxGradient gradient = new() { mode = (ParticleSystemGradientMode)obj.GetValue("mode").AsInt32() };
            switch(gradient.mode) {
                case ParticleSystemGradientMode.Color: {
                    gradient.color = context.DeserializeValue<Color>(obj.GetValue("color"));
                } break;
                case ParticleSystemGradientMode.Gradient: {
                    gradient.gradient = context.DeserializeValue<Gradient>(obj.GetValue("color"));
                } break;
                case ParticleSystemGradientMode.TwoColors: {
                    gradient.colorMax = context.DeserializeValue<Color>(obj.GetValue("color_1"));
                    gradient.colorMin = context.DeserializeValue<Color>(obj.GetValue("color_2"));
                } break;
                case ParticleSystemGradientMode.TwoGradients: {
                    gradient.gradientMax = context.DeserializeValue<Gradient>(obj.GetValue("color_1"));
                    gradient.gradientMin = context.DeserializeValue<Gradient>(obj.GetValue("color_2"));
                } break;
                case ParticleSystemGradientMode.RandomColor: {
                    gradient.colorMax = context.DeserializeValue<Color>(obj.GetValue("color_1"));
                    gradient.colorMin = context.DeserializeValue<Color>(obj.GetValue("color_2"));
                } break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return gradient;
        }
    }

    public class GradientAdapter : IJsonAdapter<Gradient>
    {
        public void Serialize(in JsonSerializationContext<Gradient> context, Gradient value)
        {
            using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
            context.Writer.WriteKeyValue("mode", (int)value.mode);
            context.Writer.WriteKeyValue("color_space", (int)value.colorSpace);
            context.Writer.WriteKey("color_keys");
            using(context.Writer.WriteArrayScope()) {
                foreach(GradientColorKey cKey in value.colorKeys) {
                    context.SerializeValue(cKey);
                }
            }
            context.Writer.WriteKey("alpha_keys");
            using(context.Writer.WriteArrayScope()) {
                foreach(GradientAlphaKey aKey in value.alphaKeys) {
                    context.SerializeValue(aKey);
                }
            }
        }

        public Gradient Deserialize(in JsonDeserializationContext<Gradient> context)
        {
            List<GradientColorKey> colorKeys = new();
            List<GradientAlphaKey> alphaKeys = new();
            
            SerializedValueView value = context.SerializedValue;
            SerializedObjectView obj  = value.AsObjectView();
            
            GradientMode mode            = (GradientMode)obj.GetValue("mode").AsInt32();
            ColorSpace colorSpace        = (ColorSpace)obj.GetValue("color_space").AsInt32();
            SerializedArrayView colArr   = value.GetValue("color_keys").AsArrayView();
            SerializedArrayView alphaArr = value.GetValue("alpha_keys").AsArrayView();
            foreach(SerializedValueView col in colArr) {
                colorKeys.Add(context.DeserializeValue<GradientColorKey>(col));
            }
            foreach(SerializedValueView a in alphaArr) {
                alphaKeys.Add(context.DeserializeValue<GradientAlphaKey>(a));
            }
            
            Gradient gradient = new() { mode = mode, colorSpace = colorSpace };
            gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
            return gradient;
        }
    }
    
    public class GradientColorKeyAdapter : IJsonAdapter<GradientColorKey>
    {
        public void Serialize(in JsonSerializationContext<GradientColorKey> context, GradientColorKey value)
        {
            using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
            context.Writer.WriteKeyValue("t", value.time);
            context.SerializeValue("color", value.color);
        }

        public GradientColorKey Deserialize(in JsonDeserializationContext<GradientColorKey> context)
        {
            SerializedValueView  value = context.SerializedValue;
            SerializedObjectView obj   = value.AsObjectView();
            float t   = obj.GetValue("t").AsFloat();
            Color col = context.DeserializeValue<Color>(obj.GetValue("color"));
            return new GradientColorKey(col, t);
        }
    }
    
    public class GradientAlphaKeyAdapter : IJsonAdapter<GradientAlphaKey>
    {
        public void Serialize(in JsonSerializationContext<GradientAlphaKey> context, GradientAlphaKey value)
        {
            using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
            context.Writer.WriteKeyValue("t", value.time);
            context.SerializeValue("a", value.alpha);
        }

        public GradientAlphaKey Deserialize(in JsonDeserializationContext<GradientAlphaKey> context)
        {
            SerializedValueView  value = context.SerializedValue;
            SerializedObjectView obj   = value.AsObjectView();
            float t = obj.GetValue("t").AsFloat();
            float a = context.DeserializeValue<float>(obj.GetValue("a"));
            return new GradientAlphaKey(a, t);
        }
    }
}
