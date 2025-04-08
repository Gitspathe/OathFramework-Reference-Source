using OathFramework.Core;
using OathFramework.Data.StatParams;
using OathFramework.EntitySystem.Actions;
using OathFramework.Utility;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public class DodgeHandler : NetLoopComponent, IEntityInitCallback
    {
        public Entity Entity  { get; private set; }
        public Stats CurStats { get; private set; }
        
        public EntityActionHandler ActionHandler => Entity.Actions;
        
        [field: SerializeField] public Dodge DodgeAction { get; private set; }
        
        private AccessToken accessToken;
        
        uint ILockableOrderedListElement.Order => 10_000;

        private void Awake()
        {
            Entity   = GetComponent<Entity>();
            CurStats = Entity.CurStats;
        }

        public void SetAccessToken(AccessToken token)
        {
            accessToken = token;
        }

        public void DoDodge(Vector3 direction, bool isRpc = false)
        {
            if(!isRpc) {
                if(IsServer) {
                    DoDodgeClientRpc(direction);
                } else if(IsOwner) {
                    DoDodgeServerRpc(direction);
                }
            }
            ExecuteDodge(direction);
        }

        private void ExecuteDodge(Vector3 direction)
        {
            DodgeAction.SetParams(direction);
            ActionHandler.InvokeAction(DodgeAction);
            if(IsOwner) {
                Entity.DecrementStamina((ushort)Entity.CurStats.GetParam(DodgeStaminaUse.Instance));
            }
        }

        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            
        }
        
        [Rpc(SendTo.NotServer)]
        private void DoDodgeClientRpc(Vector3 direction)
        {
            if(IsOwner)
                return;

            DoDodge(direction, true);
        }
        
        [Rpc(SendTo.Server)]
        private void DoDodgeServerRpc(Vector3 direction)
        {
            DoDodge(direction, true);
            DoDodgeClientRpc(direction);
        }
    }
}
