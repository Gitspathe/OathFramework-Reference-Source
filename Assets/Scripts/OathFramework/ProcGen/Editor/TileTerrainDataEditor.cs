using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scene = UnityEngine.SceneManagement.Scene;

namespace OathFramework.ProcGen.Editor
{
    [InitializeOnLoad]
    public static class TileTerrainPrefabHandler
    {
        static TileTerrainPrefabHandler()
        {
            PrefabStage.prefabStageOpened  += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnPrefabStageOpened(PrefabStage stage)
        {
            TileTerrainData[] tileTerrainData = stage.prefabContentsRoot.GetComponentsInChildren<TileTerrainData>(true);
            if(tileTerrainData == null || tileTerrainData.Length == 0)
                return;

            foreach(TileTerrainData t in tileTerrainData) {
                if(t.Terrain == null)
                    continue;
                
                if(string.IsNullOrEmpty(t.DataPath)) {
                    string prefabPath      = stage.assetPath;
                    string prefabDirectory = Path.GetDirectoryName(prefabPath);
                    if(string.IsNullOrEmpty(prefabDirectory)) {
                        Debug.LogError($"No prefab directory found for {prefabPath}");
                        return;
                    }
                    string terrainDataName 
                        = $"TerrainData{Path.DirectorySeparatorChar}{stage.prefabContentsRoot.name}_TerrainData_({t.OffsetX}, {t.OffsetY}).asset";
                    
                    t.DataPath = Path.Combine(prefabDirectory, terrainDataName);
                    Debug.Log($"Tile data path set to {t.DataPath}");
                }
                LoadTerrainData(t);
            }
        }

        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            TileTerrainData[] tileTerrainData = stage.prefabContentsRoot.GetComponentsInChildren<TileTerrainData>(true);
            if(tileTerrainData == null || tileTerrainData.Length == 0)
                return;

            foreach(TileTerrainData t in tileTerrainData) {
                if(t.Terrain == null)
                    continue;
                
                SaveTerrainData(t);
            }
        }
        
        private static void OnSceneSaving(Scene scene, string path)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if(stage == null)
                return;

            TileTerrainData[] tileTerrainData = stage.prefabContentsRoot.GetComponentsInChildren<TileTerrainData>(true);
            if(tileTerrainData == null || tileTerrainData.Length == 0)
                return;
            
            foreach(TileTerrainData t in tileTerrainData) {
                if(t.Terrain == null)
                    continue;
                
                SaveTerrainData(t);
            }
        }

        private static void LoadTerrainData(TileTerrainData tileTerrainData)
        {
            if(string.IsNullOrEmpty(tileTerrainData.DataPath)) 
                return;
            
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(tileTerrainData.DataPath);
            if(terrainData == null)
                return;

            tileTerrainData.Terrain.terrainData = terrainData;
            Debug.Log($"Loaded terrain data from {tileTerrainData.DataPath}");
        }

        private static void SaveTerrainData(TileTerrainData tileTerrainData)
        {
            if(string.IsNullOrEmpty(tileTerrainData.DataPath)) 
                return;
            
            TerrainData existingData = AssetDatabase.LoadAssetAtPath<TerrainData>(tileTerrainData.DataPath);
            if(existingData == null) {
                AssetDatabase.CreateAsset(Object.Instantiate(tileTerrainData.Terrain.terrainData), tileTerrainData.DataPath);
            } else {
                EditorUtility.SetDirty(tileTerrainData.Terrain.terrainData);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Saved terrain data to {tileTerrainData.DataPath}");
        }
    }
    
    [CustomEditor(typeof(TileTerrainData))]
    public class TileTerrainDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}
