using OathFramework.Core;
using OathFramework.UI.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OathFramework.UI.Settings
{
    public class RebindNode : RebindNodeBase
    {
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI text;
        
        public override GameObject Setup(RebindingControlSet.Node node, ControlSchemes controlScheme)
        {
            base.Setup(node, controlScheme);
            if(!UIControlsDatabase.TryGetControls(controlScheme, out UIControlsCollectionBase controls))
                return gameObject;
            
            InputAction action = node.Action.action;
            int index          = action.GetBindingIndex(InputBinding.MaskByGroup(controls.MaskGroup));
            action.GetBindingDisplayString(index, out _, out string controlPath);
            
            //Debug.Log(controlPath);
            
            image.sprite = controls.GetSprite(controlPath);
            image.color  = image.sprite == null ? Color.clear : Color.white;
            text.text    = node.DisplayName.GetLocalizedString();
            return gameObject;
        }
    }
}
