using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace OathFramework.UI.Info
{
    [CreateAssetMenu(fileName = "Perk UI Info", menuName = "ScriptableObjects/UI Info/Perk Info", order = 1)]
    public class UIPerkInfo : UIInfo
    {
        [field: SerializeField] public LocalizedString Description { get; set; }
        
        [field: Space(10)]

        [field: SerializeField] public string PerkKey { get; private set; }
        
        public Perk Perk { get; set; }
        
        public override string GetDescription(Entity entity)
        {
            Description.Arguments = null;
            Dictionary<string, string> @params = Perk.GetLocalizedParams(entity);
            if(@params != null && @params.Count > 0) {
                Description.Arguments = new List<object> { @params };
            }
            Description.RefreshString();
            return Description.GetLocalizedString();
        }

        public new UIPerkInfo DeepCopy()
        {
            UIPerkInfo copy  = Instantiate(this);
            copy.Title       = Title;
            copy.Description = Description;
            copy.Icon        = Icon;
            copy.Perk        = Perk;
            return copy;
        }
    }
}
