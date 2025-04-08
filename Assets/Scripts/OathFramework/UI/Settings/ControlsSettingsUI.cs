using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Settings;
using OathFramework.UI.Platform;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using UnityEngine.UI;
using BindingSyntax = UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace OathFramework.UI.Settings
{
    public class ControlsSettingsUI : MonoBehaviour, IControlSchemeChangedCallback
    {
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private GameObject nodeComposite4Prefab;
        [SerializeField] private Transform nodeParent;

        [Space(10)] 
        
        [SerializeField] private TMP_Dropdown controlSetDropdown;
        [SerializeField] private Button restoreDefaultsBtn;
        
        [Space(10)]
        
        [SerializeField] private string[] cancelInputs;
        [SerializeField] private string[] excludeInputs;

        [Space(10)]
        
        [SerializeField] private LocalizedString cancelStr;
        [SerializeField] private LocalizedString rebindTitleStr;
        [SerializeField] private LocalizedString rebindMsgStr;
        [SerializeField] private LocalizedString compositeRebindTitleStr;
        [SerializeField] private LocalizedString compositeRebindMsgStr;
        [SerializeField] private LocalizedString rebindFailedStr;
        [SerializeField] private LocalizedString restoreDefaultsTitleStr;
        [SerializeField] private LocalizedString restoreDefaultsMsgStr;
        [SerializeField] private LocalizedString restoreDefaultsYesStr;
        [SerializeField] private LocalizedString restoreDefaultsNoStr;

        [Space(10)]
        
        [SerializeField] private RebindingControlSet[] controlSets;

        private static Dictionary<ControlSchemes, RebindingControlSet> controlSetDict = new();
        private static Dictionary<RebindingControlSet.Node, RebindNodeBase> curNodes  = new();
        private static List<GameObject> curNodeGOs                                    = new();
        private static List<string> curExcludes                                       = new();
        private static List<string> lastRebinds                                       = new();
        private static ControlSchemes curScheme;
        private static RebindingControlSet.Node curNode;
        private static string backupJson;
        private static int curCompositeIndex;
        private static ModalConfig rebindModal;
        private static ModalConfig errorModal;
        private static ModalConfig revertConfirmModal;

        private static bool RebindingBlocked 
            => IsRebinding || errorModal != null || revertConfirmModal != null;
        
        public static bool IsRebinding { get; private set; }
        public static bool IsOpen => SettingsUI.IsOpen && SettingsUI.CurrentSubPanel == SettingsUI.SettingsMenuSubPanel.Controls;
        public static ControlsSettingsUI Instance { get; private set; }

        private bool init;

        public ControlsSettingsUI Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(ControlsSettingsUI)} singleton.");
                Destroy(Instance);
                return null;
            }

            foreach(RebindingControlSet controlSet in controlSets) {
                controlSetDict.TryAdd(controlSet.ControlScheme, controlSet);
            }
            GameControlsCallbacks.Register((IControlSchemeChangedCallback)this);
            Instance = this;
            init     = true;
            return this;
        }

        public void StartRebinding(RebindingControlSet.Node node)
        {
            if(RebindingBlocked || !controlSetDict.TryGetValue(curScheme, out RebindingControlSet controlSet))
                return;

            curExcludes.Clear();
            lastRebinds.Clear();
            curNode                         = node;
            curCompositeIndex               = 0;
            IsRebinding                     = true;
            controlSetDropdown.interactable = false;
            switch(node.Type) {
                case RebindingControlSet.NodeType.Input:
                    StartRebindingInput(controlSet);
                    break;
                case RebindingControlSet.NodeType.CompositeInput4:
                    backupJson = curNode.Action.action.SaveBindingOverridesAsJson();
                    StartRebindingComposite4(controlSet, 0);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StartRebindingInput(RebindingControlSet controlSet)
        {
            rebindModal = ModalConfig.Retrieve()
                .WithPriority(ModalPriority.Critical)
                .WithTitle(rebindTitleStr)
                .WithText(rebindMsgStr)
                .WithControlsInfo(new[] { new UIControlsInfoPanel.InfoNode("<ui>/cancel", cancelStr) })
                .Show();
            RebindingOperation operation = new RebindingOperation()
                .WithAction(curNode.Action)
                .WithBindingGroup(controlSet.BindingGroup);

            foreach(string cancelInput in cancelInputs) {
                operation = operation.WithCancelingThrough(cancelInput);
            }
            foreach(string excludeInput in excludeInputs) {
                operation = operation.WithControlsExcluding(excludeInput);
            }
            foreach(string excludeInput in curExcludes) {
                operation = operation.WithControlsExcluding(excludeInput);
            }
            operation.OnPotentialMatch(OnRebindPotentialMatch);
            operation.OnComplete(OnRebindComplete);
            operation.OnCancel(OnRebindCancel);
            operation.Start();
        }

        private void StartRebindingComposite4(RebindingControlSet controlSet, int index)
        {
            string composite         = curNode.GetComposite(index);
            BindingSyntax compSyntax = curNode.Action.action.ChangeBinding(curNode.CompositeName);
            BindingSyntax next       = compSyntax.NextPartBinding(composite);

            compositeRebindMsgStr.Arguments = new List<object>() { new Dictionary<string, string>() { { "composite", composite } } };
            rebindModal = ModalConfig.Retrieve()
                .WithPriority(ModalPriority.Critical)
                .WithTitle(compositeRebindTitleStr)
                .WithText(compositeRebindMsgStr)
                .WithControlsInfo(new[] { new UIControlsInfoPanel.InfoNode("<ui>/cancel", cancelStr) })
                .Show();
            RebindingOperation operation = new RebindingOperation()
                .WithAction(curNode.Action)
                .WithTargetBinding(next.bindingIndex)
                .WithBindingGroup(controlSet.BindingGroup)
                .WithExpectedControlType<ButtonControl>();
            
            foreach(string cancelInput in cancelInputs) {
                operation = operation.WithCancelingThrough(cancelInput);
            }
            foreach(string excludeInput in excludeInputs) {
                operation = operation.WithControlsExcluding(excludeInput);
            }
            foreach(string excludeInput in curExcludes) {
                operation = operation.WithControlsExcluding(excludeInput);
            }
            operation.OnPotentialMatch(OnRebindPotentialMatch);
            operation.OnComplete(OnRebindComplete);
            operation.OnCancel(OnRebindCancel);
            operation.Start();
        }

        private async UniTask DelayedUnsetRebinding()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            controlSetDropdown.interactable = true;
            IsRebinding                     = false;
        }
        
        private void OnRebindCancel(RebindingOperation operation)
        {
            operation.Dispose();
            rebindModal?.Close();
            rebindModal = null;
            if(curNode.Type == RebindingControlSet.NodeType.CompositeInput4) {
                curNode.Action.action.LoadBindingOverridesFromJson(backupJson);
            }
            curNode = null;
            _ = DelayedUnsetRebinding();
            Tick();
        }

        private void OnRebindComplete(RebindingOperation operation)
        {
            lastRebinds.Add(operation.selectedControl.path);
            rebindModal?.Close();
            rebindModal = null;
            if(curNode.Type == RebindingControlSet.NodeType.CompositeInput4 && curCompositeIndex < 3) {
                if(!controlSetDict.TryGetValue(curScheme, out RebindingControlSet controlSet)) {
                    Finish();
                    return;
                }
                curExcludes.Add(operation.selectedControl.path);
                operation.Dispose();
                StartRebindingComposite4(controlSet, ++curCompositeIndex);
                return;
            }
            Finish();
            return;

            void Finish()
            {
                operation.Dispose();
                UnbindCollisions();
                curExcludes.Clear();
                lastRebinds.Clear();
                curNode = null;
                _ = DelayedUnsetRebinding();
                Tick();
                Save();
            }
        }

        private void OnRebindPotentialMatch(RebindingOperation operation)
        {
            if(!IsRebindingValid(operation)) {
                operation.Cancel();
                errorModal = ModalUIScript.ShowGeneric(
                    text: rebindFailedStr, 
                    onButtonClicked: () => { errorModal = null; }
                );
                return;
            }
            
            operation.Complete();
        }

        private void UnbindCollisions()
        {
            foreach(KeyValuePair<RebindingControlSet.Node, RebindNodeBase> node in curNodes) {
                Process(node.Key.Action.action);
            }
            return;

            void Process(InputAction action)
            {
                if(action == curNode.Action.action)
                    return;
                
                List<int> toRemove = new();
                for(int i = 0; i < action.bindings.Count; i++) {
                    InputBinding binding = action.bindings[i];
                    string bindingPath   = binding.hasOverrides ? binding.overridePath : binding.path;
                    if(bindingPath == string.Empty)
                        continue;
                    
                    if(lastRebinds.Contains(FixPath(bindingPath))) {
                        toRemove.Add(i);
                    }
                }
                foreach(int binding in toRemove) {
                    action.ApplyBindingOverride(binding, string.Empty);
                }
            }
            
            string FixPath(string path)
            {
                if(path[0] == '<') {
                    path = path.Remove(0, 1).Insert(0, "/");
                }
                path = path.Replace(">/", "/");
                return path;
            }
        }
        
        private bool IsRebindingValid(RebindingOperation operation)
        {
            //Debug.Log(operation.selectedControl.name);
            if(!UIControlsDatabase.TryGetControls(curScheme, out UIControlsCollectionBase controls) 
               || !controlSetDict.TryGetValue(curScheme, out RebindingControlSet _))
                return false;
            
            return controls.GetSprite(operation.selectedControl.name) != null;
        }

        public void RestoreDefaults()
        {
            revertConfirmModal = ModalConfig.Retrieve()
                .WithPriority(ModalPriority.Critical)
                .WithTitle(restoreDefaultsTitleStr)
                .WithText(restoreDefaultsMsgStr)
                .WithButtons(new (LocalizedString, Action)[] {
                    (UICommonMessages.Yes, ConfirmRestoreDefaults),
                    (UICommonMessages.No,  CancelRestoreDefaults)
                })
                .WithInitButton(1)
                .Show();
        }
        
        private void CancelRestoreDefaults()
        {
            revertConfirmModal = null;
        }

        private void ConfirmRestoreDefaults()
        {
            foreach(InputActionMap actionMap in SettingsManager.Instance.Controls.actionMaps) {
                actionMap.RemoveAllBindingOverrides();
            }
            Tick();
            revertConfirmModal = null;
            Save();
        }

        private void Save()
        {
            _ = SettingsManager.SaveControls();
        }
        
        public void OnControlSchemeDropdownChanged()
        {
            switch(controlSetDropdown.value) {
                case 0:
                    ChangeCurrentScheme(ControlSchemes.Keyboard);
                    break;
                case 1:
                    ChangeCurrentScheme(ControlSchemes.Gamepad);
                    break;
            }
        }

        public void ChangeCurrentScheme(ControlSchemes controlScheme)
        {
            curNodes.Clear();
            curNode   = null;
            curScheme = controlScheme;
            foreach(GameObject go in curNodeGOs) {
                Destroy(go);
            }
            switch(curScheme) {
                case ControlSchemes.Keyboard:
                    controlSetDropdown.SetValueWithoutNotify(0);
                    break;
                case ControlSchemes.Gamepad:
                    controlSetDropdown.SetValueWithoutNotify(1);
                    break;
                
                case ControlSchemes.Touch:
                case ControlSchemes.None:
                default:
                    controlSetDropdown.SetValueWithoutNotify(0); // TODO: Hide or something?
                    break;
            }
            if(!controlSetDict.TryGetValue(curScheme, out RebindingControlSet controlSet))
                return;

            foreach(RebindingControlSet.Node node in controlSet.Nodes) {
                GameObject go;
                switch(node.Type) {
                    case RebindingControlSet.NodeType.Input:
                        go = Instantiate(nodePrefab, nodeParent);
                        break;
                    case RebindingControlSet.NodeType.CompositeInput4:
                        go = Instantiate(nodeComposite4Prefab, nodeParent);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                RebindNodeBase uiNode = go.GetComponent<RebindNodeBase>();
                curNodeGOs.Add(go);
                curNodes.Add(node, uiNode);
            }
            Tick();
        }

        public void Tick()
        {
            if(!controlSetDict.TryGetValue(curScheme, out RebindingControlSet controlSet) || controlSet.Nodes.Length == 0)
                return;
            
            foreach(RebindingControlSet.Node node in controlSet.Nodes) {
                if(!curNodes.TryGetValue(node, out RebindNodeBase uiNode))
                    continue;
                
                uiNode.Setup(node, curScheme);
            }
            
            int i                = controlSet.Nodes.Length - 1;
            TMP_Dropdown drop    = controlSetDropdown;
            Button btn           = restoreDefaultsBtn;
            Navigation dropNav   = drop.navigation;
            Navigation btnNav    = btn.navigation;
            dropNav.mode         = Navigation.Mode.Explicit;
            btnNav.mode          = Navigation.Mode.Explicit;
            dropNav.selectOnDown = !curNodes.TryGetValue(controlSet.Nodes[0], out RebindNodeBase navNode) ? null : navNode.Button;
            btnNav.selectOnUp    = !curNodes.TryGetValue(controlSet.Nodes[i], out navNode) ? null : navNode.Button;
            drop.navigation      = dropNav;
            btn.navigation       = btnNav;
            if(!curNodes.TryGetValue(controlSet.Nodes[0], out RebindNodeBase firstNode)
               || !curNodes.TryGetValue(controlSet.Nodes[1], out RebindNodeBase nextNode))
                return;

            Navigation firstNav         = firstNode.Button.navigation;
            firstNav.mode               = Navigation.Mode.Explicit;
            firstNav.selectOnUp         = controlSetDropdown;
            firstNav.selectOnDown       = nextNode.Button;
            firstNode.Button.navigation = firstNav;
        }

        void IControlSchemeChangedCallback.OnControlSchemeChanged(ControlSchemes controlScheme)
        {
            if(IsOpen)
                return;

            ChangeCurrentScheme(controlScheme);
        }
    }
}
