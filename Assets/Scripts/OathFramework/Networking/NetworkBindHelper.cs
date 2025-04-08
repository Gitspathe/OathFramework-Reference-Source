using System.Collections.Generic;
using Unity.Netcode;

namespace OathFramework.Networking
{
    public class NetworkBindHelper
    {
        public NetworkBehaviour Owner { get; }
        
        private HashSet<INetworkBindHelperNode> bound           = new();
        private HashSet<INetworkBindHelperNode> spawnsRemaining = new();

        public NetworkBindHelper(NetworkBehaviour owner)
        {
            Owner = owner;
            foreach(INetworkBindHelperNode bind in Owner.GetComponentsInChildren<INetworkBindHelperNode>()) {
                bound.Add(bind);
                spawnsRemaining.Add(bind);
                bind.Binder = this;
            }
        }

        public void TriggerBoundCallback()
        {
            foreach(INetworkBindHelperNode bind in bound) {
                bind.OnBound();
            }
        }

        public void OnNetworkSpawnCallback(INetworkBindHelperNode bind)
        {
            spawnsRemaining.Remove(bind);
        }

        public bool IsFinished => spawnsRemaining.Count == 0;
    }
    
    public interface INetworkBindHelperNode
    {
        NetworkBindHelper Binder { get; set; }
        void OnBound();
    }
}
