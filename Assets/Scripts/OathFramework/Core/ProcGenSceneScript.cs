using Cysharp.Threading.Tasks;
using OathFramework.Networking;
using OathFramework.ProcGen;
using OathFramework.Utility;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.Core
{
    public class ProcGenSceneScript : SceneScript
    {
        [SerializeField] private Map map;
        [SerializeField] private MapConfig testConf;
        
        private bool sceneLoaded;
        
        public override GeoType GeoType => GeoType.ProcGen;
        
        protected override async UniTask IntegrateSelf()
        {
            if(NetGame.IsServer) {
                NetGame.Instance.CreateNetScene(testConf, FRandom.Cache.Int());
            }
            while(NetScene == null) {
                await UniTask.Yield();
            }
            ushort mapConfID = NetScene.MapConfig;
            if(!ProcGenManager.TryGet(mapConfID, out MapConfig conf)) {
                Debug.LogError($"Failed to get {nameof(MapConfig)} for ID {mapConfID}");
                return;
            }
            await map.Initialize(new Stopwatch(), conf, NetScene.MapSeed, destroyCancellationToken);
            NetScene.OnSelfIntegrated();
        }
        
        protected override async UniTask WaitForIntegrationAllPeers()
        {
            while(NetScene == null) {
                await UniTask.Yield();
            }
            await NetScene.WaitForEveryone();
        }
    }
}
