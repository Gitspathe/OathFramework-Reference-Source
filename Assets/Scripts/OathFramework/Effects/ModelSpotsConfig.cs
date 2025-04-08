using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Model Spots Config", menuName = "ScriptableObjects/Model Spots Config", order = 1)]
    public class ModelSpotsConfig : ScriptableObject
    {
        [field: SerializeField] public ModelSpotParams[] @params { get; private set; }
        
        private Dictionary<byte, ModelSpotParams> dict;

        private void ConstructDictionary()
        {
            if(dict != null)
                return;

            dict = new Dictionary<byte, ModelSpotParams>();
            foreach(ModelSpotParams param in @params) {
                dict.Add(param.ID, param);
            }
        }

        public byte? GetFallback(byte modelSpot)
        {
            ConstructDictionary();
            if(dict.TryGetValue(modelSpot, out ModelSpotParams @params))
                return @params.ID;
            
            return null;
        }
    }
    
    [Serializable]
    public class ModelSpotParams
    {
        [field: SerializeField] public byte ID       { get; private set; } = 1;
        [field: SerializeField] public string Name   { get; private set; }
        [field: SerializeField] public byte Fallback { get; private set; }
    }
}
