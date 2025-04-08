using OathFramework.EntitySystem;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OathFramework.Effects
{
    public class EntityMapDecalSpawner : MonoBehaviour, IEntityInitCallback, IEntityTakeDamageCallback
    {
        [SerializeField] private MapDecalParams decalParams;
        [SerializeField] private bool customDecalColor;
       
        [SerializeField, ShowIf("@customDecalColor")] 
        private Color decalColor;

        private Entity entity;
        
        uint ILockableOrderedListElement.Order => 1;

        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            this.entity = entity;
            entity.Callbacks.Register((IEntityTakeDamageCallback)this);
        }

        void IEntityTakeDamageCallback.OnDamage(Entity entity, bool fromRpc, in DamageValue val)
        {
            if(!IsDamageSourceValid(val.Source) || ReferenceEquals(decalParams, null))
                return;
            
            SpawnGroundDecal(in val);
            if(val.Source == DamageSource.Projectile && val.HasInstigator) {
                SpawnWallDecal(in val);
            }
        }

        private void SpawnGroundDecal(in DamageValue val)
        {
            if(!Physics.Raycast(val.HitPosition, Vector3.down, out RaycastHit hitInfo, 5.0f, MapDecalsManager.Instance.RaycastLayers))
                return;

            Quaternion rot = Quaternion.LookRotation(hitInfo.normal, Vector3.up);
            MapDecalsManager.CreateMapDecal(decalParams, hitInfo.point, rot, customDecalColor ? decalColor : null);
        }

        private void SpawnWallDecal(in DamageValue val)
        {
            val.GetInstigator(out Entity e);
            Vector3 direction = e.transform.forward;
            
#if UNITY_EDITOR
            Debug.DrawRay(val.HitPosition, direction * 3.0f, Color.cyan, 1.0f);
#endif
            
            if(!Physics.Raycast(val.HitPosition, direction.normalized, out RaycastHit hitInfo, 3.0f, MapDecalsManager.Instance.RaycastLayers))
                return;
            
            Quaternion rot = Quaternion.LookRotation(hitInfo.normal, Vector3.up);
            MapDecalsManager.CreateMapDecal(decalParams, hitInfo.point, rot, customDecalColor ? decalColor : null);
        }

        private static bool IsDamageSourceValid(DamageSource source)
        {
            switch(source) {
                case DamageSource.Undefined:
                case DamageSource.Status:
                case DamageSource.SyncDeath:
                case DamageSource.DieCommand:
                    return false;
                
                default:
                    return true;
            }
        }
    }
}
