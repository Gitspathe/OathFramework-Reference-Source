using System;

namespace OathFramework.Core
{

    public abstract class UpdateTaskBase : ILoopUpdateable
    {
        protected Action Action;
        
        public int UpdateOrder         { get; }
        public bool IsValidForUpdating { get; set; }

        protected UpdateTaskBase(Action action, int updateOrder)
        {
            Action = action;
            UpdateOrder = updateOrder;
        }
        
        public UpdateTaskBase Register()
        {
            UpdateManager.Register(this);
            return this;
        }

        public UpdateTaskBase Unregister()
        {
            UpdateManager.Unregister(this);
            return this;
        }
    }

    public class FixedUpdateTask : UpdateTaskBase, ILoopFixedUpdate
    {
        public FixedUpdateTask(Action onFixedUpdate, int updateOrder = 0) : base(onFixedUpdate, updateOrder) { }
        public void LoopFixedUpdate() => Action.Invoke();
    }
    
    public class EarlyUpdateTask : UpdateTaskBase, ILoopEarlyUpdate
    {
        public EarlyUpdateTask(Action onEarlyUpdate, int updateOrder = 0) : base(onEarlyUpdate, updateOrder) { }
        public void LoopEarlyUpdate() => Action.Invoke();
    }
    
    public class UpdateTask : UpdateTaskBase, ILoopUpdate
    {
        public UpdateTask(Action onUpdate, int updateOrder = 0) : base(onUpdate, updateOrder) { }
        public void LoopUpdate() => Action.Invoke();
    }
    
    public class LateUpdateTask : UpdateTaskBase, ILoopLateUpdate
    {
        public LateUpdateTask(Action onLateUpdate, int updateOrder = 0) : base(onLateUpdate, updateOrder) { }
        public void LoopLateUpdate() => Action.Invoke();
    }

}
