using UnityEngine;

namespace OathFramework.UI.Platform
{
    public class UIDropdownParent : MonoBehaviour
    {
        public UIDropdown CurrentDropdown { get; private set; }

        public void Close()
        {
            if(CurrentDropdown == null)
                return;
            
            CurrentDropdown.Close();
        }
        
        public void OnDropdownOpened(UIDropdown dropdown)
        {
            CurrentDropdown = dropdown;
        }

        public void OnDropdownClosed(UIDropdown dropdown)
        {
            if(CurrentDropdown == dropdown) {
                CurrentDropdown = null;
            }
        }
    }
}
