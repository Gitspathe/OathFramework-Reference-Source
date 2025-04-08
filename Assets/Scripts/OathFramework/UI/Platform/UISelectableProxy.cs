using OathFramework.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI.Platform
{
    [RequireComponent(typeof(Selectable))]
    public class UISelectableProxy : LoopComponent, 
        ILoopUpdate, IControlSchemeChangedCallback, ISelectHandler, 
        IDeselectHandler, ISubmitHandler, ICancelHandler
    {
        [SerializeField] private bool controllerOnly = true;
        [SerializeField] private bool exitOnDisable  = true;
        [SerializeField] private bool exitOnBack     = true;
        
        [Space(5)]
        
        [SerializeField] private List<UINavigationGroup> onEnterEnableNavGroups;
        [SerializeField] private List<UINavigationGroup> onEnterDisableNavGroups;
        
        [Space(10)]
        
        [SerializeField] private List<UINavigationGroup> onExitEnableNavGroups;
        [SerializeField] private List<UINavigationGroup> onExitDisableNavGroups;

        [Space(10)]
        
        [SerializeField] private Selectable onEnterSelect;
        [SerializeField] private Selectable onExitSelect;
        
        public bool IsEntered { get; private set; }

        public Selectable Selectable { get; private set; }

        private static UISelectableProxy current;
        private static List<UISelectableProxy> proxies = new();

        private static void PopProxy(UISelectableProxy proxy)
        {
            for(int i = 0; i < proxies.Count; i++) {
                if(proxies[i] == proxy) {
                    proxies.RemoveAt(i);
                }
            }
            current = proxies.Count > 0 ? proxies[0] : null;
        }

        private static void PushProxy(UISelectableProxy proxy)
        {
            if(proxies.Contains(proxy))
                return;
            
            proxies.Insert(0 ,proxy);
            current = proxies.Count > 0 ? proxies[0] : null;
        }
        
        private void Awake()
        {
            Selectable = GetComponent<Selectable>();
            GameControlsCallbacks.Register((IControlSchemeChangedCallback)this);
            ProcessEnabledState();
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            if(current == this && UIControlsInputHandler.BackAction.WasPressedThisFrame() && !OnScreenKeyboard.IsOpen) {
                ((ICancelHandler)this).OnCancel(null);
            }
        }

        private void OnDestroy()
        {
            GameControlsCallbacks.Unregister((IControlSchemeChangedCallback)this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if(IsEntered && exitOnDisable) {
                ((ICancelHandler)this).OnCancel(null);
            }
        }

        private void ProcessEnabledState()
        {
            if(controllerOnly) {
                enabled = GameControls.ControlScheme == ControlSchemes.Gamepad;
                return;
            }
            enabled = true;
        }

        void IControlSchemeChangedCallback.OnControlSchemeChanged(ControlSchemes controlScheme)
        {
            ProcessEnabledState();
        }

        void ISelectHandler.OnSelect(BaseEventData eventData)
        {
            // ?
        }

        void IDeselectHandler.OnDeselect(BaseEventData eventData)
        {
            // ?
        }

        void ISubmitHandler.OnSubmit(BaseEventData eventData)
        {
            IsEntered = true;
            foreach(UINavigationGroup navGroup in onEnterEnableNavGroups) {
                navGroup.SetNavigation(true);
            }
            foreach(UINavigationGroup navGroup in onEnterDisableNavGroups) {
                navGroup.SetNavigation(false);
            }
            if(onEnterSelect != null) {
                EventSystem.current.SetSelectedGameObject(onEnterSelect.gameObject);
            }
            PushProxy(this);
        }

        void ICancelHandler.OnCancel(BaseEventData eventData)
        {
            IsEntered = false;
            foreach(UINavigationGroup navGroup in onExitEnableNavGroups) {
                navGroup.SetNavigation(true);
            }
            foreach(UINavigationGroup navGroup in onExitDisableNavGroups) {
                navGroup.SetNavigation(false);
            }
            if(onExitSelect != null) {
                EventSystem.current.SetSelectedGameObject(onExitSelect.gameObject);
            }
            PopProxy(this);
        }
    }
}
