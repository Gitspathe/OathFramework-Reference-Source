using System;
using OathFramework.Core;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.UI.Platform
{
    public class UIControlSchemeActivator : MonoBehaviour, IControlSchemeChangedCallback
    {
        [SerializeField] private List<GameObject> objects;
        [SerializeField] private List<Node> nodes;
        
        private void Awake()
        {
            GameControlsCallbacks.Register((IControlSchemeChangedCallback)this);
        }

        private void Start()
        {
            Tick(GameControls.ControlScheme);
        }

        private void OnDestroy()
        {
            GameControlsCallbacks.Unregister((IControlSchemeChangedCallback)this);
        }

        private void Tick(ControlSchemes controlScheme)
        {
            foreach(GameObject go in objects) {
                go.SetActive(false);
            }
            foreach(Node node in nodes) {
                if(node.Scheme != controlScheme)
                    continue;
                
                foreach(GameObject activate in node.Activate) {
                    activate.SetActive(true);
                }
                return;
            }
        }
        
        void IControlSchemeChangedCallback.OnControlSchemeChanged(ControlSchemes controlScheme)
        {
            Tick(controlScheme);
        }

        [Serializable]
        public class Node
        {
            [field: SerializeField] public ControlSchemes Scheme     { get; private set; }
            [field: SerializeField] public List<GameObject> Activate { get; private set; }
        }
    }
}
