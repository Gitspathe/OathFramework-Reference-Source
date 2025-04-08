using OathFramework.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI.Platform
{
    [RequireComponent(typeof(TMP_InputField))]
    public class UIInputField : LoopComponent, 
        ILoopUpdate, ISelectHandler, IDeselectHandler,
        IOnScreenKeyboardCallback, IControlSchemeChangedCallback
    {
        [SerializeField] private bool showKeyboard                          = true;
        [SerializeField] private OnScreenKeyboard.KeyboardType keyboardType = OnScreenKeyboard.KeyboardType.Main;
        [SerializeField] private bool showNumericSidePanel;
        [SerializeField] private bool clearOnHide;
        
        [Space(10)]
        
        [SerializeField] private UISelectableProxy sourceProxy;

        private TMP_InputField inputField;
        private UISelectableProxy selectableProxy;
        private bool isSelected;
        private bool isFocused;
        
        private void Awake()
        {
            inputField      = GetComponent<TMP_InputField>();
            selectableProxy = GetComponent<UISelectableProxy>();
            GameControlsCallbacks.Register((IControlSchemeChangedCallback)this);
            GameControlsCallbacks.Register((IOnScreenKeyboardCallback)this);
        }

        private void OnDestroy()
        {
            GameControlsCallbacks.Unregister((IControlSchemeChangedCallback)this);
            GameControlsCallbacks.Unregister((IOnScreenKeyboardCallback)this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if(clearOnHide) {
                inputField.text = "";
            }
            if(!isFocused)
                return;

            isFocused = false;
            GetComponent<UINavPin>()?.Unpin();
            if(showKeyboard && OnScreenKeyboard.IsOpen) {
                OnScreenKeyboard.Instance.SetActive(false);
            }
        }

        void ILoopUpdate.LoopUpdate()
        {
            if(!isSelected)
                return;

            if(isFocused && !inputField.isFocused) {
                isFocused = false;
                GetComponent<UINavPin>()?.Unpin();
                if(showKeyboard && OnScreenKeyboard.IsOpen) {
                    OnScreenKeyboard.Instance.SetActive(false);
                }
            } else if(!isFocused && inputField.isFocused) {
                isFocused = true;
                GetComponent<UINavPin>()?.Pin();
                if(showKeyboard && GameControls.UsingController) {
                    OnScreenKeyboard.Instance.SetActiveFocus(inputField, sourceProxy?.Selectable, keyboardType, showNumericSidePanel, true);
                }
            }
            if(isFocused && UIControlsInputHandler.BackAction.WasPressedThisFrame()) {
                DeactivateInputField();
            }
        }

        private void DeactivateInputField()
        {
            isFocused = false;
            GetComponent<UINavPin>()?.Unpin();
            if(showKeyboard) {
                OnScreenKeyboard.Instance.SetActive(false);
            }
            inputField.DeactivateInputField();
            if(selectableProxy != null) {
                ((ICancelHandler)selectableProxy).OnCancel(null);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            GetComponent<UINavPin>()?.Pin();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            GetComponent<UINavPin>()?.Unpin();
        }

        void IOnScreenKeyboardCallback.OnOSKOpened(Selectable target) {}

        void IOnScreenKeyboardCallback.OnOSKSubmit(Selectable target)
        {
            if(target == inputField) {
                DeactivateInputField();
            }
        }

        void IOnScreenKeyboardCallback.OnOSKClosed() {}

        void IControlSchemeChangedCallback.OnControlSchemeChanged(ControlSchemes controlScheme)
        {
            if(!isFocused)
                return;
            
            if(controlScheme != ControlSchemes.Gamepad && OnScreenKeyboard.IsOpen) {
                OnScreenKeyboard.Instance.SetActive(false);
            } else if(controlScheme == ControlSchemes.Gamepad && !OnScreenKeyboard.IsOpen) {
                OnScreenKeyboard.Instance.SetActiveFocus(inputField, sourceProxy?.Selectable, keyboardType, showNumericSidePanel, true);
            }
        }
    }
}
