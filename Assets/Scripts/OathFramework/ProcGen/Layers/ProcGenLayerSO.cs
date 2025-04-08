using Sirenix.OdinInspector;
using UnityEngine;

namespace OathFramework.ProcGen.Layers
{
    [CreateAssetMenu(fileName = "Layer", menuName = "ScriptableObjects/ProcGen/Layer", order = 1)]
    public class ProcGenLayerSO : ScriptableObject
    {
        [field: SerializeReference, InlineProperty, HideLabel]
        public ProcGenLayer Data { get; private set; }
    }
}
