using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Projectiles;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Deliverer
    /// The last shot of your ammo deals 25% more damage.
    /// </summary>
    public class Perk15 : Perk
    {
        public override string LookupKey => PerkLookup.Perk15.Key;
        public override ushort? DefaultID => PerkLookup.Perk15.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "25%" } };

        private Callback callback = new();
        
        public static Perk15 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || owner.IsDummy)
                return;
            
            owner.GetComponent<ProjectileProvider>().Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || owner.IsDummy)
                return;
            
            owner.GetComponent<ProjectileProvider>().Callbacks.Unregister(callback);
        }

        private class Callback : IOnPreProjectileSpawned
        {
            void IOnPreProjectileSpawned.OnPreProjectileSpawned(ref ProjectileParams @params, ref IProjectileData data)
            {
                if(data == null || !(data.Source is Entity dEntity) || !dEntity.TryGetComponent(out EntityEquipment equipment))
                    return;

                bool isLast = equipment.CurrentSlot.Ammo == 1;
                if(isLast) {
                    data.BaseDamage = (ushort)(data.BaseDamage * 1.25f);
                }
            }
        }
    }
}
