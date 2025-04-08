using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using OathFramework.EntitySystem.Players;
using OathFramework.Networking;
using OathFramework.Persistence;
using Random = UnityEngine.Random;

namespace OathFramework.Core.Service
{

    public class PlayerSpawnService : MonoBehaviour, IResetGameStateCallback
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject deathCamPrefab;

        [Space(10)]

        [SerializeField] private LayerMask spawnCheckMask;
        [SerializeField] private LayerMask spawnBlockMask;
        [SerializeField] private int maxAttempts            = 20;
        [SerializeField] private float spawnRaycastDistance = 30.0f;
        [SerializeField] private float playerHeight         = 2.0f;

        [Space(10)]

        [SerializeField] private float respawnDelay         = 15.0f;

        [Space(10)]

        [SerializeField] private float redeployHoldTime     = 3.0f;
        [SerializeField] private float redeploySpawnDelay   = 5.0f;
        [SerializeField] private float redeployCooldown     = 120.0f;

        private List<(NetClient, float)> pendingRespawns = new();
        private List<PlayerSpawn> spawnAreas             = new();
        //private DeathCamController deathCam;

        public LayerMask SpawnCheckMask   => spawnCheckMask;
        public LayerMask SpawnBlockMask   => spawnBlockMask;
        public float PlayerHeight         => playerHeight;
        public int MaxAttempts            => maxAttempts;
        public float SpawnRaycastDistance => spawnRaycastDistance;
        public float RedeployHoldTime     => redeployHoldTime;

        public float CurrentRedeployCooldown { get; private set; }
        public bool RedeployInCooldown => CurrentRedeployCooldown > 0.001f;
        
        public static PlayerSpawnService Instance { get; private set; }

        public PlayerSpawnService Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(PlayerSpawnService)} singletons.");
                return null;
            }

            //deathCam = Instantiate(deathCamPrefab, transform).GetComponent<DeathCamController>();
            //deathCam.gameObject.SetActive(false);

            Instance = this;
            GameCallbacks.Register((IResetGameStateCallback)this);
            return Instance;
        }

        private void Update()
        {
            CurrentRedeployCooldown -= Time.deltaTime;
            if(!NetGame.Manager.IsServer)
                return;

            for(int i = 0; i < pendingRespawns.Count; i++) {
                (NetClient player, float timeLeft) = pendingRespawns[i];
                timeLeft -= Time.deltaTime;
                if(timeLeft <= 0.0f) {
                    SpawnPlayer(player);
                    pendingRespawns.RemoveAt(i);
                    continue;
                }
                pendingRespawns[i] = new (player, timeLeft);
            }
        }

        public void ShowDeathCam(Transform target)
        {
            //deathCam.Setup(target);
        }

        public void HideDeathCam()
        {
            //deathCam.gameObject.SetActive(false);
        }

        public void QueueRespawn(NetClient client, float respawnDelay)
        {
            if(!NetGame.Manager.IsServer)
                return;

            pendingRespawns.Add((client, respawnDelay));
        }

        public void FindSpawnAreas()
        {
            spawnAreas.Clear();
            GameObject spawnParent = SceneScript.Main.SpawnsParent;
            if(spawnParent == null) {
                Debug.LogError("Could not initialize player spawns. Ensure there is a GameObject titled '_SPAWNS' in the scene.");
                return;
            }

            foreach (PlayerSpawn area in spawnParent.GetComponentsInChildren<PlayerSpawn>()) {
                if(area.gameObject.activeInHierarchy) { 
                    spawnAreas.Add(area);
                }
            }
        }

        public void Redeployed()
        {
            CurrentRedeployCooldown = redeployCooldown;
        }

        public void ResetRedeployCooldown()
        {
            CurrentRedeployCooldown = 0.0f;
        }

        public void SpawnPlayer(NetClient client)
        {
            if(!NetGame.Manager.IsServer) {
                NetGameRpcHelper.RequestPlayerSpawn();
                return;
            }

            PlayerSpawn area;
            int currentAttempts = 0;
            bool foundSpawn     = false;
            Vector3 point       = Vector3.zero;
            while(currentAttempts < maxAttempts) {
                area = spawnAreas[Random.Range(0, spawnAreas.Count)];
                if(area.GetRandomSpawnPoint(out point)) {
                    foundSpawn = true;
                    break;
                }
                currentAttempts++;
            }
            if(!foundSpawn) { 
                Debug.LogError($"Could not find a valid player spawn! Spawning in a random area.");
                area              = spawnAreas[Random.Range(0, spawnAreas.Count - 1)];
                Vector3 randPoint = area.GetRandomPointInsideCollider();
                point             = new Vector3(randPoint.x, randPoint.y + playerHeight, randPoint.z);
            }

            GameObject playerGO = Instantiate(playerPrefab, point, Quaternion.identity);
            playerGO.GetComponent<NetworkObject>().SpawnWithOwnership(client.OwnerClientId);
        }

        public void OnPlayerGOSpawned(PlayerController player, NetClient client)
        {
            client.PlayerController = player;
            client.Data.Apply();
            if(GlobalNetInfo.UsingSnapshot && ProxyDatabase<PlayerProxyComponent>.TryGetProxy(client.UniqueID, out PlayerProxyComponent proxy)) {
                player.GetComponent<PersistentObject>().AssignFromProxy(proxy);
            }
            if(client.IsOwner) {
                //UIScript.Instance.HideDeathUI();
                HideDeathCam();
            } else {
                player.Entity.InitStatsForLateClient();
            }
            player.Entity.OnNetInitializationComplete();
            GameCallbacks.Access.OnPlayerSpawned(Game.AccessToken, client);
        }

        public void OnPlayerDeath(NetClient client)//, DamageValue lastDamageVal)
        {
            //if (NetGame.Manager.IsServer) {
            //    QueueRespawn(client, respawnDelay);
            //    client.LifeState = PlayerLifeState.Dead;
            //}
            GameCallbacks.Access.OnPlayerDeath(Game.AccessToken, client);
        }

        public void Reinitialize()
        {
            ResetRedeployCooldown();
            pendingRespawns.Clear();
            HideDeathCam();
            //UIScript.Instance.HideDeathUI();
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            Reinitialize();
        }
    }

}
