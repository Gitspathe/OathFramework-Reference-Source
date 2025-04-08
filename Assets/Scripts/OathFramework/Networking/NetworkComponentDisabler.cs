using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Networking
{
    public class NetworkComponentDisabler : MonoBehaviour
    {
        [SerializeField] private NetworkBehaviour[] components;
        [SerializeField] private bool disableSinglePlayer = true;

        private void Awake()
        {
            if(disableSinglePlayer && NetGame.GameType == GameType.SinglePlayer) {
                foreach(NetworkBehaviour comp in components) {
                    comp.enabled = false;
                }
            }
        }
    }

}
