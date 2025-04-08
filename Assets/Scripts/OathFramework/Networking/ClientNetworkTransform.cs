using OathFramework.EntitySystem;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace OathFramework.Networking
{
    /// <summary>
    /// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
    /// for transforms that'll always be owned by the server.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// Used to determine who can write to this transform. Owner client only.
        /// This imposes state to the server. This is putting trust on your clients. Make sure no security-sensitive features use this transform.
        /// </summary>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
        
        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        public void ForceTeleportOwnerRpc(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Teleport(position, rotation, scale);
        }
    }
}
