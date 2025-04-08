using OathFramework.EntitySystem.Players;
using OathFramework.Core;
using OathFramework.Networking;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.UI.Info
{
    public class AllyStatusHolder : LoopComponent, ILoopUpdate, IPlayerConnectedCallback, IPlayerDisconnectedCallback
    {
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private Transform parent;

        private Dictionary<NetClient, AllyStatusInfo> nodes = new();

        public AllyStatusHolder Initialize()
        {
            GameCallbacks.Register((IPlayerConnectedCallback)this);
            GameCallbacks.Register((IPlayerDisconnectedCallback)this);
            return this;
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            foreach(KeyValuePair<NetClient,AllyStatusInfo> node in nodes) {
                node.Value.gameObject.SetActive(HUDScript.AttachedPlayer != node.Key.PlayerController);
            }
        }

        private void OnDestroy()
        {
            GameCallbacks.Unregister((IPlayerConnectedCallback)this);
            GameCallbacks.Unregister((IPlayerDisconnectedCallback)this);
        }

        private static bool IsSelf(NetClient client) 
            => client.OwnerClientId == NetworkManager.Singleton.LocalClientId;

        void IPlayerConnectedCallback.OnPlayerConnected(NetClient client)
        {
            if(IsSelf(client) || nodes.ContainsKey(client))
                return;

            GameObject go       = Instantiate(nodePrefab, parent);
            AllyStatusInfo info = go.GetComponent<AllyStatusInfo>().Setup(client);
            nodes.Add(client, info);
        }

        void IPlayerDisconnectedCallback.OnPlayerDisconnected(NetClient client)
        {
            if(IsSelf(client) || !nodes.TryGetValue(client, out AllyStatusInfo info))
                return;
            
            Destroy(info.gameObject);
            nodes.Remove(client);
        }
    }
}
