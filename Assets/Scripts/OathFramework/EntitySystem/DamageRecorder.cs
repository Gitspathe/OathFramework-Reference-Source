using OathFramework.Achievements;
using OathFramework.Data.StatParams;
using OathFramework.EntitySystem.Players;
using OathFramework.Networking;
using OathFramework.Pooling;
using OathFramework.Progression;
using OathFramework.Utility;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public class DamageRecorder : NetworkBehaviour, 
        IPoolableComponent, IEntityInitCallback, IEntityTakeDamageCallback, 
        IEntityDieCallback
    {
        [SerializeField] private bool rewardExp = true;
        
        private Dictionary<Entity, Node> nodes          = new();
        private Dictionary<NetClient, Node> playerNodes = new();
        
        public Entity Entity                 { get; private set; }
        public uint TotalDamage              { get; private set; }
        public PoolableGameObject PoolableGO { get; set; }
        
        uint ILockableOrderedListElement.Order => 9_000;

        private void Awake()
        {
            Entity = GetComponent<Entity>();
        }

        public void FireScoredKillCallbacks(in DamageValue lastDamageVal)
        {
            foreach(Node node in nodes.Values) {
                if(node.Entity == null)
                    continue;
                
                PlayerController pController = node.Entity.Controller as PlayerController;
                float ratio = !ReferenceEquals(pController, null) && !ReferenceEquals(pController.NetClient, null)
                    ? GetDamageRatio(pController.NetClient) : GetDamageRatio(node.Entity);
                
                node.Entity.OnScoredKill(Entity, in lastDamageVal, ratio);
            }
        }

        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityTakeDamageCallback)this);
            entity.Callbacks.Register((IEntityDieCallback)this);
        }

        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            Stats stats = entity.CurStats;
            float exp   = stats.GetParam(ExpReward.Instance) * stats.GetParam(ExpMult.Instance);
            if(exp < 1.0f || !rewardExp || !IsOwner)
                return;

            float total     = exp * ProgressionManager.GetExpMult();
            float shared    = total * ProgressionManager.Instance.SharedExpMult;
            float perPlayer = shared / PlayerManager.PlayerCount;
            float split     = total * (1.0f - ProgressionManager.Instance.SharedExpMult);
            foreach(NetClient player in PlayerManager.Players) {
                float ratio = GetDamageRatio(player);
                float add   = split * ratio;
                ProgressionRpcHelper.AddExp(player, (uint)(perPlayer + add));
                if(player == NetClient.Self) {
                    AchievementUtil.KilledEntity(entity, ratio);
                }
            }
            Clear();
        }

        void IEntityTakeDamageCallback.OnDamage(Entity entity, bool fromRpc, in DamageValue val)
        {
            if(!val.GetInstigator(out Entity instigator))
                return;
            
            TotalDamage += val.Amount;
            if(instigator.IsPlayer) {
                NetClient client = ((PlayerController)instigator.Controller).NetClient;
                if(playerNodes.TryGetValue(client, out Node playerNode)) {
                    Node newPlayerNode = new(instigator, playerNode.Damage + val.Amount);
                    playerNodes[client] = newPlayerNode;
                } else {
                    playerNodes[client] = new Node(instigator, val.Amount);
                }
            }
            if(nodes.TryGetValue(instigator, out Node node)) {
                Node newNode = new(instigator, node.Damage + val.Amount);
                nodes[instigator] = newNode;
                return;
            }
            nodes[instigator] = new Node(instigator, val.Amount);
        }

        public uint GetDamage(Entity entity) 
            => !nodes.TryGetValue(entity, out Node node) ? 0 : node.Damage;
        public float GetDamageRatio(Entity entity) 
            => TotalDamage == 0 || !nodes.TryGetValue(entity, out Node node) ? 0.0f : (float)node.Damage / (float)TotalDamage;
        public uint GetDamage(NetClient player) 
            => !playerNodes.TryGetValue(player, out Node node) ? 0 : node.Damage;
        public float GetDamageRatio(NetClient player) 
            => TotalDamage == 0 || !playerNodes.TryGetValue(player, out Node node) ? 0.0f : (float)node.Damage / (float)TotalDamage;

        private void Clear()
        {
            nodes.Clear();
            playerNodes.Clear();
            TotalDamage = 0;
        }
        
        public void OnRetrieve()
        {
            Clear();
        }

        public void OnReturn(bool initialization)
        {
            Clear();
        }
        
        private struct Node
        {
            public Entity Entity { get; }
            public uint Damage   { get; }

            public Node(Entity entity, uint damage)
            {
                Entity = entity;
                Damage = damage;
            }
        }
    }

}
