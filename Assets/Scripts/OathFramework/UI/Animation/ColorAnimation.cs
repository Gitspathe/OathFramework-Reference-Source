using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI.Animation
{
    public class ColorAnimation : UIAnimation
    {
        [SerializeField] private bool doOverrideColors;
        [SerializeField] private bool processOnEnabled = true;
        
        [ShowIf("@this.doOverrideColors")]
        [SerializeField] private ColorBlock overrideColors;
        
        [SerializeField] private Color hiddenColor = Color.clear;
        
        [SerializeField] private List<Graphic> graphics;

        private ColorBlock colors;

        private void Awake()
        {
            colors = doOverrideColors ? overrideColors : GetComponent<Selectable>().colors;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(!processOnEnabled)
                return;
            
            foreach (Graphic graphic in graphics) {
                graphic.CrossFadeColor(colors.normalColor, 0.0f, true, true);
            }
        }
        
        public override void DoStateTransition(SelectionState state, bool instant)
        {
            Color targetColor;
            switch(state) {
                case SelectionState.Hidden:
                    targetColor = hiddenColor;
                    break;
                case SelectionState.Disabled:
                    targetColor = colors.disabledColor;
                    break;
                case SelectionState.Highlighted:
                    targetColor = colors.highlightedColor;
                    break;
                case SelectionState.Normal:
                    targetColor = colors.normalColor;
                    break;
                case SelectionState.Pressed:
                    targetColor = colors.pressedColor;
                    break;
                case SelectionState.Selected:
                    targetColor = colors.selectedColor;
                    break;
                default:
                    targetColor = Color.white;
                    break;
            }
            
            foreach (Graphic graphic in graphics) {
                graphic.CrossFadeColor(targetColor, instant ? 0f : colors.fadeDuration, true, true);
            }
        }
    }
}
