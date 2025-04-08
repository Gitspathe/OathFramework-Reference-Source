using UnityEngine;

namespace OathFramework.UI.Info
{
    [CreateAssetMenu(fileName = "StatParam UI Info Collection", menuName = "ScriptableObjects/UI Info/StatParam Info Collection", order = 1)]
    public class UIStatParamInfoCollection : ScriptableObject
    {
        [field: SerializeField] public UIStatParamInfo[] Collection { get; private set; }
    }
}
