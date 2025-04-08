using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace OathFramework.Editor
{
 
    [InitializeOnLoad]
    public class BootstrapSceneHelper
    {
        // your first scene path:
        private const string FirstSceneToLoad = "Assets/Scenes/_MAIN.unity";
        
        // Editor pref save name, no need to change
        private const string ActiveEditorScene = "PreviousScenePath";
        private const string IsEditorInitialization = "EditorIntialization";
     
        // The scenes names that you want to do the editor initialization, only these scenes will work,
        // alternatively, you can do the initialization in all scenes by removing this list.
        private static List<string> validScenes = new() {
            "Main Menu",
            "test_level"
        };
        
        // The scenes names that you want to load in addition to the first scene. Loaded in the list order.
        private static List<string> extraScenesToLoad = new() { };
     
        static BootstrapSceneHelper()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch(state) {
                // remove "IsValidScene" method if you want to do the initialization in all scenes.
                case PlayModeStateChange.ExitingEditMode:
                    if(!IsValidScene(validScenes, out string sceneName))
                        return;
                    
                    EditorPrefs.SetString(ActiveEditorScene, sceneName);
                    EditorPrefs.SetBool(IsEditorInitialization, true);
                    SetStartScene(FirstSceneToLoad);
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    if(!EditorPrefs.GetBool(IsEditorInitialization))
                        return;
                    
                    LoadExtraScenes();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    EditorPrefs.SetBool(IsEditorInitialization, false);
                    break;
            }
        }

        private static void SetStartScene(string scenePath)
        {
            SceneAsset firstSceneToLoad = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if(firstSceneToLoad != null) {
                EditorSceneManager.playModeStartScene = firstSceneToLoad;
            } else {
                Debug.Log($"Could not find Scene '{scenePath}'");
            }
        }

        private static void LoadExtraScenes()
        {
            // extra scenes to load
            foreach(string scenePath in extraScenesToLoad) {
                SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
            }
            
            // the original scene loading
            //string prevScene = EditorPrefs.GetString(ActiveEditorScene);
            //SceneManager.LoadScene(prevScene, LoadSceneMode.Additive);
        }

        private static bool IsValidScene(List<string> scenesToCheck, out string sceneName)
        {
            sceneName = SceneManager.GetActiveScene().name;
            return scenesToCheck.Contains(sceneName);
        }
    }
}
