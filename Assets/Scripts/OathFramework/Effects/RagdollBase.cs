using OathFramework.Core;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    public abstract class RagdollBase : LoopComponent
    {
        [field: InfoBox("Ragdoll tree is not generated. This will reduce performance at runtime!", InfoMessageType.Warning, "TransformsNotGenerated")]
        [field: SerializeField] public TransformMapping MappingAsset { get; private set; }
        [field: SerializeField] public Transform Root                { get; private set; }

        [field: SerializeField, ReadOnly] public RagdollTree Tree    { get; private set; } = new();

        private bool TransformsNotGenerated() => Tree.Transforms == null || Tree.Transforms.Length == 0;
        
        [TitleGroup("Transform Mapping", order: -1f), HorizontalGroup("Transform Mapping/Horizontal"), Button("Generate Tree")]
        private void GenerateTree()
        {
            if(Root == null) {
                Debug.LogError("Root is not set.");
                return;
            }
            if(Tree == null) {
                Debug.LogError("Tree is not set.");
                return;
            }
            Tree.Initialize(Root, GetData(), true);
        }

        [TitleGroup("Transform Mapping", order: -1f), HorizontalGroup("Transform Mapping/Horizontal"), Button("Clear Tree")]
        private void ClearTree()
        {
            Tree.Clear();
        }
        
        private void Awake()
        {
            Tree.Initialize(Root, GetData());
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        protected abstract List<TransformMapping.TransformData> GetData();
    }
}
