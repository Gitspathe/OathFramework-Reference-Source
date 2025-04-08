using OathFramework.AbilitySystem;
using OathFramework.Audio;
using OathFramework.EntitySystem;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace OathFramework.UI.Info
{
    [CreateAssetMenu(fileName = "Ability Info", menuName = "ScriptableObjects/Info/Ability Info", order = 1)]
    public class AbilityInfo : UIInfo
    {
        [field: SerializeField] public LocalizedString Description     { get; set; }
        [field: SerializeField] public string AbilityKey               { get; private set; }
        [field: SerializeField] public List<EntityAudioClip> AudioData { get; private set; }
        
        public Ability Ability { get; set; }
        
        public override string GetDescription(Entity entity)
        {
            Description.Arguments = null;
            Dictionary<string, string> @params = Ability.GetLocalizedParams(entity);
            if(@params != null && @params.Count > 0) {
                Description.Arguments = new List<object> { @params };
            }
            Description.RefreshString();
            return Description.GetLocalizedString();
        }

        public new AbilityInfo DeepCopy()
        {
            AbilityInfo copy = Instantiate(this);
            copy.Title         = Title;
            copy.Description   = Description;
            copy.Icon          = Icon;
            copy.Description   = Description;
            return copy;
        }
    }
}
