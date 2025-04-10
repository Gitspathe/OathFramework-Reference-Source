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
    /// Innately boost speed by 6%. When HP is below 40%, boost speed by 15%.
    /// </summary>
    public class Perk4 : Perk, IUpdateable
    {
        public override string LookupKey => PerkLookup.Perk4.Key;
        public override ushort? DefaultID => PerkLookup.Perk4.DefaultID;
        
        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new(){ {"amt", "6"}, {"hp_threshold", "50"}, {"amt_2", "15"} };

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
                    stack = 2;
                } else {
                    float missingPercent = 1.0f - ((float)entity.CurStats.health / (float)entity.CurStats.maxHealth);
                    stack                = missingPercent >= 0.6f ? (ushort)2 : (ushort)1;
                }
                entity.States.SetState(new EntityState(Perk4State.Instance, stack));
            }
        }
    }

    public class Perk4State : PerkState
    {
        public override string LookupKey     => StateLookup.Perk4State.Key;
        public override ushort? DefaultID    => StateLookup.Perk4State.DefaultID;
        public override ushort MaxValue      => 2;
        public override bool NetSync         => true;
        public override bool PersistenceSync => true;

        public static Perk4State Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            entity.CurStats.speed *= val == 1 ? 1.06f : 1.15f;
        }
    }
}
