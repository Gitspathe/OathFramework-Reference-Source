using UnityEngine;
using OathFramework.Core;
using OathFramework.Pooling;
using Unity.Netcode;

namespace OathFramework.Utility
{ 

    public class DestroyAfterTime : LoopComponent, ILoopUpdate
    {
        [SerializeField] private float time;

        private float timeToLive;
        private bool isPooled;
        private bool isNetworked;
        private PoolableGameObject poolable;
        private NetworkObject netObj;

        public override int UpdateOrder => GameUpdateOrder.Default;

        private void Awake()
        {
            poolable    = GetComponent<PoolableGameObject>();
            netObj      = GetComponent<NetworkObject>();
            isPooled    = poolable != null;
            isNetworked = netObj != null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(time > 0.0001f) {
                timeToLive = time;
            }
        }

        public void Set(float ttl)
        {
            timeToLive = ttl;
        }

        void ILoopUpdate.LoopUpdate()
        {
            timeToLive -= Time.deltaTime;
            if(timeToLive > 0.0f)
                return;

            if(isNetworked) {
                if(!NetworkManager.Singleton.IsServer)
                    return;

                netObj.Despawn();
                return;
            }
            if(isPooled && poolable.IsInitialized) {
                PoolManager.Return(poolable);
                return;
            }
            Destroy(this);
        }
    }

}
