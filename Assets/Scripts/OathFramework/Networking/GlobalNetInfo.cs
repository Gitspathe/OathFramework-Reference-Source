using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Networking
{
    public class GlobalNetInfo : NetworkBehaviour
    {
        private NetworkVariable<bool> netUsingSnapshot = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server, 
            value: false
        );

        public static bool UsingSnapshot     { get; set; }
        public static bool IsLoaded          { get; private set; }
        public static GlobalNetInfo Instance { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Duplicate {nameof(GlobalNetInfo)} active.");
                Destroy(this);
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if(NetworkManager.Singleton.IsServer) {
                netUsingSnapshot.Value = UsingSnapshot;
            } else {
                UsingSnapshot = netUsingSnapshot.Value;
            }
            IsLoaded = true;
        }

        public static async UniTask AwaitInitialization()
        {
            while(!IsLoaded) {
                await UniTask.Yield();
            }
        }
    }
}
