using OathFramework.EntitySystem;
using OathFramework.EquipmentSystem;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace OathFramework.UI.Info
{
    
    [CreateAssetMenu(fileName = "Equippable UI Info", menuName = "ScriptableObjects/UI Info/Equippable Info", order = 1)]
    public class UIEquippableInfo : UIInfo
    {
                
        [field: SerializeField] public LocalizedString Description     { get; set; }

        
        [field: Space(10)]
        
        [field: SerializeField] public UIEquippableParams[] ParamNodes { get; set; }
        
        public Equippable Template      { get; set; }
        public List<InfoNode> InfoNodes { get; private set; } = new();

        public override string GetDescription(Entity entity)
        {
            return Description.GetLocalizedString();
        }

        public void Setup()
        {
            InfoNodes.Clear();
            foreach(UIEquippableParams node in ParamNodes) {
                InfoNodes.Add(InfoNodeFactory.CreateParamNode(node, Template));
            }
        }

        public new UIEquippableInfo DeepCopy()
        {
            UIEquippableInfo copy = Instantiate(this);
            copy.Title            = Title;
            copy.Description      = Description;
            copy.Icon             = Icon;
            copy.ParamNodes       = ParamNodes;
            copy.Template         = Template;
            copy.Description      = Description;
            return copy;
        }
    }
}
