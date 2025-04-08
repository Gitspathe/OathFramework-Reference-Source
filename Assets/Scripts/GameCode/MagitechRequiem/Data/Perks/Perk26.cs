using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using System.Collections.Generic;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Adrenaline Surge
    /// Upon reloading, begin regenerating stamina immediately. Additionally, run 20% faster while reloading.
    /// </summary>
    public class Perk26 : Perk
    {
        public override string LookupKey => PerkLookup.Perk26.Key;
        public override ushort? DefaultID => PerkLookup.Perk26.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "speed_amt", "20" } };

        private Callback callback = new();
        public static Perk26 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is IEquipmentUserController equipmentUser))
                return;

            equipmentUser.Equipment.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !(owner.Controller is IEquipmentUserController equipmentUser))
                return;
            
            equipmentUser.Equipment.Callbacks.Unregister(callback);
        }

        private class Callback : IEquipmentBeginReloadCallback
        {
            void IEquipmentBeginReloadCallback.OnEquipmentBeginReload(EntityEquipment equipment, Equippable equippable)
            {
                equipment.Controller.Entity.States.AddState(new EntityState(Perk26State.Instance));
                equipment.Controller.Entity.IncrementStamina(1, true);
            }
        }
    }

    public class Perk26State : PerkState, IUpdateable
    {
        public override string LookupKey     => StateLookup.Perk26State.Key;
        public override ushort? DefaultID    => StateLookup.Perk26State.DefaultID;
        public override float? MaxDuration   => 10.0f;
        public override ushort MaxValue      => 1;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        private HashSet<Entity> affecting     = new();
        private HashSet<Entity> affectingCopy = new();
        
        public static Perk26State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            affecting.Add(entity);
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            affecting.Remove(entity);
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            entity.CurStats.speed *= 1.2f;
        }

        void IUpdateable.Update()
        {
            affectingCopy.Clear();
            foreach(Entity e in affecting) {
                affectingCopy.Add(e);
            }
            foreach(Entity e in affectingCopy) {
                if(e.Controller is IEquipmentUserController equipmentUser && !equipmentUser.Equipment.IsReloading) {
                    e.States.RemoveState(new EntityState(Instance));
                }
            }
            affectingCopy.Clear();
        }
    }
}
