using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI.Platform
{
    public class UIDropdown : MonoBehaviour
    {
        [SerializeField] private Selectable dropdown;
        [SerializeField] private UIDropdownParent parent;

        private void Awake()
        {
            if(dropdown == null) {
                dropdown = GetComponent<Selectable>();
            }
        }

        public void Close()
        {
            if(dropdown != null && dropdown is ICancelHandler cancel) {
                cancel.OnCancel(null);
            }
        }
        
        private void OnEnable()
        {
            parent.OnDropdownOpened(this);
        }

        private void OnDisable()
        {
            parent.OnDropdownClosed(this);
        }
    }
}
