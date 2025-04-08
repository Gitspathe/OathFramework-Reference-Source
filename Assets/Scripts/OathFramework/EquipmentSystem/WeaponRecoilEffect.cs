using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{
    public class WeaponRecoilEffect : MonoBehaviour, 
        IWeaponModelInit, IEquipmentUseCallback
    {
        [SerializeField] private float amount;
        private IEquipmentUserController owner;
        
        void IWeaponModelInit.OnInitialized(IEquipmentUserController owner)
        {
            this.owner = owner;
            owner.Equipment.Callbacks.Register((IEquipmentUseCallback)this);
        }

        void IEquipmentUseCallback.OnEquipmentUse(EntityEquipment equipment, Equippable equippable, int ammo)
        {
            if(!(owner.Animation is PlayerAnimation playerAnim))
                return;
            
            playerAnim.Model.RecoilHandler.AddRecoil(amount);
        }

        private void OnDisable()
        {
            if(owner == null)
                return;
            
            owner.Equipment.Callbacks.Unregister((IEquipmentUseCallback)this);
        }
    }
}
