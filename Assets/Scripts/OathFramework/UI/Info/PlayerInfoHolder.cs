using OathFramework.Networking;
using OathFramework.UI.Platform;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI.Info
{
    public class PlayerInfoHolder : MonoBehaviour
    {
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private Transform nodesParent;
        [SerializeField] private UINavigationGroup nodesNavGroup;

        private Dictionary<NetClient, PlayerInfo> nodes = new();

        public void Setup()
        {
            UIInfoManager.RegisterPlayerInfoHolder(this);
        }

        private void OnDestroy()
        {
            UIInfoManager.UnregisterPlayerInfoHolder(this);
        }

        public Selectable GetFirst()
        {
            return nodesParent.childCount == 0 ? null : nodesParent.transform.GetChild(0).GetComponent<PlayerInfo>().Border;
        }

        public void SetSelectable(Selectable selectable, MoveDirection direction, bool clearExisting = true)
        {
            Selectable nextS = null;
            Selectable prevS = null;
            int i            = 0;
            foreach(Transform t in nodesParent.transform) {
                if(!t.TryGetComponent(out PlayerInfo pi)) {
                    i++;
                    continue;
                }
                
                Transform nextT = nodesParent.transform.childCount > i ? nodesParent.transform.GetChild(i).transform : null;
                Transform prevT = i == 0 ? null : nodesParent.transform.GetChild(i - 1).transform;
                if(nextT != null && nextT.TryGetComponent(out pi)) {
                    nextS = pi.Border;
                } else if(nextT == null && direction == MoveDirection.Down) {
                    nextS = selectable;
                }
                if(prevT != null && prevT.TryGetComponent(out pi)) {
                    prevS = pi.Border;
                } else if(prevT == null && direction == MoveDirection.Up) {
                    prevS = selectable;
                }
                
                Selectable border = pi.Border;
                Navigation nav    = border.navigation;
                if(clearExisting) {
                    nav.mode          = Navigation.Mode.Explicit;
                    nav.selectOnLeft  = null;
                    nav.selectOnUp    = null;
                    nav.selectOnRight = null;
                    nav.selectOnDown  = null;
                }
                switch(direction) {
                    case MoveDirection.Left:
                        nav.selectOnLeft = selectable;
                        break;
                    case MoveDirection.Up:
                        nav.selectOnUp = prevS;
                        break;
                    case MoveDirection.Right:
                        nav.selectOnRight = selectable;
                        break;
                    case MoveDirection.Down:
                        nav.selectOnDown = nextS;
                        break;
                    
                    case MoveDirection.None:
                    default:
                        break;
                }
                
                border.navigation = nav;
                nodesNavGroup.ResetState(border);
                i++;
            }
        }

        public void ClearInfo()
        {
            foreach(KeyValuePair<NetClient, PlayerInfo> node in nodes) {
                Destroy(node.Value);
            }
            nodes.Clear();
        }
        
        public void ClearInfo(NetClient client)
        {
            if(nodes.TryGetValue(client, out PlayerInfo node)) {
                if(node != null) {
                    Destroy(node.gameObject);
                    nodesNavGroup?.Unregister(node.GetComponent<PlayerInfo>().Border);
                }
            }
            nodes.Remove(client);
        }
        
        public void UpdateInfo(NetClient client)
        {
            if(!nodes.TryGetValue(client, out PlayerInfo node)) {
                GameObject go = Instantiate(nodePrefab, nodesParent);
                node          = go.GetComponent<PlayerInfo>();
                nodes.Add(client, node);
                nodesNavGroup?.Register(node.Border);
            }
            node.Setup(this, client);
        }
    }
}
