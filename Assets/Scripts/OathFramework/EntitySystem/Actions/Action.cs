using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using OathFramework.Attributes;
using System.Threading;

namespace OathFramework.EntitySystem.Actions
{ 

    public abstract class Action : NetworkBehaviour, IArrayElementTitle
    {
        private System.Action<InterruptionSource> onCompleteAction;

        // Assigned by Activate() each time.
        protected bool IsAuxOnly = false;

        [field: SerializeField] public string ID  { get; private set; }

        [field: SerializeField, ArrayElementTitle] 
        public Action[] ActionsOnStart            { get; private set; }
        
        [field: SerializeField, ArrayElementTitle]
        public Action[] ActionsOnComplete         { get; private set; }

        protected InterruptionSource Interruption { get; private set; }
        protected Entity Entity                   { get; private set; }
        public bool IsActive                      { get; private set; }
        
        [field: SerializeField]
        public InterruptionSource InterruptionSources { get; private set; } = InterruptionSource.Death;

        private CancellationToken ct;
        
        string IArrayElementTitle.Name => ID;
        
        protected virtual void Awake()
        {
            ct           = destroyCancellationToken;
            Interruption = InterruptionSource.None;
        }

        public void Initialize(Entity entity)
        {
            Entity = entity;
            OnInitialize();
        }

        protected virtual void OnDisable()
        {
            if(!Entity.NetInitComplete)
                return;
            
            Deactivate(InterruptionSource.Cancel);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if(!Entity.NetInitComplete)
                return;
            
            Deactivate(InterruptionSource.Cancel);
        }

        public void Activate(bool auxOnly = false, System.Action<InterruptionSource> onComplete = null)
        {
            IsActive         = true;
            IsAuxOnly        = auxOnly;
            Interruption     = InterruptionSource.None;
            onCompleteAction = onComplete;
            OnStart(ct);
            foreach(Action start in ActionsOnStart) {
                Entity.Actions.InvokeAction(start, auxOnly);
            }
        }

        public void Deactivate(InterruptionSource interruption)
        {
            IsActive     = false;
            IsAuxOnly    = false;
            Interruption = interruption;
            bool cancel  = interruption != InterruptionSource.None;
            OnEnd();
            if(!cancel && gameObject.activeSelf) {
                onCompleteAction?.Invoke(interruption);
                foreach(Action onComplete in ActionsOnComplete) {
                    Entity.Actions.InvokeAction(onComplete);
                }
            }
            Entity.Actions.ActionCompleted(this);
        }
        
        public async UniTask WaitForCompletion()
        {
            while(true) {
                if(!IsActive)
                    return;

                await UniTask.Yield();
            }
        }

        public void Tick(float deltaTime)
        {
            OnTick(deltaTime);
        }

        public bool IsInterruptedBy(InterruptionSource interruption) 
            => (InterruptionSources & interruption) == interruption;

        protected async UniTask<bool> ShouldExit(UniTask task)
        {
            bool b = await task.SuppressCancellationThrow();
            return b || !IsActive;
        }
        
        protected void Completed()
        {
            Deactivate(InterruptionSource.None);
        }

        protected abstract void OnInitialize();
        protected abstract void OnStart(CancellationToken ct);
        protected abstract void OnTick(float deltaTime);
        protected abstract void OnEnd();
    }

    [System.Flags]
    public enum InterruptionSource : uint
    {
        None    = 0,
        Stagger = 1,
        Death   = 2,
        Damage  = 4,
        Cancel  = 8,
    }

}
