using OathFramework.Pooling;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    [CreateAssetMenu(fileName = "Entity Params", menuName = "ScriptableObjects/Entity Params", order = 1)]
    public class EntityParams : ScriptableObject
    {
        [NonSerialized] private bool initialized;
        
        [field: SerializeField] public string LookupKey            { get; private set; }
        [field: SerializeField] public ushort DefaultID            { get; private set; }
        [field: SerializeField] public bool IsPooled               { get; private set; } = true;
        
        [field: SerializeField, ShowIf("@IsPooled")] 
        public PoolParams Pool                                     { get; private set; }

        [field: Space(5)]
        
        [field: SerializeField] public DeathEffects[] DeathEffects { get; private set; }
        
        [field: Space(5)]
        
        [field: SerializeField] public Stats BaseStats             { get; private set; }
        
        [field: Space(5)]
        
        [field: SerializeField]
        public ColorableEffectOverride[] EffectColorOverrides      { get; private set; }
        
        public ushort ID { get; set; }

        private Dictionary<HitSurfaceMaterial, ColorableEffectOverride> effectOverridesDict = new();
        
        public void Initialize()
        {
            if(initialized)
                return;
            
            BaseStats.InitializeParams();
            foreach(ColorableEffectOverride @override in EffectColorOverrides) {
                effectOverridesDict.Add(@override.Material, @override);
            }
            initialized = true;
        }

        public bool TryGetEffectColorOverride(HitSurfaceMaterial material, out Color? color)
        {
            color = null;
            if(!effectOverridesDict.TryGetValue(material, out ColorableEffectOverride @override))
                return false;

            color = @override.Color;
            return true;
        }
    }

    [Serializable]
    public class ColorableEffectOverride
    {
        [field: SerializeField] public HitSurfaceMaterial Material { get; private set; }
        
        [field: ColorUsage(true, true)]
        [field: SerializeField] public Color Color                 { get; private set; }
    }
    
    public enum DeathEffects
    {
        None         = 0,
        VisualEffect = 1,
        Animation    = 2,
        Ragdoll      = 3,
    }
}
