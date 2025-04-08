using OathFramework.UI.Platform;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.UI
{

    public class NavGroupToggle : MonoBehaviour, ITweenShowCallback, ITweenHideCallback
    {
        [SerializeField] private bool automatic = true;
        [SerializeField] private bool toggleState;
        [SerializeField] private UINavigationGroup[] targets;
        
        uint ILockableOrderedListElement.Order => 2;
        
        private void OnEnable()
        {
            if(!automatic)
                return;
            
            SetNavigation(false);
        }

        private void OnDisable()
        {
            if(!automatic)
                return;
            
            SetNavigation(true);
        }

        public void SetNavigation(bool val)
        {
            foreach(UINavigationGroup navGroup in targets) {
                navGroup.SetNavigation(val);
            }
        }

        void ITweenShowCallback.OnShow()
        {
            SetNavigation(toggleState);
        }

        void ITweenHideCallback.OnHide()
        {
            SetNavigation(!toggleState);
        }
    }

}
