using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.Extensions;
using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class EffectBox : MonoBehaviour
    {
        [SerializeField] protected Vector3 hitOffset;
        [SerializeField] protected List<HitEffectInfo> effects;

        private EffectBoxCallbacks callbacks;
        private bool hasCallbacks;
        private AccessToken callbackAccessToken;

        private DamageValue? damageValue;
        private HealValue? healValue;

        private EffectBoxUnion union;
        private bool isOwner;
        private EffectBoxType type;
        private List<HitEffectInfo> currentEffects;
        private bool ignoreHitBoxMultiplier;
        private Collider myCollider;
        private EntityTeams[] targetTypes;
        private HashSet<IEntity> ignore = new();
        private HashSet<IEntity> hits   = new();

        public bool HasUnion => !ReferenceEquals(union, null);

        public EffectBoxCallbacks Callbacks {
            get {
                if(hasCallbacks)
                    return callbacks;

                callbacks           = new EffectBoxCallbacks();
                hasCallbacks        = true;
                callbackAccessToken = callbacks.Access.GenerateAccessToken();
                return callbacks;
            }
        }

        protected virtual void Awake()
        {
            myCollider     = GetComponent<Collider>();
            currentEffects = effects;
            Deactivate();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if(!enabled || !gameObject.activeInHierarchy)
                return;
            
            if(targetTypes == null || targetTypes.Length == 0) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"EffectBox on {name} has no target types and will be ignored.");
                }
                return;
            }
            if(!other.gameObject.TryGetComponent(out HitBox hitBox))
                return;

            switch(type) {
                case EffectBoxType.Damage: {
                    TriggerDamage(other, hitBox);
                } break;
                case EffectBoxType.Heal: {
                    TriggerHeal(other, hitBox);
                } break;

                default:
                case EffectBoxType.None: {
                    if(Game.ExtendedDebug) {
                        Debug.LogWarning($"EffectBox on {name} has no type and will be ignored.");
                    }
                    return;
                }
            }
        }

        private void TriggerDamage(Collider hitCollider, HitBox hit)
        {
            if(!damageValue.HasValue) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"EffectBox on {name} has no damage value and will be ignored.");
                }
                return;
            }
            
            DamageValue val = damageValue.Value;
            
            // Ignoring HitBox flags.
            if(hit.IgnoreMelee && val.Source == DamageSource.Melee)
                return;
            if(hit.IgnoreProjectile && val.Source == DamageSource.Projectile)
                return;
            
            IEntity entity = hit.Entity;
            Entity derived = entity as Entity;
            bool isDerived = !ReferenceEquals(derived, null);
            bool hasSrc    = val.GetInstigator(out Entity srcEntity);

            // Ignore list and teams check.
            if(entity == null || !targetTypes.Contains(entity.Team) || ignore.Contains(entity) || !hits.Add(entity))
                return;
            
            // Union check.
            if(HasUnion && union.CheckHit(entity))
                return;
            
            // Client-side hit confirmation.
            if(isDerived && derived.IsPlayer && !derived.IsOwner)
                return;
            
            // Non-owner cannot damage enemies but takes friendly fire damage if needed. This avoids duplicate damage events firing.
            if(!isOwner && hasSrc) {
                if(isDerived && !derived.IsPlayer && EntityTypes.AreEnemies(srcEntity.Team, entity.Team))
                    return;
                if(!isDerived && EntityTypes.AreEnemies(srcEntity.Team, entity.Team))
                    return;
            }
            
            // Handle friendly fire.
            ref DamageValue valRef = ref val;
            if(EntityManager.ApplyFriendlyFireModifiers(entity, ref valRef))
                return; // Total immunity.
            
            // Actually deal the damage, finally.
            valRef.HitPosition        = hitCollider.ClosestPointOnBounds(transform.position + hitOffset);
            HitEffectValue? effectVal = null;
            HitEffectInfo info        = FindHitEffect(hit.Material);
            if(info != null) {
                effectVal = new HitEffectValue(info.hitEffectParams);
            }
            hit.Damage(valRef, effectVal, ignoreHitBoxMultiplier);

            // Register hit to union if the EffectBox has one.
            if(HasUnion) {
                union.RegisterHit(entity);
            }
            
            // Handle callbacks registered to the EffectBox (this).
            if(hasCallbacks) {
                DamageValue modVal = valRef;
                modVal.Amount     *= (ushort)Mathf.Clamp(ignoreHitBoxMultiplier ? 1.0f : hit.DamageMultiplier, 0.0f, ushort.MaxValue);
                callbacks.Access.OnDamagedEntity(callbackAccessToken, entity, in modVal);
            }
        }

        private void TriggerHeal(Collider hitCollider, HitBox hit)
        {
            if(!healValue.HasValue) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"EffectBox on {name} has no heal value and will be ignored.");
                }
                return;
            }
            
            HealValue val  = healValue.Value;
            IEntity entity = hit.Entity;
            
            // Ignore list and teams check.
            if(entity == null || !targetTypes.Contains(entity.Team) || ignore.Contains(entity) || !hits.Add(entity))
                return;
            
            // Union check.
            if(HasUnion && union.CheckHit(entity))
                return;
            
            // Handle healing.
            ref HealValue valRef      = ref val;
            HitEffectValue? effectVal = null;
            HitEffectInfo info        = FindHitEffect(hit.Material);
            if(info != null) {
                effectVal = new HitEffectValue(info.hitEffectParams);
            }
            hit.Heal(valRef, effectVal, ignoreHitBoxMultiplier);
            
            // Register hit to union if the EffectBox has one.
            if(HasUnion) {
                union.RegisterHit(entity);
            }

            // Handle callbacks registered to the EffectBox (this).
            if(hasCallbacks) {
                HealValue modVal = valRef;
                modVal.Amount   *= (ushort)Mathf.Clamp(ignoreHitBoxMultiplier ? 1.0f : hit.DamageMultiplier, 0.0f, ushort.MaxValue);
                callbacks.Access.OnHealedEntity(callbackAccessToken, entity, in modVal);
            }
        }

        public void Activate()
        {
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
            hits.Clear();
        }
        
        protected HitEffectInfo FindHitEffect(HitSurfaceMaterial material)
        {
            foreach(HitEffectInfo effect in effects) {
                if(effect.ContainsMaterial(material))
                    return effect;
            }
            return null;
        }
        
        public void SetHitEffects(List<HitEffectInfo> effects)
        {
            if(effects == null || effects.Count == 0) {
                currentEffects = this.effects;
                return;
            }
            currentEffects = effects;
        }
        
        public void Setup(
            DamageValue val, 
            EntityTeams[] targets, 
            List<HitEffectInfo> hitEffectsOverride = null,
            EffectBoxUnion union                   = null, 
            bool ignoreHitBoxMultiplier            = false, 
            IEntity ignore                         = null, 
            bool isOwner                           = true)
        {
            Clear();
            if(!ReferenceEquals(ignore, null)) {
                this.ignore.Add(ignore);
            }
            targetTypes                 = targets;
            damageValue                 = val;
            this.ignoreHitBoxMultiplier = ignoreHitBoxMultiplier;
            this.isOwner                = isOwner;
            this.union                  = union;
            type                        = EffectBoxType.Damage;
            if(hitEffectsOverride != null) {
                SetHitEffects(hitEffectsOverride);
            }
        }

        public void Setup(
            HealValue val, 
            EntityTeams[] targets, 
            List<HitEffectInfo> hitEffectsOverride = null, 
            EffectBoxUnion union                   = null,
            bool ignoreHitBoxMultiplier            = false,
            IEntity ignore                         = null,
            bool isOwner                           = true)
        {
            Clear();
            if(!ReferenceEquals(ignore, null)) {
                this.ignore.Add(ignore);
            }
            targetTypes                 = targets;
            healValue                   = val;
            this.ignoreHitBoxMultiplier = ignoreHitBoxMultiplier;
            this.isOwner                = isOwner;
            this.union                  = union;
            type                        = EffectBoxType.Heal;
            if(hitEffectsOverride != null) {
                SetHitEffects(hitEffectsOverride);
            }
        }

        private void Clear()
        {
            damageValue = default;
            healValue   = default;
            ignore.Clear();
        }
    }

    public enum EffectBoxType : byte
    {
        None     = 0,
        Damage   = 1,
        Heal     = 2
    }
}
