using OathFramework.EntitySystem;
using OathFramework.Pooling;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    public class CopyableModelPlug : MonoBehaviour
    {
        private ModelPlugType type;
        private Effect effect;
        private Prop prop;

        private List<ICopyableModelPlugComponent> copyableComponents = new();
        
        private void Awake()
        {
            effect = GetComponent<Effect>();
            prop   = GetComponent<Prop>();
            copyableComponents.AddRange(GetComponentsInChildren<ICopyableModelPlugComponent>(true));
            foreach(ICopyableModelPlugComponent c in copyableComponents) {
                c.Initialize();
            }
            if(effect != null) {
                type = ModelPlugType.Effect;
                return;
            }
            if(prop != null) {
                type = ModelPlugType.Prop;
                return;
            }
            Debug.LogError($"{name} has a {nameof(CopyableModelPlug)} component, but no effect or prop.");
        }

        public CopyData GetData()
        {
            Dictionary<string, ICopyableModelPlugComponentData> cData = StaticObjectPool<Dictionary<string, ICopyableModelPlugComponentData>>.Retrieve();
            foreach(ICopyableModelPlugComponent c in copyableComponents) {
                cData.Add(c.ID, c.GetData());
            }
            switch(type) {
                case ModelPlugType.Effect: {
                    return new CopyData(
                        ModelPlugType.Effect,
                        effect.Params.ID,
                        effect.Source,
                        effect.CurrentSpot,
                        effect.CurDuration,
                        effect.Dissipation,
                        effect.GetComponent<ColorableEffect>()?.CurrentColor,
                        effect.ExtraData, 
                        cData
                    );
                }
                case ModelPlugType.Prop: {
                    return new CopyData(
                        ModelPlugType.Prop,
                        prop.Params.ID,
                        null,
                        prop.CurrentSpot,
                        0.0f,
                        prop.Dissipation,
                        prop.GetComponent<ColorableEffect>()?.CurrentColor,
                        0,
                        cData
                    );
                }

                default: {
                    Debug.LogError($"Invalid {nameof(ModelPlugType)}");
                    return default;
                }
            }
        }

        public static IModelPlug InitializeCopy(ModelSocketHandler targetSockets, in CopyData data, bool returnToPool = true)
        {
            try {
                switch(data.Type) {
                    case ModelPlugType.Effect: {
                        Effect e = EffectManager.Retrieve(
                            data.PrefabID,
                            data.Source,
                            sockets: targetSockets,
                            modelSpot: data.ModelSpot,
                            extraData: data.ExtraData
                        );
                        e.AssignCopyData(in data);
                        return e;
                    }
                    case ModelPlugType.Prop: {
                        Prop p = PropManager.Retrieve(
                            data.PrefabID,
                            sockets: targetSockets,
                            modelSpot: data.ModelSpot
                        );
                        p.AssignCopyData(in data);
                        return p;
                    }

                    default: {
                        Debug.LogError($"Invalid {nameof(ModelPlugType)}");
                        return null;
                    }
                }
            } finally {
                if(returnToPool) {
                    data.ComponentData.Clear();
                    StaticObjectPool<Dictionary<string, ICopyableModelPlugComponentData>>.Return(data.ComponentData);
                }
            }
        }

        public readonly struct CopyData
        {
            public readonly ModelPlugType Type;
            public readonly ushort PrefabID;
            public readonly IEntity Source;
            public readonly byte ModelSpot;
            public readonly float CurDuration;
            public readonly float CurDissipation;
            public readonly ParticleSystem.MinMaxGradient? Color;
            public readonly ushort ExtraData;
            
            public readonly Dictionary<string, ICopyableModelPlugComponentData> ComponentData;

            public CopyData(
                ModelPlugType type,
                ushort prefabID,
                IEntity source,
                byte modelSpot, 
                float curDuration, 
                float dissipation, 
                ParticleSystem.MinMaxGradient? color, 
                ushort extraData, 
                Dictionary<string, ICopyableModelPlugComponentData> componentData)
            {
                Type           = type;
                PrefabID       = prefabID;
                Source         = source;
                ModelSpot      = modelSpot;
                CurDuration    = curDuration;
                CurDissipation = dissipation;
                Color          = color;
                ExtraData      = extraData;
                ComponentData  = componentData;
            }
        }
    }
    
    public interface ICopyableModelPlugComponent
    {
        string ID { get; }
        void Initialize();
        ICopyableModelPlugComponentData GetData();
        void ApplyData(ICopyableModelPlugComponentData data);
    }

    public interface ICopyableModelPlugComponentData { }
}
