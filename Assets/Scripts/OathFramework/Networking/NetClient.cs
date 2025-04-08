using Cysharp.Threading.Tasks;
using OathFramework.Core;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using OathFramework.Core.Service;
using OathFramework.EntitySystem.Players;

namespace OathFramework.Networking
{ 

    public class NetClient : NetworkBehaviour
    {
        private NetworkVariable<FixedString64Bytes> clientUIDNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );
        
        private NetworkVariable<FixedString64Bytes> clientNameNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        private NetworkVariable<byte> playerIndexNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        private NetworkVariable<PlayerLifeState> lifeStateNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server,
            value: PlayerLifeState.NotSpawned
        );

        private NetworkVariable<bool> showSupporterBadgeNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server,
            value: false
        );
        
        private NetworkVariable<ulong> supporterSecretNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server,
            value: 0
        );

        public byte Index                        { get; private set; }
        public string UniqueID                   { get; private set; }
        public string Name                       { get; private set; }
        public bool ShowSupporterBadge           { get; private set; }
        public ulong SupporterSecret             { get; private set; }
        public PlayerBuildDataHandler Data       { get; private set; }
        public PlayerController PlayerController { get; set; }
        public float ConnectionTimeOut           { get; set; }

        public bool Alive                  => PlayerController != null && PlayerController.Entity != null && !PlayerController.Entity.IsDead;
        public static bool SelfAlive       => SelfExists && Self.Alive;
        public static bool SelfInitialSync => SelfExists && !Self.Data.PendingInitialSync;
        public static bool SelfExists      => PlayerManager.Instance != null 
                                              && PlayerManager.TryGetPlayerFromNetID(NetworkManager.Singleton.LocalClientId, out NetClient _);

        public static NetClient Self {
            get {
                if(PlayerManager.TryGetPlayerFromNetID(NetworkManager.Singleton.LocalClientId, out NetClient player))
                    return player;

                Debug.LogError($"Could not locate NetPlayer for self, client ID '{NetworkManager.Singleton.LocalClientId}'");
                return null;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
            Data = GetComponent<PlayerBuildDataHandler>();
        }

        public override void OnNetworkSpawn()
        {
            _ = LoadTask();
        }

        private async UniTask LoadTask()
        {
            await GlobalNetInfo.AwaitInitialization();
            if(IsServer) {
                clientUIDNetVar.Value          = UniqueID;
                clientNameNetVar.Value         = Name;
                playerIndexNetVar.Value        = Index;
                showSupporterBadgeNetVar.Value = ShowSupporterBadge && SupporterDLCUtil.VerifySecret(SupporterSecret);
                supporterSecretNetVar.Value    = SupporterSecret;
            } else if(IsClient) {
                UniqueID           = clientUIDNetVar.Value.ToString();
                Name               = clientNameNetVar.Value.ToString();
                Index              = playerIndexNetVar.Value;
                ShowSupporterBadge = showSupporterBadgeNetVar.Value && SupporterDLCUtil.VerifySecret(supporterSecretNetVar.Value);
                SupporterSecret    = supporterSecretNetVar.Value;
            }
            if(IsOwner) {
                SupporterSecret    = SupporterDLCUtil.Secret;
                ShowSupporterBadge = SupporterDLCUtil.HasSupporterDLC;
                NetGameRpcHelper.Instance.NotifyConnectionFinishedServerRpc();
            }
            ConnectionService.OnPlayerClientSpawned(this);
        }

        public override void OnNetworkDespawn()
        {
            ConnectionService.OnPlayerClientDespawned(this);
        }

        public void Initialize(ConnectionData data, byte playerIndex, string uid)
        {
            Name               = data.Name;
            ShowSupporterBadge = data.ShowSupporterBadge;
            SupporterSecret    = data.SupporterSecret;
            Index              = playerIndex;
            UniqueID           = uid;
        }
    }

    public enum PlayerLifeState
    {
        NotSpawned,
        Alive,
        Dead
    }

}
