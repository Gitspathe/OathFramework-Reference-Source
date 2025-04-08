using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.EntitySystem.Players;
using OathFramework.Networking;
using OathFramework.UI;
using OathFramework.Utility;
using System;
using Unity.Netcode;
using UnityEngine;

namespace GameCode.MagitechRequiem
{
    public class MagitechGame : MonoBehaviour, IGame, ISceneLoadEventCompleted
    {
        uint ILockableOrderedListElement.Order => 100;

        void IGame.Initialize()
        {
            NetGame.Callbacks.Register((ISceneLoadEventCompleted)this);
        }
        
        void ISceneLoadEventCompleted.OnSceneLoadEventCompleted(SceneEvent sceneEvent)
        {
            _ = IntegrateTask();
        }

        private async UniTask IntegrateTask()
        {
            await SceneScript.Main.IntegrateSelfTask();
            await SpawnPlayersTask();
        }

        private async UniTask SpawnPlayersTask()
        {
            await SceneScript.Main.WaitForIntegrationAllPeersTask();
            if(NetGame.IsServer && SceneScript.Main.Type == SceneType.GameLevel) {
                SpawnPlayers();
            }
            _ = LoadingUIScript.Hide();
        }

        private void SpawnPlayers()
        {
            foreach(NetClient player in PlayerManager.Players) {
                SpawnPlayer(player);
            }
        }

        private void SpawnPlayer(NetClient player)
        {
            try {
                NetGame.Instance.CreateClientGameObject(player);
            } catch(Exception e) {
                Debug.LogError($"Error occured when spawning player: {e.Message}");
                NetGame.Instance.KickPlayer(player, "Error occured.");
            }
        }
    }
}
