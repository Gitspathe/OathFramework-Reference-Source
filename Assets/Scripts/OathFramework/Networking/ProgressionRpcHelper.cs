using OathFramework.Progression;
using OathFramework.UI;
using Unity.Netcode;

namespace OathFramework.Networking
{
    public class ProgressionRpcHelper : NetworkBehaviour
    {
        public static ProgressionRpcHelper Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            Instance = null;
        }

        public static void AddExp(NetClient player, uint amount)
        {
            if(player.IsOwner) {
                HUDScript.ExpPopup.RecordExp(amount);
                ProgressionManager.Profile.AddExp(amount);
                return;
            }
            Instance.AddExpClientRpc(amount, Instance.RpcTarget.Single(player.OwnerClientId, RpcTargetUse.Temp));
        }
        
        [Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
        public void AddExpClientRpc(uint amount, RpcParams rpcParams = default)
        {
            HUDScript.ExpPopup.RecordExp(amount);
            ProgressionManager.Profile.AddExp(amount);
        }
    }
}
