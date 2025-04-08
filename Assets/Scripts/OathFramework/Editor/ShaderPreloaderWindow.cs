using OathFramework.Settings;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace OathFramework.Editor
{
    public class ShaderPreloaderWindow : EditorWindow
    {
        private ScrollView scrollView;
        private List<MaterialsNode> allNodes = new();
        private TwoPaneSplitView splitView;
        private VisualElement rightPane;
        private VisualElement splitViewSpacer;
        private MaterialDB selectedDB;
        private int selectedSceneNodeIndex;

        [MenuItem("Window/Oath/ShaderPreloader")]
        public static void ShowMyEditor()
        {
            EditorWindow wnd = GetWindow<ShaderPreloaderWindow>();
            wnd.titleContent = new GUIContent("Shader Preloader");
            wnd.minSize      = new Vector2(450, 200);
            wnd.maxSize      = new Vector2(1920, 720);
        }

        public void CreateGUI()
        {
            scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);
            ReloadContent();
        }

        private void ReloadContent()
        {
            string[]         allObjectGuids = AssetDatabase.FindAssets("t:MaterialDB");
            List<MaterialDB> allObjects     = new();
            foreach(string guid in allObjectGuids) {
                allObjects.Add(AssetDatabase.LoadAssetAtPath<MaterialDB>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            selectedDB = allObjects.Count == 0 ? null : allObjects[0];
            
            scrollView.Clear();
            if(allObjects.Count == 0) {
                ShowEmpty(scrollView);
                return;
            }
            
            ShowDBDropDown(scrollView, allObjects);
        }

        private void ShowEmpty(ScrollView view)
        {
            Button btn = new(() => {
                string path = "Assets/MaterialDB.asset";
                AssetDatabase.CreateAsset(CreateInstance(typeof(MaterialDB)), path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ReloadContent();
            }) { text = "Create MaterialDB Asset" };
            
            view.Add(new Label("There are no MaterialDB assets."));
            view.Add(btn);
        }
        
        private void ShowDBDropDown(ScrollView view, List<MaterialDB> matDBs)
        {
            VisualElement matParent = new();
            if(selectedDB != null) {
                ShowMaterialDB(matParent, selectedDB);
            }
            view.Add(matParent);
        }

        private void ShowMaterialDB(VisualElement element, MaterialDB matDB)
        {
            allNodes.Clear();
            allNodes.AddRange(matDB.materialNodes);
            element.Clear();

            Button processScene = new(() => { 
                ShaderPreloaderEditorUtil.ProcessScene(SceneManager.GetActiveScene(), matDB);
                EditorUtility.SetDirty(matDB);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ReloadContent();
            }) { text = "Process current scene" };
            Button processAll = new(() => { 
                ShaderPreloaderEditorUtil.ProcessAllScenes(matDB);
                EditorUtility.SetDirty(matDB);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ReloadContent(); 
            }) { text = "Process all scenes" };
            
            element.Add(new VisualElement { style = { height = 16 } });
            element.Add(processScene);
            element.Add(processAll);
            ShowMaterialSplitView();
        }

        private void ShowMaterialSplitView()
        {
            if(splitView != null) {
                rootVisualElement.Remove(splitView);
                rootVisualElement.Remove(splitViewSpacer);
            }
            splitView       = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            splitViewSpacer = new VisualElement { style = { height = 10 } };
            rootVisualElement.Add(splitViewSpacer);
            rootVisualElement.Add(splitView);
            
            ListView leftPane = new();
            splitView.Add(leftPane);
            rightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            splitView.Add(rightPane);
            
            leftPane.makeItem      = () => new Label();
            leftPane.bindItem      = (item, index) => { (item as Label).text = allNodes[index].scenePath; };
            leftPane.itemsSource   = allNodes;
            leftPane.selectedIndex = selectedSceneNodeIndex;
            leftPane.selectionChanged += OnSceneNodeSelectionChange;
            leftPane.selectionChanged += _ => { selectedSceneNodeIndex = leftPane.selectedIndex; };
        }

        private void OnSceneNodeSelectionChange(IEnumerable<object> obj)
        {
            rightPane.Clear();
            
            IEnumerator<object> enumerator = obj.GetEnumerator();
            if(!enumerator.MoveNext())
                return;

            MaterialsNode selectedNode = enumerator.Current as MaterialsNode;
            if(selectedNode == null)
                return;

            if(selectedNode.materials.Count == 0) {
                rightPane.Add(new Label("No materials tracked."));
                return;
            }
            
            foreach(Material mat in selectedNode.materials) {
                try {
                    rightPane.Add(new Label(mat?.name));
                } catch(NullReferenceException) { /* ignored */ }
            }
            rightPane.Add(new VisualElement { style = { height = 16 } });
            rightPane.Add(new Label($"Total: {selectedNode.materials.Count}"));
        }
    }
}
