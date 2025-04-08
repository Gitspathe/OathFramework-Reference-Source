using OathFramework.Extensions;
using OathFramework.Core;
using OathFramework.UI.Platform;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OathFramework.UI.Settings
{
    public class RebindNodeComposite4 : RebindNodeBase
    {
        [SerializeField] private Image image1;
        [SerializeField] private Image image2;
        [SerializeField] private Image image3;
        [SerializeField] private Image image4;
        [SerializeField] private TextMeshProUGUI text;

        private void SetImage(ControlSchemes controlScheme, Image image, InputAction action, string composite, int startIndex)
        {
            if(!UIControlsDatabase.TryGetControls(controlScheme, out UIControlsCollectionBase controls)) {
                image.sprite = null;
                return;
            }

            try {
                if(action.TryFindCompositeIndex(composite, startIndex, out int binding)) {
                    action.GetBindingDisplayString(binding, out _, out string controlPath);
                    image.sprite = controls.GetSprite(controlPath);
                    image.color  = image.sprite != null ? Color.white : Color.clear;
                } else {
                    image.sprite = null;
                    image.color  = Color.clear;
                }
            } catch(Exception e) {
                Debug.LogError(e);
                image.sprite = null;
                image.color  = Color.clear;
            }
        }
        
        public override GameObject Setup(RebindingControlSet.Node node, ControlSchemes controlScheme)
        {
            base.Setup(node, controlScheme);
            if(!UIControlsDatabase.TryGetControls(controlScheme, out UIControlsCollectionBase controls)) 
                return gameObject;
            
            InputAction action = node.Action.action;
            int index          = action.GetBindingIndex(InputBinding.MaskByGroup(controls.MaskGroup));
            text.text          = node.DisplayName.GetLocalizedString();
            SetImage(controlScheme, image1, action, node.Composite1, index);
            SetImage(controlScheme, image2, action, node.Composite2, index);
            SetImage(controlScheme, image3, action, node.Composite3, index);
            SetImage(controlScheme, image4, action, node.Composite4, index);
            return gameObject;
        }
    }
}
