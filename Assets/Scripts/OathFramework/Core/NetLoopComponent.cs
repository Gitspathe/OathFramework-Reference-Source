using Unity.Netcode;

namespace OathFramework.Core
{

    public class NetLoopComponent : NetworkBehaviour, ILoopUpdateable
    {
        public virtual int UpdateOrder => GameUpdateOrder.Default;
        public bool IsValidForUpdating { get; set; }

        protected virtual void OnEnable()
        {
            UpdateManager.Register(this);
        }

        protected virtual void OnDisable()
        {
            UpdateManager.Unregister(this);
        }
    }

}
