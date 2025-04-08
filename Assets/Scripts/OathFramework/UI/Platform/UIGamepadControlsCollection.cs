using OathFramework.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.UI.Platform
{

    [CreateAssetMenu(fileName = "UI Controls Collection", menuName = "ScriptableObjects/Platform/UIGamepadControlsCollection", order = 1)]
    public class UIGamepadControlsCollection : UIControlsCollectionBase
    {
        [field: SerializeField] public GamepadTypes GamepadType { get; private set; }
        
        public override ControlSchemes GetControlSchemeID() => ControlSchemes.Gamepad;
    }

}
