using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace OathFramework.Persistence
{

    public class EditorScenePersistenceDB : MonoBehaviour
    {
        [LabelText("$EditorText")]
        [ReadOnly] public List<Node> nodes = new();

        // ReSharper disable once UnusedMember.Local
        private string EditorText => $"tracking {nodes.Count} objects";
        
        [Button("Assign IDs")]
        private void AssignIDs()
        {
            HashSet<string> takenIDs = new();
            PersistentObject[] objs  = FindObjectsOfType<PersistentObject>(true);
            int removedNonExistent = 0, removedOld = 0, addedNew = 0;
            
            // Remove old objects which no longer use per-scene persistence.
            foreach(PersistentObject obj in objs) {
                if(obj.gameObject.scene != gameObject.scene || obj.Type != PersistentObject.ObjectType.Scene) {
                    Remove(obj);
                    removedNonExistent++;
                }
            }

            // Remove old objects which no longer exist.
            HashSet<Node> toRemove = new();
            foreach(Node node in nodes) {
                if(objs.Contains(node.Obj))
                    continue;

                toRemove.Add(node);
                removedOld++;
            }
            foreach(Node node in toRemove) {
                nodes.Remove(node);
            }
            
            // Add all taken IDs.
            foreach(Node node in nodes) {
                takenIDs.Add(node.ID);
            }

            // Register new objects.
            foreach(PersistentObject obj in objs) {
                if(obj.gameObject.scene != gameObject.scene || obj.Type != PersistentObject.ObjectType.Scene || ContainsObject(obj))
                    continue;

                obj.SpawnID = GenerateID(obj, takenIDs);
                Add(obj);
                takenIDs.Add(obj.SpawnID);
                addedNew++;
#if UNITY_EDITOR
                EditorUtility.SetDirty(obj);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
            }
            
            Debug.Log($"Executed automatic ID assigment. Removed or skipped {removedNonExistent} (not scene serialized), " +
                      $"removed {removedOld} (no longer existing), added {addedNew} new objects.");
        }
        
        [Button("Clear IDs")]
        private void ClearIDs()
        {
            nodes.Clear();
        }

        private string GenerateID(PersistentObject obj, HashSet<string> takenIDs)
        {
            string id = FixID(obj.SpawnID);
            if(!string.IsNullOrEmpty(id) && !ContainsID(id, takenIDs))
                return obj.SpawnID;

            int numTries = 0;
            id           = FixID(obj.name);
            while(ContainsID(id, takenIDs)) {
                string s = UniqueID.Generate().ToString();
                id       = $"{FixID(obj.name)}_{s}";
                numTries++;
                if(numTries < 128)
                    continue;

                Debug.LogError("Failed to generate unique ID for scene object.");
                return "";
            }
            return id;
        }

        private static string FixID(string id)
        {
            return id.ToLower().Replace(' ', '_');
        }

        private bool ContainsID(string id, HashSet<string> takenIDs)
        {
            if(takenIDs.Contains(id))
                return true;
            
            foreach(Node node in nodes) {
                if(node.ID == id) {
                    return true;
                }
            }
            return false;
        }
        
        private bool ContainsObject(PersistentObject obj)
        {
            foreach(Node node in nodes) {
                if(node.Obj == obj) {
                    return true;
                }
            }
            return false;
        }

        private void Add(PersistentObject obj)
        {
            foreach(Node node in nodes) {
                if(node.Obj == obj) {
                    return;
                }
            }
            nodes.Add(new Node(obj.SpawnID, obj));
        }

        private void Remove(PersistentObject obj)
        {
            for(int i = 0; i < nodes.Count; i++) {
                if(nodes[i].Obj == obj) {
                    nodes.RemoveAt(i);
                    return;
                }
            }
        }
    }

    [Serializable]
    public class Node
    {
        public string ID;
        public PersistentObject Obj;
        
        public Node() { }

        public Node(string id, PersistentObject obj)
        {
            ID  = id;
            Obj = obj;
        }
    }

}
