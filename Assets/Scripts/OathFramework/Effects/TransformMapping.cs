using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    [CreateAssetMenu(fileName = "Transform Mapping", menuName = "ScriptableObjects/Transform Mapping", order = 1)]
    public class TransformMapping : ScriptableObject
    {
        [field: SerializeField] public List<TransformData> SourceTransforms { get; private set; } = new();
        [field: SerializeField] public List<TransformData> TargetTransforms { get; private set; } = new();
        
        [Serializable]
        public class TransformData
        {
            [field: SerializeField] public string RelativePathFromRoot { get; set; }
            [field: SerializeField] public int UniqueID                { get; set; }
            [field: SerializeField] public bool Excluded               { get; set; } 

            public string Name {
                get {
                    if(string.IsNullOrEmpty(RelativePathFromRoot))
                        return string.Empty;
                    
                    string[] parts = RelativePathFromRoot.Split('/');
                    return parts.Length == 1 ? parts[0] : parts[parts.Length - 1];
                }
            }
        }
    }
}
