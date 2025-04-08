using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using Debug = UnityEngine.Debug;

namespace OathFramework.Core
{
    public sealed class GameControls : Subsystem
    {
        [SerializeField] private GameObject leftTouchStick;
        [SerializeField] private GameObject rightTouchStick;
        [SerializeField] private InputSystemUIInputModule uiInputSystem;
        [SerializeField] private InputActionReference leftGamepadStick;
        [SerializeField] private InputActionReference rightGamepadStick;

        private static StickControl leftTouchStickControl;
        private static StickControl rightTouchStickControl;

        public static bool LeftStickPressed {
            get {
                switch(ControlScheme) {
                    case ControlSchemes.None:
                        return false;
                    case ControlSchemes.Keyboard:
                        return false;
                    case ControlSchemes.Touch:
                        return leftTouchStickControl.IsPressed();
                    case ControlSchemes.Gamepad:
                        return Instance.leftGamepadStick.action.IsPressed();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        public static bool RightStickPressed {
            get {
                switch(ControlScheme) {
                    case ControlSchemes.None:
                        return false;
                    case ControlSchemes.Keyboard:
                        return false;
                    case ControlSchemes.Touch:
                        return rightTouchStickControl.IsPressed();
                    case ControlSchemes.Gamepad:
                        return Instance.rightGamepadStick.action.IsPressed();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static Vector2 LeftStickValue {
            get {
                switch(ControlScheme) {
                    case ControlSchemes.None:
                        return Vector2.zero;
                    case ControlSchemes.Keyboard:
                        return Vector2.zero;
                    case ControlSchemes.Touch:
                        return leftTouchStickControl.ReadValue();
                    case ControlSchemes.Gamepad:
                        return Instance.leftGamepadStick.action.ReadValue<Vector2>();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        public static Vector2 RightStickValue {
            get {
                switch(ControlScheme) {
                    case ControlSchemes.None:
                        return Vector2.zero;
                    case ControlSchemes.Keyboard:
                        return Vector2.zero;
                    case ControlSchemes.Touch:
                        return rightTouchStickControl.ReadValue();
                    case ControlSchemes.Gamepad:
                        return Instance.rightGamepadStick.action.ReadValue<Vector2>();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static bool UsingController => ControlScheme == ControlSchemes.Gamepad;
        public static bool UsingTouch      => ControlScheme == ControlSchemes.Touch;
        public static bool UsingKeyboard   => ControlScheme == ControlSchemes.Keyboard;
        
        public static ControlSchemes ControlScheme { get; set; }
        public static GamepadTypes GamepadType     { get; set; }
        public static PlayerInput PlayerInput      { get; private set; }
        public static GameControls Instance        { get; private set; }
        
        public override string Name    => "Controls";
        public override uint LoadOrder => SubsystemLoadOrders.Controls;

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(GameControls)} singleton.");
                Destroy(this);
            }
            Instance    = this;
            PlayerInput = GetComponent<PlayerInput>();
            
#if UNITY_IOS || UNITY_ANDROID
            PlayerInput.neverAutoSwitchControlSchemes = true;
#endif
            
            PlayerInput.onControlsChanged += OnControlsChanged;
            SetDefaultControlScheme();
            return UniTask.CompletedTask;
        }

        private void SetDefaultControlScheme()
        {
#if UNITY_IOS || UNITY_ANDROID
            // wtf?
            PlayerInput.SwitchCurrentControlScheme("Touch", /* ??? */ Gamepad.all[0], Touchscreen.current != null ? Touchscreen.current : null);
#else
            if(Keyboard.current != null && Mouse.current != null) {
                PlayerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
                OnControlsChanged(PlayerInput);
                return;
            } 
            if(Gamepad.current != null) {
                PlayerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
                OnControlsChanged(PlayerInput);
                return;
            }
            if(Touchscreen.current != null) {
                PlayerInput.SwitchCurrentControlScheme("Touch", Touchscreen.current);
                OnControlsChanged(PlayerInput);
            }
#endif
        }

        private void OnControlsChanged(PlayerInput input)
        {
            switch(input.currentControlScheme) {
                case "Keyboard&Mouse":
                    SetControlScheme(ControlSchemes.Keyboard, false);
                    break;
                case "Gamepad":
                    SetControlScheme(ControlSchemes.Gamepad, false);
                    break;
                case "Touch":
                    SetControlScheme(ControlSchemes.Touch, false);
                    break;
            }
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private void DetermineGamepadType()
        {
            Gamepad gamepad = Gamepad.current;
            if(gamepad == null || string.IsNullOrEmpty(gamepad.description.product)) {
                GamepadType = GamepadTypes.None;
                return;
            }

            string product = gamepad.description.product.ToLower();
            if(product.Contains("xbox") || product.Contains("xinput")) {
                GamepadType = GamepadTypes.XBox;
                return;
            }
            if(product.Contains("playstation") || product.Contains("ps")) {
                GamepadType = GamepadTypes.PlayStation;
                return;
            }
            GamepadType = GamepadTypes.Generic;
        }
        
        public void SetControlScheme(ControlSchemes scheme, bool switchPlayerInput = true)
        {
            ControlScheme = scheme;
            leftTouchStick.SetActive(scheme == ControlSchemes.Touch);
            rightTouchStick.SetActive(scheme == ControlSchemes.Touch);
            if(UsingTouch) {
                leftTouchStickControl  = (StickControl)leftTouchStick.GetComponent<OnScreenStick>().control;
                rightTouchStickControl = (StickControl)rightTouchStick.GetComponent<OnScreenStick>().control;
            }
            if(!switchPlayerInput) {
                DetermineGamepadType();
                GameControlsCallbacks.ControlSchemeChanged(scheme);
                return;
            }

            switch(scheme) {
                case ControlSchemes.Keyboard:
                    PlayerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
                    break;
                case ControlSchemes.Touch:
                    PlayerInput.SwitchCurrentControlScheme("Touch", Touchscreen.current);
                    break;
                case ControlSchemes.Gamepad:
                    PlayerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
                    DetermineGamepadType();
                    break;
                
                case ControlSchemes.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null);
            }
            GameControlsCallbacks.ControlSchemeChanged(scheme);
        }
    }
    
    public enum ControlSchemes
    {
        None       = 0,
        Keyboard   = 1,
        Touch      = 2,
        Gamepad    = 3
    }

    public enum GamepadTypes
    {
        None        = 0,
        Generic     = 1,
        PlayStation = 2,
        XBox        = 3
    }
}
