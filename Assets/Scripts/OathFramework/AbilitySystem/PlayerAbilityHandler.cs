using OathFramework.EntitySystem.Players;
using OathFramework.EquipmentSystem;
using OathFramework.Networking;
using OathFramework.Progression;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.AbilitySystem
{
    public class PlayerAbilityHandler : AbilityHandler, INetworkBindHelperNode
    {
        private PlayerController controller;
        private EntityEquipment equipment;
        private QList<Ability> abilities = new();

        public NetworkBindHelper Binder          { get; set; }
        public ExtBool.Handler UseAbilityBlocked { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            equipment         = GetComponent<EntityEquipment>();
            controller        = GetComponent<PlayerController>();
            equipment         = GetComponent<EntityEquipment>();
            UseAbilityBlocked = ExtBool.Handler.CreateFunc(this, UseAbilityBlockFlag);
            return;

            bool UseAbilityBlockFlag(PlayerAbilityHandler t)
                => t.equipment.IsReloading 
                   || t.equipment.Controller.ActionBlocks.Contains(ActionBlockers.Dodge)
                   || t.equipment.Controller.ActionBlocks.Contains(ActionBlockers.Stagger)
                   || t.equipment.Controller.ActionBlocks.Contains(ActionBlockers.AbilityUse);
        }

        public override void OnNetworkSpawn()
        {
            Binder.OnNetworkSpawnCallback(this);
            base.OnNetworkSpawn();
        }
        
        void INetworkBindHelperNode.OnBound()
        {
            if(!IsOwner || GlobalNetInfo.UsingSnapshot)
                return;
            
            Assign(controller.NetClient.Data.CurrentBuild);
        }

        public override void LoopLateUpdate()
        {
            base.LoopLateUpdate();
            if(!IsOwner)
                return;
            
            AddChargeProgress(1.0f * Time.deltaTime);
        }

        public void Assign(in PlayerBuildData data)
        {
            ClearAbilities(false);
            abilities.Clear();
            for(int i = 0; i < 2; i++) {
                abilities.Add(null);
            }
            if(!string.IsNullOrEmpty(data.ability1) && AbilityManager.TryGet(data.ability1, out Ability ability)) {
                abilities.Array[0] = ability;
                AddAbility(new EntityAbility(ability, Entity), false, false);
            }
            if(!string.IsNullOrEmpty(data.ability2) && AbilityManager.TryGet(data.ability2, out ability)) {
                abilities.Array[1] = ability;
                AddAbility(new EntityAbility(ability, Entity), false, false);
            }
        }

        public bool TryGetAbility(Ability ability, out EntityAbility entityAbility, out byte index)
        {
            entityAbility = default;
            index         = 0;
            for(byte i = 0; i < abilities.Count; i++) {
                Ability a = abilities.Array[i];
                if(a == null || a.ID != ability.ID || !TryGetEntityAbility(ability, out entityAbility))
                    continue;
                
                index = i;
                return true;
            }
            return false;
        }

        public bool TryGetAbilityAtIndex(int index, out EntityAbility entityAbility)
        {
            entityAbility = default;
            if(index >= abilities.Count)
                return false;
            
            Ability ability = abilities.Array[index];
            return ability != null && TryGetEntityAbility(ability, out entityAbility);
        }

        public void UseAbility(byte index)
        {
            Ability use = null;
            if(index < abilities.Count) {
                use = abilities.Array[index];
            }
            if(use != null && use.GetUsable(Entity)) {
                ActivateAbility(use);
            }
        }
    }
}
