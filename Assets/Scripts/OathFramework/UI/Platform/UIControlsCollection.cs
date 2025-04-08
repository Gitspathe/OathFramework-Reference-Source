using OathFramework.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.UI.Platform
{

    [CreateAssetMenu(fileName = "UI Controls Collection", menuName = "ScriptableObjects/Platform/UIControlsCollection", order = 1)]
    public class UIControlsCollection : UIControlsCollectionBase
    {
        [field: SerializeField] public ControlSchemes ControlScheme { get; private set; }

        public override ControlSchemes GetControlSchemeID() => ControlScheme;
    }
    
    public abstract class UIControlsCollectionBase : ScriptableObject
    {
        public abstract ControlSchemes GetControlSchemeID();

        [SerializeField] private UIControlNode[] nodes;
        [NonSerialized] private Dictionary<string, UIControlNode> nodesDict = new();
        [NonSerialized] private bool isInit;
        
        [field: SerializeField] public string MaskGroup { get; private set; }
        
        public void Initialize()
        {
            if(isInit)
                return;

            foreach(UIControlNode node in nodes) {
                nodesDict.TryAdd(node.Path, node);
            }
            isInit = true;
        }
        
        public Sprite GetSprite(string path) 
            => string.IsNullOrEmpty(path) || !nodesDict.TryGetValue(path, out UIControlNode node) ? null : node.Sprite;
    }
    
    [Serializable]
    public class UIControlNode
    {
        [field: SerializeField] public string Path   { get; private set; }
        [field: SerializeField] public Sprite Sprite { get; private set; }
    }

}
