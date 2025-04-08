using System;
using UnityEngine;

namespace OathFramework.Core
{
    
    public abstract class LoopComponent : MonoBehaviour, ILoopUpdateable
    {
        public virtual int UpdateOrder => GameUpdateOrder.Default;
        public virtual Func<bool> ShouldRegisterDelegate { get; }
        public bool IsValidForUpdating { get; set; }

        protected virtual void OnEnable()
        {
            if(ShouldRegisterDelegate == null || ShouldRegisterDelegate.Invoke()) {
                UpdateManager.Register(this);
            }
        }

        protected virtual void OnDisable()
        {
            UpdateManager.Unregister(this);
        }
    }

}
