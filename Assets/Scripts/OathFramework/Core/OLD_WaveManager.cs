using OathFramework.Attributes;
using OathFramework.EntitySystem;
using OathFramework.Pooling;
using OathFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OathFramework.Core
{ 

    public class OLD_WaveManager : MonoBehaviour
    {
        public float startDelay       = 5.0f;
        public float timeBetweenWaves = 15.0f;
        public int startWave          = 1;
        public int maxWaves           = 100;
        public int maxEnemies         = 25;
        public int bossWaves          = 5;
        public int baseEnemies        = 5;
        public int addEnemies         = 2;

        [Space(10)]

        [ArrayElementTitle] public SpawnType[] spawns;
        [ArrayElementTitle] public SpawnType[] bossSpawns;

        private float timeUntilNextSpawn;
        private List<float> spawnWeights = new();
        private List<float> bossSpawnWeights = new();
        private List<SpawnPoint> spawnPoints = new();
        private List<SpawnPoint> bossSpawnPoints = new();
        private List<SpawnPoint> tempSpawnPoints = new();

        public bool IsActive        { get; private set; }
        public int CurrentWave      { get; private set; }
        public int CurrentEnemies   { get; private set; }
        public int EnemiesRemaining { get; private set; }
        public int SpawnsRemaining  { get; private set; }
        public bool IsBossWave => CurrentWave % Instance.bossWaves == 0;

        public static OLD_WaveManager Instance { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(OLD_WaveManager)} singleton.");
                Destroy(Instance);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentWave = 0;
            CurrentEnemies = 0;
            EnemiesRemaining = 0;
            SpawnsRemaining = 0;
            timeUntilNextSpawn = startDelay;
            IsActive = scene.name != "Main Menu" && scene.name != "_MAIN";

            spawnPoints.Clear();
            bossSpawnPoints.Clear();
            foreach(SpawnPoint point in FindObjectsOfType<SpawnPoint>()) {
                if(point.enemy) {
                    spawnPoints.Add(point);
                }
                if(point.boss) {
                    bossSpawnPoints.Add(point);
                }
            }
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            if(!IsActive)
                return;

            // New waves are blocked when escape or nuke are active.
            bool newWaveBlocked = false; // TODO.
            if(!newWaveBlocked && CurrentWave > 0 && EnemiesRemaining == 0) {
                //if(GameFlow.Instance.waveCompleteCoroutine == null) { 
                //    GameFlow.Instance.OnWaveComplete(CurrentWave);
                //}
            }

            if(SpawnsRemaining == 0 || CurrentEnemies >= maxEnemies)
                return;

            timeUntilNextSpawn -= Time.deltaTime;
            if(timeUntilNextSpawn > 0.0f)
                return;

            SpawnEnemy();
            timeUntilNextSpawn = 1.0f; // TODO.
        }

        public void NewWave(int wave)
        {
            CurrentWave = wave;
            CalculateWeights(spawnWeights, spawns);
            CalculateWeights(bossSpawnWeights, bossSpawns);
            if(IsBossWave) {
                EnemiesRemaining = 1;
                SpawnsRemaining = 1;
                SpawnBoss();
            } else {
                EnemiesRemaining = baseEnemies + (addEnemies * (wave - 1));
                SpawnsRemaining = EnemiesRemaining;
            }
        }

        private void SpawnEnemy()
        {
            if(!GetRandomSpawnEnemy(spawnWeights, spawns, out SpawnType enemy)) {
                Debug.LogError("Could not find a valid enemy to spawn.");
                return;
            }
            if(!FindRandomSpawnPoint(false, out SpawnPoint point)) {
                point = spawnPoints[0];
            }

            Vector3 spawnPos = point.transform.position;
            GameObject go = PoolManager.Retrieve(enemy.prefab, spawnPos).gameObject;
            go.transform.position = spawnPos;
            EnemySpawned();
        }

        private void SpawnBoss()
        {
            if(!GetRandomSpawnEnemy(bossSpawnWeights, bossSpawns, out SpawnType boss)) {
                Debug.LogError("Could not find a valid boss to spawn.");
                return;
            }
            if(!FindRandomSpawnPoint(true, out SpawnPoint point)) {
                point = bossSpawnPoints[0];
            }

            Vector3 spawnPos = point.transform.position;
            GameObject go = PoolManager.Retrieve(boss.prefab, spawnPos).gameObject;
            go.transform.position = spawnPos;
            GameUI.Instance.AttachedBoss = go.GetComponent<Entity>();
            EnemySpawned();
        }

        private bool FindRandomSpawnPoint(bool boss, out SpawnPoint point)
        {
            point = null;
            Camera mainCamera = Camera.main;
            //if(mainCamera == null || Game.Instance.Player == null)
            //    return false;

            tempSpawnPoints.Clear();
            //Transform playerTrans = Game.Instance.Player.transform;
            List<SpawnPoint> collection = boss ? bossSpawnPoints : spawnPoints;
            foreach(SpawnPoint spawnPoint in collection) {
                //if(Vector3.Distance(playerTrans.position, spawnPoint.transform.position) < 100.0f)
                //    continue;

                Vector3 viewportPoint = mainCamera.WorldToViewportPoint(spawnPoint.transform.position);
                if(viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f) {
                    tempSpawnPoints.Add(spawnPoint);
                }
            }
            if(tempSpawnPoints.Count == 0)
                return false;

            point = tempSpawnPoints[Random.Range(0, tempSpawnPoints.Count)];
            return true;
        }

        public void EnemySpawned()
        {
            SpawnsRemaining--;
            CurrentEnemies++;
        }

        public void EnemyKilled()
        {
            EnemiesRemaining--;
            CurrentEnemies--;
        }

        private void CalculateWeights(List<float> weights, IEnumerable<SpawnType> collection)
        {
            weights.Clear();
            foreach(SpawnType spawn in collection) {
                float weight = spawn.spawnWeight;
                if(!spawn.enabled || CurrentWave <= spawn.minWave) {
                    weight = 0.0f;
                }

                weights.Add(weight);
            }
        }

        private bool GetRandomSpawnEnemy(List<float> weights, SpawnType[] spawns, out SpawnType entity)
        {
            entity = null;
            int index = GetRandomWeightedIndex(weights);
            if(index == -1)
                return false;

            entity = spawns[index];
            return true;
        }

        private int GetRandomWeightedIndex(List<float> weights)
        {
            if(weights == null || weights.Count == 0)
                return -1;

            float w;
            float t = 0.0f;
            int i;
            for(i = 0; i < weights.Count; i++) {
                w = weights[i];

                if(float.IsPositiveInfinity(w))
                    return i;

                if(w >= 0f && !float.IsNaN(w)) {
                    t += weights[i];
                }
            }

            float r = Random.value;
            float s = 0.0f;
            for(i = 0; i < weights.Count; i++) {
                w = weights[i];
                if(float.IsNaN(w) || w <= 0.0f)
                    continue;

                s += w / t;
                if(s >= r)
                    return i;
            }

            return -1;
        }

        [System.Serializable]
        public class SpawnType : IArrayElementTitle
        {
            public GameObject prefab;
            public bool enabled      = true;
            public float spawnWeight = 100.0f;
            public int minWave       = 0;

            string IArrayElementTitle.Name => prefab == null ? "NULL" : prefab.name;
        }
    }

}
