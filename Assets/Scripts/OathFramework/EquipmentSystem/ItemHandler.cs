using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Actions;
using OathFramework.EntitySystem.States;
using OathFramework.Networking;
using OathFramework.Utility;
using UnityEngine;
using Action = System.Action;

namespace OathFramework.EquipmentSystem
{
    [RequireComponent(typeof(Entity))]
    public class ItemHandler : NetLoopComponent, 
        INetworkBindHelperNode, IEntityInitCallback, IEntityStaggerCallback
    {
        private IEntityController controller;
        private QList<EntitySpEvent> itemSpEvents;
        private Action onTriggerAction;
        private bool auxOnly;
        
        [field: SerializeField] public UseAbility UseAbilityAction { get; private set; }

        public Entity Entity => controller.Entity;
        uint ILockableOrderedListElement.Order => 100;

        public NetworkBindHelper Binder { get; set; }

        private void Awake()
        {
            controller = GetComponent<IEntityController>();
        }
        
        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityStaggerCallback)this);
        }
        
        void IEntityStaggerCallback.OnStagger(Entity entity, StaggerStrength strength, Entity instigator)
        {
            if(UseAbilityAction != null) {
                entity.EntityModel.Animator.ResetTrigger(UseAbilityAction.AnimNameHash);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Binder.OnNetworkSpawnCallback(this);
        }

        void INetworkBindHelperNode.OnBound()
        {
            
        }
        
        public void UseItem(
            ActionAnimParams @params, 
            QList<EntitySpEvent> spEvents, 
            float speedMult = 1.0f, 
            Action onTrigger = null, 
            bool auxOnly = false)
        {
            itemSpEvents    = spEvents;
            this.auxOnly    = auxOnly;
            onTriggerAction = onTrigger;
            UseAbilityAction.SetParams(this, @params, speedMult);
            Entity.Actions.InvokeAction(UseAbilityAction, auxOnly);
        }
        
        public void EndItemUse()
        {
            itemSpEvents    = null;
            onTriggerAction = null;
        }

        public bool TriggerEffects()
        {
            if(ReferenceEquals(itemSpEvents, null))
                return false;

            Entity.SpEvents.ApplyEvents(itemSpEvents, auxOnly);
            onTriggerAction?.Invoke();
            itemSpEvents    = null;
            onTriggerAction = null;
            return true;
        }
    }
}
