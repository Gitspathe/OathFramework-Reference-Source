using OathFramework.Core;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI.Settings
{
    public abstract class RebindNodeBase : MonoBehaviour
    {
        protected RebindingControlSet.Node Node;
        
        public ExButton Button { get; private set; }
        
        public void OnClicked()
        {
            ControlsSettingsUI.Instance.StartRebinding(Node);
        }

        public virtual GameObject Setup(RebindingControlSet.Node node, ControlSchemes controlScheme)
        {
            Node   = node;
            Button = GetComponent<ExButton>();
            return gameObject;
        }
    }
}
