using Cysharp.Threading.Tasks;
using OathFramework.Networking;
using OathFramework.Persistence;
using OathFramework.UI;
using UnityEngine;

namespace OathFramework.Core
{ 

    public abstract class SceneScript : MonoBehaviour
    {
        [SerializeField] private string sceneName;
        [SerializeField] private SceneType type;

        public abstract GeoType GeoType { get; }
        
        public string SceneName            => sceneName;
        public SceneType Type              => type;
        public PersistentScene Persistence => persistentBehaviour == null ? null : persistentBehaviour.Scene;
        
        protected NetScene NetScene;

        private PersistentSceneBehaviour persistentBehaviour;
        
        public static SceneScript Main { get; private set; }

        protected virtual void Awake()
        {
            Main = this;
            persistentBehaviour = GetComponent<PersistentSceneBehaviour>();
        }
        
        public void NetSceneSpawned(NetScene scene)
        {
            NetScene = scene;
        }

        public async UniTask IntegrateSelfTask()
        {
            await IntegrateSelf();
            NetGame.Instance.OnClientIntegrated(NetClient.Self);
        }
        
        public async UniTask WaitForIntegrationAllPeersTask()
        {
            LoadingUIScript.SetProgress(NetGame.Msg.WaitingForOthersStr, 1.0f);
            await WaitForIntegrationAllPeers();
            if(Game.State != GameState.Preload) {
                Game.SetState(type == SceneType.GameLevel ? GameState.InGame : GameState.Lobby);
            }
            NetGame.Instance.OnAllPeersIntegrated();
        }

        protected abstract UniTask IntegrateSelf();
        protected abstract UniTask WaitForIntegrationAllPeers();
    }

    public enum SceneType : byte
    {
        Lobby     = 0,
        Map       = 1,
        GameLevel = 2
    }

    public enum GeoType : byte
    {
        Standard = 0,
        ProcGen  = 1
    }

}
