using OathFramework.Core;
using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.UI.Animation
{
    public abstract class UIAnimation : LoopComponent, ITweenShowCallback, ITweenHideCallback
    {
        [SerializeField] private UITweenCallbackController tweenController;
        
        uint ILockableOrderedListElement.Order => 1;

        private void Awake()
        {
            if(tweenController == null)
                return;
            
            tweenController.Register((ITweenShowCallback)this);
            tweenController.Register((ITweenHideCallback)this);
        }

        private void OnDestroy()
        {
            if(tweenController == null)
                return;
            
            tweenController.Unregister((ITweenShowCallback)this);
            tweenController.Unregister((ITweenHideCallback)this);
        }

        public abstract void DoStateTransition(SelectionState state, bool instant);
        
        public enum SelectionState
        {
            Normal      = 0,
            Highlighted = 1,
            Pressed     = 2,
            Selected    = 3,
            Disabled    = 4,
            Hidden      = 99 // To cast from Unity state, hidden is at the end.
        }

        void ITweenShowCallback.OnShow()
        {
            DoStateTransition(SelectionState.Normal, false);
        }

        void ITweenHideCallback.OnHide()
        {
            DoStateTransition(SelectionState.Hidden, false);
        }
    }
}
