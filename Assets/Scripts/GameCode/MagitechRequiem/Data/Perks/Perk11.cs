using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using System.Collections.Generic;
using UnityEngine;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Arcane Reserves
    /// Slowly replenish ammo in unequipped slots.
    /// </summary>
    public class Perk11 : Perk, IUpdateable
    {
        public override string LookupKey => PerkLookup.Perk11.Key;
        public override ushort? DefaultID => PerkLookup.Perk11.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { };

        private Dictionary<Entity, (float, float)> progressDict = new();
        private Dictionary<Entity, (float, float)> copy = new();
        
        public static Perk11 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || owner.IsDummy)
                return;
            
            progressDict.Add(owner, default);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || owner.IsDummy)
                return;
            
            progressDict.Remove(owner);
            copy.Remove(owner);
        }

        void IUpdateable.Update()
        {
            copy.Clear();
            foreach((Entity owner, (float primaryProgress, float secondaryProgress) tuple) in progressDict) {
                bool execPrimary, execSecondary;
                float primaryProgress     = tuple.primaryProgress;
                float secondaryProgress   = tuple.secondaryProgress;
                EntityEquipment equipment = owner.GetComponent<EntityEquipment>();
                if(equipment == null || owner.IsDead) {
                    copy.Add(owner, tuple);
                    continue;
                }
                switch(equipment.CurrentSlot.SlotID) {
                    case EquipmentSlot.Primary: {
                        execPrimary   = false;
                        execSecondary = true;
                    } break;
                    case EquipmentSlot.Secondary: {
                        execPrimary   = true;
                        execSecondary = false;
                    } break;

                    case EquipmentSlot.None:
                    case EquipmentSlot.Melee:
                    case EquipmentSlot.Special1:
                    case EquipmentSlot.Special2:
                    case EquipmentSlot.Special3:
                    default: {
                        execPrimary   = true;
                        execSecondary = true;
                    } break;
                }
                if(execPrimary)   { Exec(equipment, equipment.GetInventorySlot(EquipmentSlot.Primary), ref primaryProgress); }
                if(execSecondary) { Exec(equipment, equipment.GetInventorySlot(EquipmentSlot.Secondary), ref secondaryProgress); }
                copy.Add(owner, (primaryProgress, secondaryProgress));
            }
            progressDict.Clear();
            foreach(KeyValuePair<Entity, (float, float)> toAdd in copy) {
                progressDict.Add(toAdd.Key, toAdd.Value);
            }
            return;
            
            void Exec(EntityEquipment equipment, InventorySlot slot, ref float curProgress)
            {
                if(slot == null || slot.IsEmpty || !slot.IsRanged)
                    return;

                EquippableRanged equippable = slot.Equippable.As<EquippableRanged>();
                float perSecond             = Mathf.Clamp(equippable.Stats.AmmoCapacity / 30.0f, 0.0f, 1.0f);
                float add                   = perSecond * Time.deltaTime;
                curProgress                += add;
                while(curProgress > 1.0f) {
                    curProgress    -= 1.0f;
                    ushort nextAmmo = (ushort)Mathf.Clamp(slot.Ammo + 1, 0, equippable.Stats.AmmoCapacity);
                    equipment.SetAmmoForSlot(slot.SlotID, nextAmmo);
                }
            }
        }
    }
}
