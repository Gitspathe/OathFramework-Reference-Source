using Cysharp.Threading.Tasks;
using OathFramework.Networking;
using OathFramework.Persistence;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace OathFramework.Core
{
    public class LoadSequence
    {
        public LoadSequenceEntry Current { get; private set; }
        public Stopwatch Timer           { get; private set; }
        public int TimeBudget            { get; private set; }
        
        private QList<LoadSequenceEntry> entries = new();

        public LoadSequence(Stopwatch timer = null, int? timeBudget = null)
        {
            Timer      = timer ?? new Stopwatch();
            TimeBudget = timeBudget ?? AsyncFrameBudgets.High;
        }
        
        public LoadSequence Clear()
        {
            entries.Clear();
            return this;
        }

        public LoadSequence WithScene(string sceneName, bool bypassAlreadyLoading = false)
        {
            entries.Add(new LoadSceneEntry(sceneName, bypassAlreadyLoading));
            return this;
        }

        public LoadSequence WithSnapshot(string snapshotName, bool ignoreIfLoaded = true)
        {
            entries.Add(new LoadSnapshotEntry(snapshotName, ignoreIfLoaded));
            return this;
        }

        public LoadSequence WithSnapshotScene(string sceneName)
        {
            entries.Add(new LoadSnapshotSceneEntry(sceneName));
            return this;
        }

        public LoadSequence WithPoolInstantiation(PoolCollectionType poolType)
        {
            entries.Add(new InstantiatePoolCollectionEntry(poolType));
            return this;
        }
        
        public LoadSequence WithPoolDestruction(PoolCollectionType poolType)
        {
            entries.Add(new DestroyPoolCollectionEntry(poolType));
            return this;
        }

        public async UniTask Execute()
        {
            int count = entries.Count;
            for(int i = 0; i < count; i++) {
                Current = entries.Array[i];
                await Current.Execute(Timer, TimeBudget);
            }
            Current = null;
        }
    }

    public abstract class LoadSequenceEntry
    {
        public abstract UniTask Execute(Stopwatch timer, int timeBudget);
    }

    public class LoadSceneEntry : LoadSequenceEntry
    {
        public string SceneName          { get; private set; }
        public bool BypassAlreadyLoading { get; private set; }
        
        public LoadSceneEntry(string sceneName, bool bypassAlreadyLoading = false)
        {
            SceneName            = sceneName;
            BypassAlreadyLoading = bypassAlreadyLoading;
        }

        public override async UniTask Execute(Stopwatch timer, int timeBudget)
        {
            NetworkManager m = NetworkManager.Singleton;
            if(m.IsListening && m.IsServer && !m.ShutdownInProgress) {
                await LoadSceneAsServer(timer, timeBudget);
                return;
            }
            await LoadScene(timer, timeBudget);
        }

        private async UniTask LoadSceneAsServer(Stopwatch timer, int timeBudget)
        {
            if(!NetworkManager.Singleton.IsServer)
                return;
            
            if(!BypassAlreadyLoading && NetGame.IsLoadingScene) {
                Debug.LogError("Attempted to load scene as server while a scene is already loading.");
                return;
            }
            
            NetGame.ConnectionState         = GameConnectionState.Loading;
            SceneEventProgressStatus status = NetworkManager.Singleton.SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            if(status != SceneEventProgressStatus.Started) {
                Debug.LogError($"Failed to load {SceneName} with a {nameof(SceneEventProgressStatus)}: {status}");
                NetGame.IsLoadingScene  = false;
                NetGame.ConnectionState = GameConnectionState.Ready; // ?
                return;
            }

            NetGameRpcHelper.Instance.NotifyServerStartedLoadingClientRpc();
            NetGame.IsLoadingScene = true;
            while(NetGame.IsLoadingScene) {
                await UniTask.Yield();
            }
        }

        private async UniTask LoadScene(Stopwatch timer, int timeBudget)
        {
            if(!BypassAlreadyLoading && NetGame.IsLoadingScene) {
                Debug.LogError("Attempted to load scene as server while a scene is already loading.");
                return;
            }

            NetGame.IsLoadingScene   = true;
            AsyncOperation operation = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            if(operation == null) {
                Debug.LogError($"Failed to load scene '{SceneName}");
                return;
            }
            while(!operation.isDone) {
                await UniTask.Yield();
            }
            NetGame.IsLoadingScene = false;
        }
    }

    public class LoadSnapshotEntry : LoadSequenceEntry
    {
        public string SnapshotName { get; private set; }
        public bool IgnoreIfLoaded { get; private set; }

        public LoadSnapshotEntry(string snapshotName, bool ignoreIfLoaded)
        {
            SnapshotName   = snapshotName;
            IgnoreIfLoaded = ignoreIfLoaded;
        }

        public override async UniTask Execute(Stopwatch timer, int timeBudget)
        {
            if(PersistenceManager.CurrentData != null && IgnoreIfLoaded)
                return;
            
            await PersistenceManager.LoadSnapshot(SnapshotName);
        }
    }

    public class LoadSnapshotSceneEntry : LoadSequenceEntry
    {
        public string SceneName { get; private set; }

        public LoadSnapshotSceneEntry(string sceneName)
        {
            SceneName = sceneName;
        }

        public override async UniTask Execute(Stopwatch timer, int timeBudget)
        {
            await PersistenceManager.ApplySceneData(SceneName);
        }
    }

    public class InstantiatePoolCollectionEntry : LoadSequenceEntry
    {
        public PoolCollectionType Collection { get; }
        
        public InstantiatePoolCollectionEntry(PoolCollectionType collection)
        {
            Collection = collection;
        }

        public override async UniTask Execute(Stopwatch timer, int timeBudget)
        {
            await PoolManager.InstantiatePendingAsync(timer, Collection);
        }
    }
    
    public class DestroyPoolCollectionEntry : LoadSequenceEntry
    {
        public PoolCollectionType Collection { get; }
        
        public DestroyPoolCollectionEntry(PoolCollectionType collection)
        {
            Collection = collection;
        }

        public override async UniTask Execute(Stopwatch timer, int timeBudget)
        {
            await PoolManager.DestroyAsync(timer, Collection);
        }
    }
}
