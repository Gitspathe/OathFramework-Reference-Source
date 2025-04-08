using OathFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace OathFramework.UI.Platform
{
    public class UIControlsInfoPanel : MonoBehaviour, IControlSchemeChangedCallback
    {
        [SerializeField] private Transform infoParent;
        [SerializeField] private List<InfoNode> nodes;
        
        private List<GameObject> spawnedGOs = new();

        private static GameObject SpritePrefab => UIControlsDatabase.InfoNodeSpritePrefab;
        private static GameObject TextPrefab   => UIControlsDatabase.InfoNodeTextPrefab;
        private static GameObject DivPrefab    => UIControlsDatabase.InfoNodeDivPrefab;

        private void OnEnable()
        {
            GameControlsCallbacks.Register((IControlSchemeChangedCallback)this);
            LocalizationSettings.SelectedLocaleChanged += OnLocalizationChanged;
            Tick();
        }

        private void OnDisable()
        {
            GameControlsCallbacks.Unregister((IControlSchemeChangedCallback)this);
            LocalizationSettings.SelectedLocaleChanged -= OnLocalizationChanged;
        }
        
        private void OnLocalizationChanged(Locale newLocale)
        {
            Tick();
        }

        void IControlSchemeChangedCallback.OnControlSchemeChanged(ControlSchemes controlScheme)
        {
            Tick();
        }

        public void SetNodes(IEnumerable<InfoNode> nodes)
        {
            this.nodes.Clear();
            if(nodes == null) {
                Tick();
                return;
            }
            
            this.nodes.AddRange(nodes);
            Tick();
        }

        private void Tick()
        {
            foreach(GameObject go in spawnedGOs) {
                Destroy(go);
            }
            spawnedGOs.Clear();
            if(!UIControlsDatabase.TryGetCurrentControls(out UIControlsCollectionBase controls))
                return;
            
            foreach(InfoNode infoNode in nodes) {
                try {
                    InputAction action = infoNode.Action.action;
                    int index          = action.GetBindingIndex(InputBinding.MaskByGroup(controls.MaskGroup));
                    action.GetBindingDisplayString(index, out _, out string controlPath);
                    
                    Sprite sprite = controls.GetSprite(controlPath);
                    string text   = infoNode.Text.GetLocalizedString();
                    if(sprite == null)
                        continue;
                
                    spawnedGOs.Add(Instantiate(SpritePrefab, infoParent).GetComponent<ControlInfoNodeSprite>().Setup(sprite));
                    spawnedGOs.Add(Instantiate(TextPrefab, infoParent).GetComponent<ControlInfoNodeText>().Setup(text));
                    spawnedGOs.Add(Instantiate(DivPrefab, infoParent));
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
        }
        
        [Serializable]
        public class InfoNode
        {
            [field: SerializeField] public InputActionReference Action { get; private set; }
            [field: SerializeField] public LocalizedString Text        { get; private set; }

            public InfoNode(string actionRefID, LocalizedString text)
            {
                InputActionReference actionRef = UIControlsInputHandler.GetInputActionReference(actionRefID);
                if(actionRef == null)
                    return;

                Action = actionRef;
                Text   = text;
            }
        }
    }
}
