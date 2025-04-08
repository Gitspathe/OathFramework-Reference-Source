using OathFramework.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace OathFramework.UI.Platform
{

    public class UIControlsDatabase : MonoBehaviour
    {
        [SerializeField] private GameObject infoNodeSpritePrefab;
        [SerializeField] private GameObject infoNodeTextPrefab;
        [SerializeField] private GameObject infoNodeDivPrefab;
        
        [Space(10)]
        
        [SerializeField] private UIControlsCollectionBase[] controls;

        public static GameObject InfoNodeSpritePrefab => Instance.infoNodeSpritePrefab;
        public static GameObject InfoNodeTextPrefab   => Instance.infoNodeTextPrefab;
        public static GameObject InfoNodeDivPrefab    => Instance.infoNodeDivPrefab;

        public static UIControlsDatabase Instance { get; private set; }

        public UIControlsDatabase Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(UIControlsDatabase)} singleton.");
                Destroy(Instance);
            }
            Instance = this;
            
            foreach(UIControlsCollectionBase collection in controls) {
                collection.Initialize();
            }
            return this;
        }

        public static bool TryGetCurrentControls(out UIControlsCollectionBase controls)
        {
            controls = null;
            if(GameControls.ControlScheme == ControlSchemes.None)
                return false;

            return GameControls.UsingController 
                ? TryGetGamepadControls(GameControls.GamepadType, out controls) 
                : TryGetControls(GameControls.ControlScheme, out controls);
        }

        public static bool TryGetControls(ControlSchemes scheme, out UIControlsCollectionBase controls)
        {
            controls = null;
            if(scheme == ControlSchemes.Gamepad)
                return TryGetGamepadControls(GameControls.GamepadType == GamepadTypes.None ? GamepadTypes.Generic : GameControls.GamepadType, out controls);
            
            foreach (UIControlsCollectionBase iControls in Instance.controls) {
                if(iControls.GetControlSchemeID() != scheme)
                    continue;
                
                controls = iControls;
                return true;
            }
            return false;
        }

        public static bool TryGetGamepadControls(GamepadTypes gamepadType, out UIControlsCollectionBase controls)
        {
            controls = null;
            foreach (UIControlsCollectionBase iControls in Instance.controls) {
                if(iControls.GetControlSchemeID() == ControlSchemes.Gamepad 
                   && iControls is UIGamepadControlsCollection gamepadControls
                   && gamepadControls.GamepadType == gamepadType) {
                    controls = iControls;
                    return true;
                }
            }
            return false;
        }
    }

}
