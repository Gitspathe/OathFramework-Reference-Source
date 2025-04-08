using OathFramework.Data.StatParams;
using OathFramework.EntitySystem;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using OathFramework.Utility;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Medic
    /// 50% of quick heal amount is also restored to allies within 15 meters.
    /// </summary>
    public class Perk9 : Perk
    {
        public override string LookupKey => PerkLookup.Perk9.Key;
        public override ushort? DefaultID => PerkLookup.Perk9.DefaultID;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new() { { "amt", "50%" }, { "range", "15" } };

        private Callback callback = new();
        
        public static Perk9 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnAdded(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !owner.TryGetComponent(out QuickHealHandler handler))
                return;
            
            handler.Callbacks.Register(callback);
        }

        protected override void OnRemoved(Entity owner, bool auxOnly, bool lateJoin)
        {
            if(auxOnly || !owner.TryGetComponent(out QuickHealHandler handler))
                return;
            
            handler.Callbacks.Unregister(callback);
        }

        private class Callback : IOnUseQuickHealCallback
        {
            private QList<EntityDistance> entityCache = new();
            
            void IOnUseQuickHealCallback.OnUseQuickHeal(QuickHealHandler handler, bool auxOnly)
            {
                if(auxOnly)
                    return;
                
                entityCache.Clear();
                ushort amt = (ushort)(handler.Entity.CurStats.GetParam(QuickHealAmount.Instance) * 0.5f); 
                handler.Entity.Targeting.GetDistances(entityCache, handler.Entity.Team);
                for(int i = 0; i < entityCache.Count; i++) {
                    EntityDistance dist = entityCache.Array[i];
                    if(dist.Distance > 15.0f || dist.Entity == handler.Entity)
                        continue;
                    
                    dist.Entity.Heal(new HealValue(amt, instigator: handler.Entity), new HitEffectValue("core:heal1"));
                }
            }
        }
    }
}
