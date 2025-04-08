using OathFramework.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI.Platform
{
    public class UINavigationGroup : MonoBehaviour, IOnScreenKeyboardCallback
    {
        [SerializeField] private bool startNav                   = true;
        [SerializeField] private bool disableWithOSK             = true;
        [SerializeField] private bool oskRestorePrevEnabledState = true;
        [SerializeField] private bool handleInteractable;
        [SerializeField] private Selectable[] selectables;

        private bool prevState;
        private bool initialized;
        private bool allowNav;
        private Dictionary<Selectable, Node> nodes = new();

        private void Initialize()
        {
            if(initialized)
                return;

            initialized = true;
            allowNav    = startNav;
            foreach(Selectable selectable in selectables) {
                Register(selectable);
            }
            SetNavigation(allowNav);
        }
        
        private void Awake()
        {
            Initialize();
            GameControlsCallbacks.Register((IOnScreenKeyboardCallback)this);
        }

        private void OnDestroy()
        {
            GameControlsCallbacks.Unregister((IOnScreenKeyboardCallback)this);
        }

        public void SetNavigation(bool val)
        {
            Initialize();
            allowNav = val;
            foreach(Node node in nodes.Values) {
                node.SetNavigation(val, handleInteractable);
            }
        }

        public void ResetState(Selectable selectable)
        {
            Initialize();
            if(!nodes.Remove(selectable, out Node _))
                return;

            Register(selectable);
        }

        public void Register(Selectable selectable)
        {
            Initialize();
            Node node = new(selectable);
            if(nodes.TryAdd(selectable, node)) {
                node.SetNavigation(allowNav, handleInteractable);
            }
        }

        public void Unregister(Selectable selectable)
        {
            Initialize();
            if(!nodes.TryGetValue(selectable, out Node node))
                return;

            node.SetNavigation(true, handleInteractable);
            nodes.Remove(selectable);
        }

        private readonly struct Node
        {
            private Selectable Selectable       { get; }
            private Navigation.Mode DefaultMode { get; }

            public Node(Selectable selectable)
            {
                Selectable  = selectable;
                DefaultMode = selectable.navigation.mode;
            }

            public void SetNavigation(bool val, bool interaction)
            {
                Navigation copy       = Selectable.navigation;
                copy.mode             = val ? DefaultMode : Navigation.Mode.None;
                Selectable.navigation = copy;
                if(interaction) {
                    Selectable.interactable = val;
                }
            }
        }

        void IOnScreenKeyboardCallback.OnOSKOpened(Selectable target)
        {
            prevState = allowNav;
            if(disableWithOSK) {
                SetNavigation(false);
            }
        }

        public void OnOSKSubmit(Selectable target) {}

        void IOnScreenKeyboardCallback.OnOSKClosed()
        {
            if(disableWithOSK) {
                // ReSharper disable once SimplifyConditionalTernaryExpression
                SetNavigation(oskRestorePrevEnabledState ? prevState : true);
            }
        }
    }
}
