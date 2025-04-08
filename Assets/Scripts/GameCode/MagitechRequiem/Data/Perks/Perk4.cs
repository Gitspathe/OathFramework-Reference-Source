using GameCode.MagitechRequiem.Data.EntityStates;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.PerkSystem;
using System.Collections.Generic;
using StateLookup = GameCode.MagitechRequiem.Data.States.StateLookup.PerkStates;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Flight Response
    /// Boost speed by 5.0% + 1% for every 10% of missing hp
    /// </summary>
    public class Perk4 : Perk, IUpdateable
    {
        public override string LookupKey => PerkLookup.Perk4.Key;
        public override ushort? DefaultID => PerkLookup.Perk4.DefaultID;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new(){ {"amt", "5% + 1%"}, {"missing_hp", "10%"} };

        public static Perk4 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        public void Update()
        {
            foreach(Entity entity in Assigned.Current) {
                if(!entity.IsOwner)
                    continue;
                
                ushort stack;
                if(entity.CurStats.health == 0 || entity.CurStats.maxHealth == 0) {
                    stack = 10;
                } else {
                    float missingPercent = 1.0f - ((float)entity.CurStats.health / (float)entity.CurStats.maxHealth);
                    stack = (ushort)(missingPercent * 10.0f);
                }
                entity.States.SetState(new EntityState(Perk4State.Instance, stack));
            }
        }
    }

    public class Perk4State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk4State.Key;
        public override ushort? DefaultID    => StateLookup.Perk4State.DefaultID;
        public override ushort MaxValue      => 10;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        public static Perk4State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            entity.CurStats.speed *= 1.05f + (val * 0.01f);
        }
    }
}
