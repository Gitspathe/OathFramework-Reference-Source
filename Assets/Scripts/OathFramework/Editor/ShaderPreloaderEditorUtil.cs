using OathFramework.Settings;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OathFramework.Editor
{
    public static class ShaderPreloaderEditorUtil
    {
        public static void ProcessScene(Scene scene, MaterialDB materialDB, HashSet<Material> foundMaterials = null)
        {
            MaterialsNode node = null;
            foreach(MaterialsNode mNode in materialDB.materialNodes) {
                if(mNode.scenePath != scene.path)
                    continue;

                mNode.materials.Clear();
                node = mNode;
            }
            if(node == null) {
                node = new MaterialsNode {
                    scenePath       = scene.path, 
                    sceneBuildIndex = scene.buildIndex, 
                    materials       = new List<Material>()
                };
                materialDB.materialNodes.Add(node);
            }
            if(foundMaterials == null) {
                foundMaterials = new HashSet<Material>();
                foreach(MaterialsNode mNode in materialDB.materialNodes) {
                    foreach(Material mat in mNode.materials) {
                        foundMaterials.Add(mat);
                    }
                }
            }
            foreach(GameObject go in SceneManager.GetActiveScene().GetRootGameObjects()) {
                foreach(Component comp in go.GetComponentsInChildren(typeof(Component), true)) {
                    try {
                        HandleComponent(comp, node);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
            }
            Debug.Log($"ShaderPreloader processed '{scene.path}'. Found {node.materials.Count} materials.");
        }

        public static void ProcessAllScenes(MaterialDB materialDB)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            
            materialDB.materialNodes.Clear();
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            foreach(EditorBuildSettingsScene buildScene in buildScenes) {
                Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                ProcessScene(scene, materialDB);
            }
        }
        
        private static void TryRegisterMaterial(Material material, MaterialsNode matNode)
        {
            if(matNode.materials.Contains(material))
                return;
            
            matNode.materials.Add(material);
        }
        
        private static void HandleComponent(Component component, MaterialsNode matNode)
        {
            switch(component) {
                case Renderer renderer: {
                    foreach(Material mat in renderer.sharedMaterials) {
                        TryRegisterMaterial(mat, matNode);
                    }
                } break;
                case Terrain terrain: {
                    TryRegisterMaterial(terrain.materialTemplate, matNode);
                } break;
                case IMaterialPreloaderDataProvider provider: {
                    foreach(Material mat in provider.GetMaterials()) {
                        TryRegisterMaterial(mat, matNode);
                    }
                } break;
            }
        }
    }
}
