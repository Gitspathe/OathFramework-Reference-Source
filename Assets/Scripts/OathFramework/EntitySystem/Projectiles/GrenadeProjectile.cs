using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.Pooling;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{
    public partial class GrenadeProjectile : LoopComponent, 
        ILoopUpdate, IPoolableComponent, IProjectile
    {
        private DestroyAfterTime ttl;
        private GrenadeProjectileData data;
        private EntityTeams[] targets;
        private PoolableGameObject poolGO;
        private bool isOwner;
        private bool createdExplosion;
        
        IProjectileData IProjectile.ProjectileData => data;
        
        [field: SerializeField] public GrenadeCollider Collider { get; set; }
        [field: SerializeField] public LayerMask LayerMask      { get; private set; }
        [field: SerializeField] public LayerMask BlockingMask   { get; private set; }
        
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;
        
        public PoolableGameObject PoolableGO { get; set; }
        public Transform CTransform          { get; private set; }

        private void Awake()
        {
            poolGO     = GetComponent<PoolableGameObject>();
            ttl        = GetComponent<DestroyAfterTime>();
            CTransform = transform;
        }
        
        public void Initialize(bool isOwner, EntityTeams[] targets, in IProjectileData data, float? distanceOverride = null)
        {
            GrenadeProjectileData gData = (GrenadeProjectileData)data;
            this.isOwner                = isOwner;
            this.targets                = targets;
            this.data                   = gData;
            Collider.gameObject.SetActive(true);
            Collider.AddForce(gData.Force);
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            
        }

        public void CreateExplosion()
        {
            if(createdExplosion)
                return;
            
            createdExplosion = true;
            Collider.gameObject.SetActive(false);
            if(isOwner) {
                EffectManager.Retrieve(
                    data.ExplosionEffectID, 
                    data.Source, 
                    Collider.transform.position, 
                    extraData: data.EquippableID,
                    local: true
                );
            }
        }
        
        private void ReturnProjectile()
        {
            data = default;
            data?.Return();
            if(!ReferenceEquals(ttl, null)) {
                ttl.enabled = true;
                return;
            }
            poolGO.Return();
        }
        
        void IPoolableComponent.OnRetrieve()
        {
            
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            createdExplosion = false;
        }
    }
}
