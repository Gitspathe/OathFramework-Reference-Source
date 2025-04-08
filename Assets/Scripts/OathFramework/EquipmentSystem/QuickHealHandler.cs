using OathFramework.Core;
using OathFramework.Data;
using OathFramework.Data.SpEvents;
using OathFramework.Data.StatParams;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Actions;
using OathFramework.EntitySystem.States;
using OathFramework.Networking;
using OathFramework.Utility;
using Unity.Netcode;
using UnityEngine;
using Action = System.Action;

namespace OathFramework.EquipmentSystem
{
    [RequireComponent(typeof(ItemHandler))]
    public class QuickHealHandler : NetLoopComponent, ILoopUpdate, INetworkBindHelperNode
    {
        [SerializeField] private ActionAnimParams animParams;

        private AccessToken callbackAccessToken;
        private Action onTriggerAction;
        private ItemHandler itemHandler;
        private QList<EntitySpEvent> events = new();
        private NetworkVariable<byte> netHealCharges = new(
            value: 0, 
            readPerm: NetworkVariableReadPermission.Everyone, 
            writePerm: NetworkVariableWritePermission.Owner
        );
        
        public byte CurrentCharges { get; private set; }
        public Stats Stats => itemHandler.Entity.CurStats;

        public QuickHealHandlerCallbacks Callbacks { get; } = new();
        public Entity Entity                       { get; private set; }
        public NetworkBindHelper Binder            { get; set; }
        
        private void Awake()
        {
            callbackAccessToken = Callbacks.Access.GenerateAccessToken();
            Entity              = GetComponent<Entity>();
            itemHandler         = GetComponent<ItemHandler>();
            onTriggerAction     = OnTriggerAction;
            events.Add(new EntitySpEvent(QuickHeal.Instance));
            SetVisual();
        }
        
        private void SetVisual()
        {
            if(!EffectManager.TryGetID(ModelEffectLookup.Status.QuickHeal, out ushort eID)) {
                Debug.LogError("Failed to retrieve ID for quick heal model effect.");
                return;
            }
            events.Add(new EntitySpEvent(ModelEffect.Instance, new SpEvent.Values(eID, ModelSpotLookup.Core.Root)));
        }
        
        public void DoHeal()
        {
            if(!IsOwner || CurrentCharges < 1)
                return;
            
            DoHealInternal();
            if(!IsServer) {
                DoHealServerRpc();
            } else {
                DoHealNotOwnerRpc();
            }
        }
        
        private void OnTriggerAction()
        {
            if(IsOwner) {
                CurrentCharges--;
                netHealCharges.Value = CurrentCharges;
            }
            Callbacks.Access.OnUseQuickHeal(callbackAccessToken, this, !IsOwner);
        }

        private void DoHealInternal(bool auxOnly = false)
        {
            float speedMult = Stats.GetParam(QuickHealSpeedMult.Instance);
            itemHandler.UseItem(animParams, events, speedMult, onTriggerAction, auxOnly);
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Binder.OnNetworkSpawnCallback(this);
            if(!IsOwner) {
                netHealCharges.OnValueChanged += OnHealChargesChanged;
            }
        }

        private void OnHealChargesChanged(byte prevVal, byte newVal)
        {
            CurrentCharges = newVal;
        }

        void INetworkBindHelperNode.OnBound()
        {
            if(IsOwner) {
                CurrentCharges       = (byte)Stats.GetParam(QuickHealCharges.Instance);
                netHealCharges.Value = CurrentCharges;
                return;
            }
            CurrentCharges = netHealCharges.Value;
        }

        void ILoopUpdate.LoopUpdate()
        {
            
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Reliable)]
        private void DoHealNotOwnerRpc()
        {
            if(IsServer)
                return;
            
            DoHealInternal(true);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
        private void DoHealServerRpc()
        {
            DoHealInternal(true);
            DoHealNotOwnerRpc();
        }
    }
}
