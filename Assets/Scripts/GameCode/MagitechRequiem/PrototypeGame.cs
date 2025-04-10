using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameCode.MagitechRequiem
{

    public class PrototypeGame : NetworkBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject minibossPrefab;
        [SerializeField] private Transform[] spawnPoints;

        [SerializeField] private AnimationCurve spawnCurve;
        [SerializeField] private float maxTime;
        [SerializeField] private float stopTime;
        [SerializeField] private int maxMonsters = 6;
        [SerializeField] private int maxMiniboss = 1;

        private HashSet<GameObject> monsters   = new();
        private HashSet<GameObject> miniBosses = new();
        private float curTime = 0.0001f;
        private float timer;
        
        private float SpawnMult => Mathf.Clamp(curTime / maxTime, 0.0f, 1.0f);
        
        private void Update()
        {
            if(!IsOwner || !IsSpawned)
                return;
            
            curTime += Time.deltaTime;
            if(curTime >= stopTime || monsters.Count >= maxMonsters * PlayerManager.PlayerCount)
                return;
            
            timer -= Time.deltaTime;
            while(timer <= 0.0f) {
                SpawnEnemy();
                timer += spawnCurve.Evaluate(SpawnMult);
            }
        }

        private void SpawnEnemy()
        {
            if(miniBosses.Count < maxMiniboss && FRandom.Cache.Range(1, 10) == 5) {
                SpawnMiniboss();
                return;
            }
            Transform t     = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject inst = PoolManager.Retrieve(enemyPrefab, t.position, Quaternion.identity).gameObject;
            inst.GetComponent<NetworkObject>().Spawn(true);
            inst.GetComponent<Entity>().Callbacks.Register(new DeathCallback(this, false));
            monsters.Add(inst);
        }

        private void SpawnMiniboss()
        {
            Transform t     = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject inst = PoolManager.Retrieve(minibossPrefab, t.position, Quaternion.identity).gameObject;
            inst.GetComponent<NetworkObject>().Spawn(true);
            inst.GetComponent<Entity>().Callbacks.Register(new DeathCallback(this, true));
            monsters.Add(inst);
            miniBosses.Add(inst);
        }

        private class DeathCallback : IEntityDieCallback
        {
            private PrototypeGame game;
            private bool isMiniboss;

            public DeathCallback(PrototypeGame game, bool isMiniboss)
            {
                this.game       = game;
                this.isMiniboss = isMiniboss;
            }
            
            uint ILockableOrderedListElement.Order => 100;

            void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
            {
                game.monsters.Remove(entity.gameObject);
                if(isMiniboss) {
                    game.miniBosses.Remove(entity.gameObject);
                }
                entity.Callbacks.Unregister(this);
            }
        }
    }

}
