using Unity.Serialization.Json;
using UnityEngine;

namespace OathFramework.Persistence
{
    public struct TransformData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public TransformData(Transform transform)
        {
            Position = transform.position;
            Rotation = transform.rotation;
            Scale    = transform.localScale;
        }
    }
    
    public class TransformDataAdapter : IJsonAdapter<TransformData>
    {
        public void Serialize(in JsonSerializationContext<TransformData> context, TransformData value)
        {
            using JsonWriter.ObjectScope obj1Scope = context.Writer.WriteObjectScope();
            context.Writer.WriteKey("pos");
            using(context.Writer.WriteObjectScope()) {
                context.Writer.WriteKeyValue("x", value.Position.x);
                context.Writer.WriteKeyValue("y", value.Position.y);
                context.Writer.WriteKeyValue("z", value.Position.z);
            }
            context.Writer.WriteKey("rot");
            using(context.Writer.WriteObjectScope()) {
                context.Writer.WriteKeyValue("x", value.Rotation.x);
                context.Writer.WriteKeyValue("y", value.Rotation.y);
                context.Writer.WriteKeyValue("z", value.Rotation.z);
                context.Writer.WriteKeyValue("w", value.Rotation.w);
            }
            context.Writer.WriteKey("scale");
            using(context.Writer.WriteObjectScope()) {
                context.Writer.WriteKeyValue("x", value.Scale.x);
                context.Writer.WriteKeyValue("y", value.Scale.y);
                context.Writer.WriteKeyValue("z", value.Scale.z);
            }
        }

        public TransformData Deserialize(in JsonDeserializationContext<TransformData> context)
        {
            TransformData data         = new();
            SerializedValueView value  = context.SerializedValue;
            SerializedObjectView pView = value.GetValue("pos").AsObjectView();
            SerializedObjectView rView = value.GetValue("rot").AsObjectView();
            SerializedObjectView sView = value.GetValue("scale").AsObjectView();
            data.Position = new Vector3(
                pView.GetValue("x").AsFloat(), 
                pView.GetValue("y").AsFloat(), 
                pView.GetValue("z").AsFloat()
            );
            data.Rotation = new Quaternion(
                rView.GetValue("x").AsFloat(),
                rView.GetValue("y").AsFloat(),
                rView.GetValue("z").AsFloat(),
                rView.GetValue("w").AsFloat()
            );
            data.Scale = new Vector3(
                sView.GetValue("x").AsFloat(), 
                sView.GetValue("y").AsFloat(), 
                sView.GetValue("z").AsFloat()
            );
            return data;
        }
    }
}
