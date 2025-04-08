using OathFramework.AbilitySystem;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.EquipmentSystem;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Abilities
{
    public class Ability5 : Ability
    {
        public override string LookupKey         => "core:flashbang";
        public override ushort? DefaultID        => 5;
        public override bool AutoNetSync         => true;
        public override bool AutoPersistenceSync => true;
        public override bool HasCharges          => true;
        public override bool IsInstant           => true;
        public override bool AutoChargeDecrement => false;

        public override float GetMaxCooldown(Entity entity)       => 0.5f;
        public override byte GetMaxCharges(Entity entity)         => 2;
        public override float GetMaxChargeProgress(Entity entity) => 17.5f;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) => new() {
            { "duration", "3" }, { "radius", "8" }
        };
        
        public static Ability5 Instance { get; private set; }

        private OnEquipmentUseCallback callback;
        
        protected override void OnInitialize()
        {
            Instance = this;
            callback = new OnEquipmentUseCallback(this);
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            PlayerController player = owner.Controller as PlayerController;
            if(owner.IsDummy || !owner.IsOwner || player == null || !TryGetAssignedSlotAsEquipmentSlot(owner, out EquipmentSlot slot))
                return;
            
            player.Equipment.SetEquippableForSlot(slot, "core:ability_flashbang");
            player.Equipment.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            PlayerController player = owner.Controller as PlayerController;
            if(owner.IsDummy || !owner.IsOwner || player == null || !TryGetAssignedSlotAsEquipmentSlot(owner, out EquipmentSlot slot))
                return;
            
            player.Equipment.SetEquippableForSlot(slot, "");
            player.Equipment.Callbacks.Unregister(callback);
        }

        protected override void OnActivate(Entity invoker, bool auxOnly, bool lateJoin)
        {
            if(!invoker.IsOwner)
                return;
            
            PlayerController player = invoker.Controller as PlayerController;
            if(player == null || !TryGetAssignedSlotAsEquipmentSlot(invoker, out EquipmentSlot slot))
                return;
            
            player.Equipment.SetCurrentSlot(slot);
        }
        
        private class OnEquipmentUseCallback : IEquipmentUseCallback
        {
            private Ability5 ability;

            public OnEquipmentUseCallback(Ability5 ability)
            {
                this.ability = ability;
            }
            
            void IEquipmentUseCallback.OnEquipmentUse(EntityEquipment equipment, Equippable equippable, int ammo)
            {
                PlayerController pController = equipment.Controller as PlayerController;
                if(pController == null || equippable.EquippableKey != "core:ability_flashbang")
                    return;

                pController.Abilities.DecrementCharge(ability);
            }
        }
    }
}
