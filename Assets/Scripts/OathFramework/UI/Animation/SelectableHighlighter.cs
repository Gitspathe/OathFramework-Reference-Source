using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.UI.Animation
{

    public class SelectableHighlighter : MonoBehaviour, ITweenShowCallback, ITweenHideCallback
    {
        [SerializeField] private bool automatic = true;
        [SerializeField] private UIAnimation[] targets;
        
        uint ILockableOrderedListElement.Order => 3;

        private void OnEnable()
        {
            if(!automatic)
                return;

            SetHighlighted(true);
        }

        private void OnDisable()
        {
            if(!automatic)
                return;

            SetHighlighted(false);
        }

        public void SetHighlighted(bool val)
        {
            if(val) {
                foreach(UIAnimation anim in targets) {
                    anim.DoStateTransition(UIAnimation.SelectionState.Highlighted, true);
                }
            } else {
                foreach(UIAnimation anim in targets) {
                    anim.DoStateTransition(UIAnimation.SelectionState.Normal, true);
                }
            }
        }

        void ITweenShowCallback.OnShow()
        {
            SetHighlighted(true);
        }

        void ITweenHideCallback.OnHide()
        {
            SetHighlighted(false);
        }
    }

}
