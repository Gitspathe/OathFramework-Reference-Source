using Cysharp.Threading.Tasks;
using OathFramework.Data.EntityStates;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using OathFramework.Networking;
using OathFramework.Utility;
using QFSW.QC;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OathFramework.DevConsole
{
    public class UtilityCommands : MonoBehaviour
    {
        [Command("fps-counter", "Toggles the FPS counter on or off.", MonoTargetType.Singleton)]
        private void ToggleFPSCounter()
        {
            FramerateCounter.Instance.Toggle();
        }

        [Command("godmode", "Toggles invincibility.", MonoTargetType.Singleton)]
        private void ToggleGodMode()
        {
            if(NetClient.Self == null || NetClient.Self.PlayerController == null)
                return;

#if !DEBUG
            if(NetGame.GameType == GameType.Multiplayer) {
                Debug.Log("Cannot enable God Mode in Multiplayer.");
                return;
            }
#endif
            Entity e    = NetClient.Self.PlayerController.Entity;
            bool toggle = !e.States.HasState(GodModeState.Instance);
            ushort val  = toggle ? (ushort)1 : (ushort)0;
            e.States.SetState(new EntityState(GodModeState.Instance, val), applyStats: false);
            Debug.Log(toggle ? "Toggled GodMode ON" : "Toggled GodMode OFF");
        }

        [Command("list-scenes", "Lists all scenes.", MonoTargetType.Singleton)]
        private void ListScenes()
        {
            int i = 0;
            List<string> scenes = new();
            while(true) {
                string s = SceneUtility.GetScenePathByBuildIndex(i++);
                if(string.IsNullOrEmpty(s))
                    break;
                
                string[] split = s.Split('/');
                string line    = "  " + (i - 1) + ") ";
                line += split.Length <= 1 ? s : split[split.Length - 1];
                scenes.Add(line.Replace(".unity", ""));
            }

            StringBuilder sb = new();
            foreach(string s in scenes) {
                sb.AppendLine(s);
            }
            Debug.Log($"Scenes:\n{sb}");
        }
        
        [Command("load-scene", "Loads a scene.", MonoTargetType.Singleton)]
        private async UniTask LoadScene(string scene)
        {
            int index = SceneUtility.GetBuildIndexByScenePath(scene);
            if(index == -1) {
                Debug.LogError($"Scene '{scene}' was not found.");
                return;
            }
            if(scene == "Main Menu" || scene == "_MAIN" || scene == "_PRELOAD") {
                Debug.LogError("Cannot load manager scenes. Quit the game instead if you are stuck.");
                return;
            }
#if !DEBUG
            if(NetGame.GameType == GameType.Multiplayer) {
                Debug.Log("Cannot change scene in Multiplayer.");
                return;
            }
#endif
            
            if(!NetworkManager.Singleton.IsListening) {
                await NetGame.Instance.StartSinglePlayerHost();
            }
            NetGame.Instance.LoadScene(scene);
        }

        [Command("die", "Death moment", MonoTargetType.Singleton)]
        private void Die()
        {
            if(NetClient.Self == null || NetClient.Self.PlayerController == null)
                return;
            
            Entity e = NetClient.Self.PlayerController.Entity;
            e.DieCommand();
        }
    }
}
