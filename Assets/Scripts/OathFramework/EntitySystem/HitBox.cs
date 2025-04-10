using OathFramework.Core;
using OathFramework.EntitySystem.Projectiles;
using System;
using UnityEngine;

namespace OathFramework.EntitySystem
{

    [RequireComponent(typeof(Collider))]
    public class HitBox : MonoBehaviour, IHitSurface
    {
        [SerializeField] private float blockingPower    = 100.0f;
        [SerializeField] private float damageMultiplier = 1.0f;
        [SerializeField] private HitBoxFlags flags      = HitBoxFlags.Group1;
        [SerializeField] private BasicEntity sceneEntity;
        
        [field: SerializeField] public bool IsStatic { get; private set; }

        public float DamageMultiplier {
            get => damageMultiplier; 
            set => damageMultiplier = value;
        }

        public bool DefaultIgnoreMelee => (HitBoxFlags.IgnoreMelee & DefaultFlags) != 0;

        public bool IgnoreMelee {
            get => (HitBoxFlags.IgnoreMelee & flags) != 0;
            set {
                if(value) {
                    flags |= HitBoxFlags.IgnoreMelee;
                } else {
                    flags &= ~HitBoxFlags.IgnoreMelee;
                }
            }
        }
        
        public bool DefaultIgnoreProjectile => (HitBoxFlags.IgnoreProjectile & DefaultFlags) != 0;

        public bool IgnoreProjectile {
            get => (HitBoxFlags.IgnoreProjectile & flags) != 0;
            set {
                if(value) {
                    flags |= HitBoxFlags.IgnoreProjectile;
                } else {
                    flags &= ~HitBoxFlags.IgnoreProjectile;
                }
            }
        }
        
        public bool IsDodging { get; set; }
        
        [field: SerializeField] public HitSurfaceMaterial Material { get; private set; }
        
        public HitBoxFlags DefaultFlags { get; private set; }
        public Transform CTransform     { get; private set; }
        public IEntity Entity           { get; private set; }

        private Collider[] myColliders;
        
        public bool HasFlags(HitBoxFlags flags)    => (flags & this.flags) != 0;
        public bool HasGroups(HitBoxGroups groups) => ((HitBoxFlags)groups & flags) != 0;

        private void Awake()
        {
            CTransform   = transform;
            DefaultFlags = flags;
            if(sceneEntity != null) {
                Entity = sceneEntity;
            }
            myColliders ??= GetComponents<Collider>();
        }

        public void RestoreFlags()
        {
            IsDodging = false;
            flags     = DefaultFlags;
        }

        public void OnDeath()
        {
            foreach(Collider collider in myColliders) {
                collider.enabled = false;
            }
        }

        public void Initialize(IEntity entity)
        {
            if(sceneEntity != null) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"Attempted to initialize a HitBox on {gameObject.name} when it has already been applied to a scene Entity.");
                }
                return;
            }

            Entity        = entity;
            myColliders ??= GetComponents<Collider>();
            foreach(Collider col in myColliders) {
                col.enabled = true;
            }
        }

        public void Damage(DamageValue val, HitEffectValue? effect = null, bool ignoreMultiplier = false)
        {
            int amountInt          = (int)(ignoreMultiplier ? val.Amount : val.Amount * damageMultiplier);
            ushort amount          = (ushort)Mathf.Clamp(amountInt, 0.0f, ushort.MaxValue - 1.0f);
            ref DamageValue valRef = ref val;
            valRef.Amount          = amount;
            Entity.Damage(valRef, effect);
        }

        public void Heal(HealValue val, HitEffectValue? effect = null, bool ignoreMultiplier = true)
        {
            int amountInt        = (int)(ignoreMultiplier ? val.Amount : val.Amount * damageMultiplier);
            ushort amount        = (ushort)Mathf.Clamp(amountInt, 0.0f, ushort.MaxValue - 1.0f);
            ref HealValue valRef = ref val;
            valRef.Amount        = amount;
            Entity.Heal(val, effect);
        }

        public HitSurfaceParams GetHitSurfaceParams(Vector3 position) => new(blockingPower, Material);
    }
    
    [Flags]
    public enum HitSurfaceMaterial : uint
    {
        None     = 0,
        Default  = 1,
        Dirt     = 2,
        Rock     = 4,
        Metal    = 8,
        Foliage  = 16,
        Wood     = 32,
        Brick    = 64,
        Concrete = 128,
        Plaster  = 256,
        Water    = 512,
        Glass    = 1024,
        Flesh    = 2048
    }

    [Flags]
    public enum HitBoxFlags : byte
    {
        None             = 0,
        Group1           = 1,
        Group2           = 2,
        Group3           = 4,
        Group4           = 8,
        Group5           = 16,
        Group6           = 32,
        IgnoreMelee      = 64,
        IgnoreProjectile = 128
    }

    [Flags]
    public enum HitBoxGroups : byte
    {
        None             = 0,
        Group1           = 1,
        Group2           = 2,
        Group3           = 4,
        Group4           = 8,
        Group5           = 16,
        Group6           = 32
    }
}
