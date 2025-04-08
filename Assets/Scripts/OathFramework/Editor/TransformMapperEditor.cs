using OathFramework.Effects;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using OathFramework.Utility;

namespace OathFramework.Editor
{
    public class TransformMapperEditor : EditorWindow
    {
        private Transform sourceRoot;
        private Transform targetRoot;
        private TreeViewState treeViewState;
        private TransformTreeView transformTreeView;
        private bool isEditing;
        
        private TransformMapping mappingAsset;
        private TransformMapping MappingAsset {
            get => mappingAsset;
            set {
                if(mappingAsset == value)
                    return;

                mappingAsset = value;
                UpdateMode();
                UpdateTrees();
            }
        }

        public bool MappingAssetFilled => MappingAsset != null 
                                          && MappingAsset.SourceTransforms.Count > 0 
                                          && MappingAsset.TargetTransforms.Count > 0;

        [MenuItem("Window/Oath/Transform Mapper")]
        public static void ShowWindow()
        {
            GetWindow<TransformMapperEditor>("Transform Mapper");
        }

        private void OnEnable()
        {
            treeViewState ??= new TreeViewState();
            UpdateMode();
        }

        private void OnValidate()
        {
            UpdateMode();
        }

        private void UpdateMode()
        {
            if(MappingAssetFilled) {
                transformTreeView = new TransformTreeView(this, treeViewState, MappingAsset);
                isEditing         = true;
                transformTreeView.ExpandAll();
            } else {
                isEditing = false;
            }
        }

        private void UpdateTrees()
        {
            if(sourceRoot == null || targetRoot == null)
                return;
            
            UpdateTree(sourceRoot, mappingAsset.SourceTransforms);
            UpdateTree(targetRoot, mappingAsset.TargetTransforms);
        }

        private void UpdateTree(Transform transform, List<TransformMapping.TransformData> map)
        {
            RagdollBase b = transform.root.GetComponentInChildren<RagdollBase>(true);
            if(b != null) {
                b.Tree.Initialize(transform, map, true);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Transform Mapping Editor", EditorStyles.boldLabel);

            MappingAsset = (TransformMapping)EditorGUILayout.ObjectField("Mapping Asset", MappingAsset, typeof(TransformMapping), false);
            if(MappingAsset == null) {
                EditorGUILayout.HelpBox("Assign a TransformMappingSO asset to create/edit a mapping.", MessageType.Info);
                return;
            }

            sourceRoot = (Transform)EditorGUILayout.ObjectField("Source Transform Root", sourceRoot, typeof(Transform), true);
            targetRoot = (Transform)EditorGUILayout.ObjectField("Target Transform Root", targetRoot, typeof(Transform), true);
            if(!isEditing) {
                if(!GUILayout.Button("Create Mapping"))
                    return;

                GenerateTransformMapping();
                isEditing = true;
                return;
            }
            
            GUILayout.Label("Edit Transform Mapping", EditorStyles.boldLabel);
            if(GUILayout.Button("Regenerate Mapping")) {
                GenerateTransformMapping();
            }
            if(GUILayout.Button("Erase Mapping")) {
                MappingAsset.SourceTransforms.Clear();
                MappingAsset.TargetTransforms.Clear();
                EditorUtility.SetDirty(MappingAsset);
                isEditing = false;
            }

            GUILayout.Space(10);
            GUILayout.Label("Transform Hierarchy", EditorStyles.boldLabel);
            if(transformTreeView != null) {
                Rect treeViewRect = GUILayoutUtility.GetRect(0, 1000, 0, transformTreeView.totalHeight);
                transformTreeView.OnGUI(treeViewRect);
            }
        }

        private void GenerateTransformMapping()
        {
            if(sourceRoot == null || targetRoot == null) {
                Debug.LogError("Source or target root is not selected.");
                return;
            }

            MappingAsset.SourceTransforms.Clear();
            MappingAsset.TargetTransforms.Clear();
            Dictionary<Transform, int> sourceMap = new();
            Dictionary<Transform, int> targetMap = new();
            int currentID                        = 0;
            
            CacheHierarchyWithIDs(sourceRoot, targetRoot, sourceMap, targetMap, ref currentID);
            foreach(KeyValuePair<Transform, int> entry in sourceMap) {
                MappingAsset.SourceTransforms.Add(new TransformMapping.TransformData {
                    RelativePathFromRoot = TransformUtil.GetRelativePath(entry.Key, sourceRoot),
                    UniqueID             = entry.Value
                });
            }
            foreach(KeyValuePair<Transform,int> entry in targetMap) {
                MappingAsset.TargetTransforms.Add(new TransformMapping.TransformData {
                    RelativePathFromRoot = TransformUtil.GetRelativePath(entry.Key, targetRoot),
                    UniqueID             = entry.Value
                });
            }

            EditorUtility.SetDirty(MappingAsset);
            AssetDatabase.SaveAssets();
            transformTreeView = new TransformTreeView(this, treeViewState, MappingAsset);
            transformTreeView.ExpandAll();
            UpdateTrees();
        }

        private void CacheHierarchyWithIDs(
            Transform sourceRoot, 
            Transform targetRoot, 
            Dictionary<Transform, int> sourceMap, 
            Dictionary<Transform, int> targetMap, 
            ref int currentID)
        {
            if(sourceRoot.name != targetRoot.name)
                return;

            sourceMap[sourceRoot] = currentID;
            targetMap[targetRoot] = currentID++;
            foreach(Transform child in sourceRoot) {
                Transform sibling = TransformUtil.GetSiblingTransform(targetRoot, child.name);
                if(sibling == null)
                    continue;
                
                CacheHierarchyWithIDs(child, sibling, sourceMap, targetMap, ref currentID);
            }
        }

        private class TransformTreeView : TreeView
        {
            private readonly TransformMapperEditor editor;
            private readonly TransformMapping mappingAsset;

            public TransformTreeView(TransformMapperEditor editor, TreeViewState state, TransformMapping mappingAsset) : base(state)
            {
                this.editor       = editor;
                this.mappingAsset = mappingAsset;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new() {
                    id          = 0,
                    depth       = -1,
                    displayName = "Root"
                };
                Dictionary<string, TreeViewItem> pathToItemMap = new() { { string.Empty, root } };
                int id = 1;
                
                foreach(TransformMapping.TransformData data in mappingAsset.SourceTransforms) {
                    string[] parts      = data.RelativePathFromRoot.Split('/');
                    string currentPath  = string.Empty;
                    TreeViewItem parent = root;
                    for(int depth = 0; depth < parts.Length; depth++) {
                        string part = parts[depth];
                        currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                        if(!pathToItemMap.TryGetValue(currentPath, out TreeViewItem currentItem)) {
                            currentItem = new TreeViewItem {
                                id          = id++,
                                depth       = depth,
                                displayName = part
                            };
                            parent.AddChild(currentItem);
                            pathToItemMap[currentPath] = currentItem;
                        }
                        parent = currentItem;
                    }
                }
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if(args.item.id == 0 || mappingAsset.SourceTransforms.Count == 0)
                    return;
                
                TransformMapping.TransformData dataSource = mappingAsset.SourceTransforms[args.item.id];
                TransformMapping.TransformData dataTarget = mappingAsset.TargetTransforms[args.item.id];
                GUI.color                                 = dataSource.Excluded ? Color.gray : Color.white;
                base.RowGUI(args);

                GUI.color          = dataSource.Excluded ? Color.green : Color.red;
                Rect buttonRect    = new(args.rowRect.xMax - 30, args.rowRect.y, 30, args.rowRect.height);
                string buttonLabel = dataSource.Excluded ? "+" : "-";
                if(GUI.Button(buttonRect, buttonLabel)) {
                    dataSource.Excluded = !dataSource.Excluded;
                    dataTarget.Excluded = !dataTarget.Excluded;
                    EditorUtility.SetDirty(mappingAsset);
                    Reload();
                    editor.UpdateTrees();
                }
                GUI.color = Color.white;
            }
        }
    }
}
