using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    public class RagdollSource : RagdollBase
    {
        protected override List<TransformMapping.TransformData> GetData() => MappingAsset.SourceTransforms;
    }
}
