using OathFramework.UI;
using UnityEngine;

namespace OathFramework.Utility
{

    public class ComponentDisabler : MonoBehaviour, ITweenShowCallback, ITweenHideCallback
    {
        [SerializeField] private bool automatic = true;
        [SerializeField] private MonoBehaviour[] components;
        
        uint ILockableOrderedListElement.Order => 0;

        private void OnEnable()
        {
            if(!automatic)
                return;
            
            SetActive(false);
        }

        private void OnDisable()
        {
            if(!automatic)
                return;
            
            SetActive(true);
        }

        public void SetActive(bool val)
        {
            foreach(MonoBehaviour comp in components) {
                comp.enabled = val;
            }
        }

        void ITweenShowCallback.OnShow()
        {
            SetActive(false);
        }

        void ITweenHideCallback.OnHide()
        {
            SetActive(true);
        }
    }

}
