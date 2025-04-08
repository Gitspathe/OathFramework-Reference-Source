using Cysharp.Threading.Tasks;
using OathFramework.Networking;
using OathFramework.Utility;
using Unity.Netcode;

namespace OathFramework.Core
{
    public class StandardSceneScript : SceneScript, ISceneLoadEventCompleted
    {
        private bool loaded;
        
        public override GeoType GeoType => GeoType.Standard;
        
        uint ILockableOrderedListElement.Order => 200;

        protected override void Awake()
        {
            base.Awake();
            NetGame.Callbacks.Register((ISceneLoadEventCompleted)this);
        }

        private void OnDestroy()
        {
            NetGame.Callbacks.Unregister((ISceneLoadEventCompleted)this);
        }
        
        protected override UniTask IntegrateSelf()
        {
            return UniTask.CompletedTask;
        }

        protected override async UniTask WaitForIntegrationAllPeers()
        {
            while(!loaded) {
                // Standard scene - NetworkManager handles this with no extra logic needed.
                await UniTask.Yield();
            }
        }

        void ISceneLoadEventCompleted.OnSceneLoadEventCompleted(SceneEvent sceneEvent)
        {
            loaded = true;
        }
    }
}
