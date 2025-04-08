using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    [Serializable]
    public class RagdollTree
    {
        [field: SerializeField] public Transform[] Transforms { get; private set; }
        
        public void Initialize(Transform root, List<TransformMapping.TransformData> map, bool @override = false)
        {
            if(Application.isPlaying && (Transforms == null || Transforms.Length == 0)) {
                Debug.LogWarning($"Transforms on {root.name} are being generated at runtime. Generate them in the editor to improve performance.");
            }
            if(!@override && Transforms != null && Transforms.Length > 0)
                return;

            Transforms                             = new Transform[map.Count];
            Dictionary<string, Transform> pathDict = new(map.Count);
            BuildPathDictionary(root, pathDict);
            for(int i = 0; i < Transforms.Length; i++) {
                if(map[i].Excluded)
                    continue;

                string path = map[i].RelativePathFromRoot;
                if(!pathDict.TryGetValue(path, out Transform t)) {
                    Debug.LogError($"Failed to find transform at {path}.");
                    continue;
                }
                Transforms[i] = t;
            }
        }

        public void Clear()
        {
            Transforms = Array.Empty<Transform>();
        }
        
        private void BuildPathDictionary(Transform root, Dictionary<string, Transform> pathDict)
        {
            BuildPathDictionaryRecursive(root, root, pathDict);
        }

        private void BuildPathDictionaryRecursive(Transform root, Transform current, Dictionary<string, Transform> pathDict)
        {
            string relativePath = TransformUtil.GetRelativePath(current, root);
            pathDict.TryAdd(relativePath, current);
            foreach(Transform child in current) {
                BuildPathDictionaryRecursive(root, child, pathDict);
            }
        }
    }
}
