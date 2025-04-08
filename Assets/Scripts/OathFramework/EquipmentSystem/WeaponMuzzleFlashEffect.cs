using OathFramework.Audio;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{
    public class WeaponMuzzleFlashEffect : MonoBehaviour, 
        IEquippableManagerInit, IWeaponModelInit, IEquipmentUseCallback
    {
        [field: SerializeField] public Transform MuzzleFlashParent  { get; private set; }
        [field: SerializeField] public PoolParamsSO MuzzleFlashPool { get; private set; }

        private IEquipmentUserController owner;
        private AudioOverrides audioOverride;

        private void Awake()
        {
            audioOverride = AudioOverrides.NoSpatialBlend;
        }

        private void OnDisable()
        {
            if(owner != null) {
                owner.Equipment.Callbacks.Unregister((IEquipmentUseCallback)this);
            }
        }

        void IWeaponModelInit.OnInitialized(IEquipmentUserController owner)
        {
            this.owner = owner;
            owner.Equipment.Callbacks.Register((IEquipmentUseCallback)this);
        }

        void IEquipmentUseCallback.OnEquipmentUse(EntityEquipment equipment, Equippable equippable, int ammo)
        {
            if(ReferenceEquals(MuzzleFlashPool, null) || ReferenceEquals(MuzzleFlashParent, null))
                return;

            GameObject go = PoolManager.Retrieve(MuzzleFlashPool.Params.Prefab, null, null, Vector3.one).gameObject;
            go.GetComponent<DelayedTransform>().SetTarget(MuzzleFlashParent);
        }

        void IEquippableManagerInit.OnEquippableManagerInit()
        {
            PoolManager.RegisterPool(new PoolManager.GameObjectPool(MuzzleFlashPool.Params), true);
        }
    }
}
