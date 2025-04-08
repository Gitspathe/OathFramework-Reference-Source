using System.Collections.Generic;
using UnityEngine;
using OathFramework.Extensions;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.Persistence;
using OathFramework.Pooling;
using OathFramework.Utility;

namespace OathFramework.EntitySystem.Projectiles
{ 

    [RequireComponent(typeof(PoolableGameObject))]
    public sealed partial class RaycastProjectile : LoopComponent,
        IPersistableComponent, IPoolableComponent, ILoopUpdate, 
        IProjectile
    {
        private DestroyAfterTime ttl;
        private bool active;
        private float curDistance;
        private float curPenetration;
        private Effect vfxObj;
        private IProjectileVFX iVFX;
        private StdBulletData data;
        private RaycastHit[] hitCache            = new RaycastHit[256];
        private List<RaycastHit> copyList        = new(256);
        private HashSet<IHitSurface> prevHits    = new(256);
        private HashSet<IEntity> prevHitEntities = new(256);
        private EntityTeams[] targets;
        private PoolableGameObject poolGO;
        private bool isOwner;
        private bool missed = true;

        public IProjectileData ProjectileData => data;
        
        [field: SerializeField] public LayerMask LayerMask    { get; private set; }
        [field: SerializeField] public LayerMask BlockingMask { get; private set; }

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
            StdBulletData rData = (StdBulletData)data;
            this.data           = rData;
            this.isOwner        = isOwner;
            this.targets        = targets;
            curPenetration      = rData.Penetration;
            active              = true;
            curDistance         = 0.0f;
            if(!ReferenceEquals(ttl, null)) {
                ttl.enabled = false;
            }
            if(!ReferenceEquals(rData.VFX, null)) {
                Transform muzzle                 = CTransform;
                Entity dEntity                   = data.Source as Entity;
                IEntityControllerBase controller = dEntity?.Controller;
                if(controller is IEquipmentUserController equipmentUser) {
                    muzzle = equipmentUser.Equipment.ThirdPersonEquippableModel.AimStartPoint;
                }
                vfxObj = EffectManager.Retrieve(rData.VFX.ID, data.Source, muzzle.position, muzzle.rotation);
                if(vfxObj.TryGetComponent(out iVFX)) {
                    iVFX.Initialize(CTransform, muzzle, rData.EffectColor);
                }
            }
            Execute(distanceOverride ?? rData.Speed * Time.deltaTime);
        }

        void ILoopUpdate.LoopUpdate()
        {
            if(!active)
                return;
            
            Execute(data.Speed * Time.deltaTime);
        }

        private void Execute(float distance)
        {
            Transform trans = CTransform;

            distance = Mathf.Clamp(distance, 0.0f, data.MaxDistance - curDistance);
            
#if UNITY_EDITOR
            if(Game.DebugGizmos) {
                Vector3 cPos = trans.position;
                Debug.DrawLine(cPos, cPos + (trans.forward * distance), Color.red, 10.0f);
            }
#endif
            
            prevHits.Clear();
            Vector3 forward = trans.forward;
            Vector3 pos     = trans.position;
            Ray ray         = new(pos, forward);
            pos            += forward * distance;
            trans.position  = pos;
            int hits        = Physics.RaycastNonAlloc(ray, hitCache, distance, LayerMask.value, QueryTriggerInteraction.Collide);
            if(hits == 0) {
                curDistance += distance;
                if(data.IsInstant || curDistance > data.MaxDistance) {
                    ReturnProjectile();
                }
                return;
            }
            
            copyList.Clear();
            for(int i = 0; i < hits; i++) {
                copyList.Add(hitCache[i]);
            }
            
            copyList.Sort((x, y) => x.distance.CompareTo(y.distance));
            for(int i = 0; i < hits; i++) {
                RaycastHit hit = copyList[i];
                if(ProcessHit(ref curPenetration, in hit))
                    continue;

                CTransform.position = hit.point;
                ReturnProjectile();
                return;
            }
            curDistance += distance;
            if(!data.IsInstant && curDistance < data.MaxDistance)
                return;

            ReturnProjectile();
        }
        
        private bool ProcessHit(ref float penetration, in RaycastHit hit)
        {
            GameObject go   = hit.collider.gameObject;
            bool hasSurface = go.TryGetComponent(out IHitSurface surface);
            bool isBlocking = BlockingMask.ContainsLayer(go.layer);
            if(!hasSurface && isBlocking)
                return false;
            if(!hasSurface || prevHits.Contains(surface))
                return true;
            
            bool isStatic            = surface.IsStatic;
            HitSurfaceParams @params = surface.GetHitSurfaceParams(hit.point);
            if(surface is HitBox hitBox) {
                IEntity entity = hitBox.Entity;
                if(prevHitEntities.Contains(entity))
                    return true;
                
                if(!hitBox.IgnoreProjectile && !ReferenceEquals(entity, null) && entity != data.Source && targets.Contains(entity.Team)) {
                    bool playSfx       = !entity.PlayedHitEffectThisFrame;
                    DamageValue dmgVal = CreateDamage(penetration / data.Penetration, curDistance + hit.distance, in hit);
                    if(EntityManager.ApplyFriendlyFireModifiers(hitBox.Entity, ref dmgVal))
                        return true;
                    
                    Damage(hitBox, in dmgVal);
                    CreateHitEffect(entity, hitBox.CTransform, @params.Material, playSfx, in hit, in dmgVal);
                    entity.PlayedHitEffectThisFrame = true;
                    prevHitEntities.Add(entity);
                    missed = false;
                }
            } else {
                Transform t = isStatic ? null : go.transform;
                CreateHitEffect(null, t, @params.Material, true, in hit, CreateDamagePlaceholder(in hit));
            }
            prevHits.Add(surface);
            penetration -= @params.BlockingPower;
            return penetration > 0.0f;
        }

        private void CreateHitEffect(
            IEntity target,
            Transform transform,
            HitSurfaceMaterial material,
            bool playSound, 
            in RaycastHit hit,
            in DamageValue damageVal)
        {
            if(data.TryGetEffectOverride(material, out HitEffectInfo effectOverride)) {
                HitEffectValue hitVal = effectOverride.ToValue();
                Color? col            = null;
                target?.TryGetEffectColorOverride(material, out col);
                HitEffectManager.CreateEffect(transform, playSound, in damageVal, in hitVal, col);
                return;
            }
            ProjectileManager.CreateDecal(material, hit.point, hit.normal, playSound, transform);  
        }

        private DamageValue CreateDamage(float mult, float distance, in RaycastHit hit)
        {
            int damage    = ProjectileUtils.DamageDistanceFunc(data.BaseDamage, distance, data.MinDistance, data.MaxDistance, data.DistanceMod);
            ushort amount = (ushort)Mathf.Clamp(damage * mult, 0.0f, ushort.MaxValue - 1.0f);
            return new DamageValue(
                amount,
                hit.point,
                DamageSource.Projectile,
                data.StaggerStrength,
                data.StaggerAmount,
                DamageFlags.HasInstigator, 
                data.Source as Entity,
                null
            );
        }
        
        private DamageValue CreateDamagePlaceholder(in RaycastHit hit)
        {
            return new DamageValue(
                0, 
                hit.point, 
                DamageSource.Projectile, 
                StaggerStrength.None, 
                0,
                DamageFlags.None, 
                data.Source as Entity,
                null
            );
        }
        
        private void Damage(HitBox hitBox, in DamageValue damageVal)
        {
            if(!isOwner)
                return;
            
            hitBox.Damage(damageVal);
        }

        private void ReturnProjectile()
        {
            ProjectileUtils.ProjectileDespawned(this, missed);
            missed = true;
            data   = default;
            active = false;
            data?.Return();
            if(!ReferenceEquals(ttl, null)) {
                ttl.enabled = true;
                return;
            }
            poolGO.Return();
        }
        
        void IPoolableComponent.OnRetrieve() { }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            prevHitEntities.Clear();
            if(ReferenceEquals(vfxObj, null))
                return;
            
            vfxObj.Return();
            if(ReferenceEquals(iVFX, null))
                return;
            
            iVFX.OnBulletReturn();
        }
    }

}
