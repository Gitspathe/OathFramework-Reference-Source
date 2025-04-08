using UnityEngine;

namespace OathFramework.UI.Info
{
    [CreateAssetMenu(fileName = "StatParam UI Info", menuName = "ScriptableObjects/UI Info/StatParam Info", order = 1)]
    public class UIStatParamInfo : UIInfo
    {
        [field: SerializeField] public string StatParamKey { get; private set; }
        
        public new UIStatParamInfo DeepCopy()
        {
            UIStatParamInfo copy = Instantiate(this);
            copy.Title           = Title;
            return copy;
        }
    }
}
