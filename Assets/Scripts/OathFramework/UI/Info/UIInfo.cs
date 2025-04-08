using OathFramework.EntitySystem;
using OathFramework.UI.Builds;
using UnityEngine;
using UnityEngine.Localization;

// ReSharper disable SuggestVarOrType_SimpleTypes

namespace OathFramework.UI.Info
{
    
    [CreateAssetMenu(fileName = "UI Info", menuName = "ScriptableObjects/UI Info/Info", order = 1)]
    public class UIInfo : ScriptableObject, IDetailsViewObject
    {
        [field: SerializeField] public LocalizedString Title { get; set; }
        [field: SerializeField] public Sprite Icon           { get; set; }
        
        string IDetailsViewObject.Title => Title?.GetLocalizedString();
        string IDetailsViewObject.GetDescription(Entity entity) => GetDescription(entity);
        
        public virtual string GetDescription(Entity entity) => "";

        public UIInfo DeepCopy()
        {
            var copy   = Instantiate(this);
            copy.Icon  = Icon;
            copy.Title = Title;
            return copy;
        }
    }

}
