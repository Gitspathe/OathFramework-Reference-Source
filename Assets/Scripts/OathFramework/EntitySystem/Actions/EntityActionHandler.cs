using OathFramework.Core;
using OathFramework.Utility;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public class EntityActionHandler : NetLoopComponent, 
        ILoopUpdate, IEntityTakeDamageCallback, IEntityInitCallback, 
        IEntityDieCallback, IEntityStaggerCallback
    {
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;

        public Entity Entity { get; private set; }

        private HashSet<Action> runningActions        = new();
        private HashSet<Action> toRemove              = new();
        private Dictionary<string, Action> actionDict = new();
        
        uint ILockableOrderedListElement.Order => 10_000;
        
        private void Awake()
        {
            Entity = GetComponent<Entity>();
            foreach(Action action in GetComponentsInChildren<Action>()) {
                actionDict.Add(action.ID, action);
                action.Initialize(Entity);
            }
        }

        void ILoopUpdate.LoopUpdate()
        {
            foreach(Action action in runningActions) {
                if(!action.IsActive)
                    continue;
                
                action.Tick(Time.deltaTime);
            }
            foreach(Action action in toRemove) {
                runningActions.Remove(action);
            }
            toRemove.Clear();
        }

        public void GetActionsOfType<T>(QList<T> actions) where T : Action
        {
            foreach(Action action in actionDict.Values) {
                if(action.GetType() == typeof(T)) {
                    actions.Add((T)action);
                }
            }
        }

        public Action InvokeAction(string id, bool auxOnly = false, System.Action<InterruptionSource> onComplete = null)
        {
            if(!actionDict.TryGetValue(id, out Action action)) {
                Debug.LogError($"No entity action with ID '{id}' found.");
                return null;
            }
            
            return InvokeAction(action, auxOnly, onComplete);
        }

        public Action InvokeAction(Action action, bool auxOnly = false, System.Action<InterruptionSource> onComplete = null)
        {
            toRemove.Remove(action);
            runningActions.Add(action);
            action.Activate(auxOnly, onComplete);
            return action;
        }

        public void ActionCompleted(Action action)
        {
            toRemove.Add(action);
        }

        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityTakeDamageCallback)this);
            entity.Callbacks.Register((IEntityStaggerCallback)this);
            entity.Callbacks.Register((IEntityDieCallback)this);
        }

        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            foreach(Action action in runningActions) {
                if(!action.IsActive)
                    continue;
                
                if(action.IsInterruptedBy(InterruptionSource.Death)) {
                    action.Deactivate(InterruptionSource.Death);
                }
            }
        }

        void IEntityStaggerCallback.OnStagger(Entity entity, StaggerStrength strength, Entity instigator)
        {
            foreach(Action action in runningActions) {
                if(!action.IsActive)
                    continue;
                
                if(action.IsInterruptedBy(InterruptionSource.Stagger)) {
                    action.Deactivate(InterruptionSource.Stagger);
                }
            }
        }

        void IEntityTakeDamageCallback.OnDamage(Entity entity, bool fromRpc, in DamageValue val)
        {
            foreach(Action action in runningActions) {
                if(!action.IsActive)
                    continue;
                
                if(action.IsInterruptedBy(InterruptionSource.Damage)) {
                    action.Deactivate(InterruptionSource.Damage);
                }
            }
        }
        
        [Rpc(SendTo.NotServer)]
        public void InvokeActionClientRpc(NetworkBehaviourReference actionRef, RpcParams @params = default)
        {
            if(IsOwner)
                return;
            
            actionRef.TryGet(out Action action);
            Entity.Actions.InvokeAction(action);
        }
    }

}
