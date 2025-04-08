using OathFramework.AbilitySystem;
using OathFramework.Data;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Projectiles;
using OathFramework.EntitySystem.States;
using OathFramework.EquipmentSystem;
using OathFramework.Pooling;
using System.Collections.Generic;
using UnityEngine;
using Lookup = GameCode.MagitechRequiem.Data.States.StateLookup.Status;

namespace GameCode.MagitechRequiem.Data.Abilities
{
    public class Ability6 : Ability, IActionAbility
    {
        public override string LookupKey         => "core:gun_buff1";
        public override ushort? DefaultID        => 6;
        public override bool AutoNetSync         => true;
        public override bool AutoPersistenceSync => true;
        public bool SyncActivation               => false;
        public string ActionAnimParams           => ActionAnimParamsLookup.Ability.GunBuff;
        public override bool HasCharges          => true;
        public override bool IsInstant           => true;
        
        public override float GetMaxCooldown(Entity entity)       => 0.5f;
        public override byte GetMaxCharges(Entity entity)         => 1;
        public override float GetMaxChargeProgress(Entity entity) => 60.0f;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) => new() {
            { "duration", "15" }, { "dmg_increase", "20" }, { "penetration_increase", "50" }
        };
        
        public static Ability6 Instance { get; private set; }
        
        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnActivate(Entity invoker, bool auxOnly, bool lateJoin)
        {
            ApplyClipData(invoker);
            if(!invoker.IsOwner)
                return;
            
            invoker.States.AddState(new EntityState(GunBuff1State.Instance, 1), false, false);
        }

        public void OnInvoked(Entity invoker, bool auxOnly, bool lateJoin)
        {
            ApplyClipData(invoker);
        }

        public void OnCancelled(Entity invoker, bool auxOnly, bool lateJoin)
        {
            
        }
    }
    
    public class GunBuff1State : State
    {
        public override string LookupKey     => Lookup.GunBuff1.Key;
        public override ushort? DefaultID    => Lookup.GunBuff1.DefaultID;
        public override ushort MaxValue      => 1;
        public override float? MaxDuration   => 15.0f;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        private ushort vfxID;
        private Dictionary<Entity, Mod> mods = new();

        public static GunBuff1State Instance { get; private set; }
        
        protected override void OnInitialize()
        {
            Instance = this;
            EffectManager.TryGetID("core:firearm_buff_aura_1", out vfxID);
        }

        protected override void OnApplied(Entity entity, bool lateJoin, ushort val, ushort? originalVal)
        {
            if(originalVal != null || !(entity.Controller is IEquipmentUserController equipmentUser))
                return;

            EntityEquipment equip            = equipmentUser.Equipment;
            EquippableThirdPersonModel model = equip.ThirdPersonEquippableModel;
            if(model != null && model.TryGetComponent(out ModelSocketHandler sockets)) { 
                EffectManager.Retrieve(vfxID, sockets: sockets, modelSpot: ModelSpotLookup.EquippableRanged.Root);
            }
            Mod mod = StaticObjectPool<Mod>.Retrieve().Initialize(equipmentUser, equip.CurrentSlot.EquippableNetID);
            equip.Projectiles.Callbacks.Register(mod);
            mods.Add(entity, mod);
        }

        protected override void OnRemoved(Entity entity, bool lateJoin, ushort val, ushort originalVal)
        {
            if(!(entity.Controller is IEquipmentUserController equipmentUser))
                return;

            if(mods.TryGetValue(entity, out Mod mod)) {
                equipmentUser.Equipment.Projectiles.Callbacks.Unregister(mod);
                mod.Reset();
                mods.Remove(entity);
                StaticObjectPool<Mod>.Return(mod);
            }
            equipmentUser.Equipment.ClearVFX(vfxID, ModelPlugRemoveBehavior.Dissipate);
        }

        private class Mod : IOnPreProjectileSpawned
        {
            private IEquipmentUserController equipmentUser;
            private ushort equippableID;

            public Mod() { }

            public Mod Initialize(IEquipmentUserController user, ushort equippable)
            {
                equipmentUser = user;
                equippableID  = equippable;
                return this;
            }

            public void Reset()
            {
                equipmentUser = null;
                equippableID  = 0;
            }
            
            void IOnPreProjectileSpawned.OnPreProjectileSpawned(ref ProjectileParams @params, ref IProjectileData data)
            {
                if(equipmentUser.Equipment.CurrentSlot.IsEmpty || equipmentUser.Equipment.CurrentSlot.Equippable.ID != equippableID)
                    return;

                if(data is StdBulletData stdBullet) {
                    stdBullet.BaseDamage   = (ushort)Mathf.Clamp(stdBullet.BaseDamage * 1.2f, 0.0f, ushort.MaxValue);
                    stdBullet.Penetration += 50;
                }
            }
        }
    }
}
