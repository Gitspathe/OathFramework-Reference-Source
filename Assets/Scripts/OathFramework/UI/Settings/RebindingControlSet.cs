using System;
using OathFramework.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

namespace OathFramework.UI.Settings
{
    [CreateAssetMenu(fileName = "Rebinding Control Set", menuName = "ScriptableObjects/Platform/RebindingControlSet", order = 1)]
    public class RebindingControlSet : ScriptableObject
    {
        [field: SerializeField] public ControlSchemes ControlScheme { get; private set; }
        [field: SerializeField] public string BindingGroup          { get; private set; }
        [field: SerializeField] public Node[] Nodes                 { get; private set; }

        [Serializable]
        public class Node
        {
            [field: SerializeField] public NodeType Type               { get; private set; } = NodeType.Input;
            [field: SerializeField] public LocalizedString DisplayName { get; private set; }
            [field: SerializeField] public InputActionReference Action { get; private set; }
            
            [field: ShowIf("@this.Type == NodeType.CompositeInput4")]
            [field: SerializeField] public string CompositeName        { get; private set; }
            
            [field: ShowIf("@this.Type == NodeType.CompositeInput4")]
            [field: SerializeField] public string Composite1           { get; private set; }
            
            [field: ShowIf("@this.Type == NodeType.CompositeInput4")]
            [field: SerializeField] public string Composite2           { get; private set; }
            
            [field: ShowIf("@this.Type == NodeType.CompositeInput4")]
            [field: SerializeField] public string Composite3           { get; private set; }
            
            [field: ShowIf("@this.Type == NodeType.CompositeInput4")]
            [field: SerializeField] public string Composite4           { get; private set; }

            public string GetComposite(int index)
            {
                switch(index) {
                    case 0: return Composite1;
                    case 1: return Composite2;
                    case 2: return Composite3;
                    case 3: return Composite4;
                }
                return null;
            }
        }

        public enum NodeType
        {
            Input,
            CompositeInput4
        }
    }
}
