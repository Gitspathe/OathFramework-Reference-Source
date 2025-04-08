using System;
using UnityEngine.InputSystem;

namespace OathFramework.Extensions
{
    public static class InputActionExtensions
    {
        public static bool TryFindCompositeBinding(this InputAction action, string composite, int startIndex, out InputBinding? binding)
        {
            binding = null;
            int i   = startIndex;
            while(true) {
                if(i > action.bindings.Count)
                    return false;
                if(!action.bindings[i].isPartOfComposite)
                    return false;
                
                if(!string.Equals(action.bindings[i].name, composite, StringComparison.CurrentCultureIgnoreCase)) {
                    i++;
                    continue;
                }
                binding = action.bindings[i];
                return true;
            }
        }
        
        public static bool TryFindCompositeIndex(this InputAction action, string composite, int startIndex, out int bindingIndex)
        {
            bindingIndex = -1;
            int i = startIndex;
            while(true) {
                if(i > action.bindings.Count)
                    return false;
                if(!action.bindings[i].isPartOfComposite)
                    return false;
                
                if(!string.Equals(action.bindings[i].name, composite, StringComparison.CurrentCultureIgnoreCase)) {
                    i++;
                    continue;
                }
                bindingIndex = i;
                return true;
            }
        }
    }
}
