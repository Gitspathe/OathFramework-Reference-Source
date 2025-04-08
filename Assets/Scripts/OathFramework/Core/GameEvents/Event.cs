using Cysharp.Threading.Tasks;

namespace OathFramework.Core.GameEvents
{
    public abstract class EventBase : LoopComponent
    {
        public override int UpdateOrder => GameUpdateOrder.Default;
        public abstract Event.Type EventType { get; }
        public UniTask Task                  { get; protected set; }
        public bool IsComplete               { get; protected set; }
        public bool IsActive                 { get; protected set; }
        public bool IsInitialized            { get; private set; }
        
        public void Initialize()
        {
            if(IsInitialized)
                return;
            
            OnInitialize();
            IsInitialized = true;
            IsComplete    = false;
        }
        
        public void Deactivate(bool complete)
        {
            if(!IsActive)
                return;

            IsActive   = false;
            IsComplete = true;
            EventManager.DeactivatedCallback(EventType);
            OnDeactivate(complete);
        }
        
        protected abstract void OnInitialize();
        protected abstract void OnDeactivate(bool complete);
        protected abstract void OnActivate();
    }
    
    public abstract class Event : EventBase
    {
        public void Activate()
        {
            if(IsActive) {
                Deactivate(false);
            }
            IsActive    = true;
            IsComplete  = false;
            OnActivate();
        }
        
        public abstract UniTask<Event> WaitForCompletion();
        
        public enum Type
        {
            None             = 0,
            ScreenTransition = 1
        }
    }

    public abstract class Event<TInput> : EventBase
    {
        public TInput Input { get; private set; }
        
        public void Activate(TInput input)
        {
            if(IsActive) {
                Deactivate(false);
            }
            Input       = input;
            IsActive    = true;
            IsComplete  = false;
            OnActivate();
        }
        
        public abstract UniTask<Event<TInput>> WaitForCompletion();
    }
}
