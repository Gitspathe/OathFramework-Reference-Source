using System.Collections;
using System.IO;
using UnityEngine;

namespace OathFramework.Utility
{
    public class IconStudio : MonoBehaviour
    {
        [field: SerializeField] public string SavePath       { get; private set; } = "Assets/IconStudio/";
        [field: SerializeField] public Color IconCutoutColor { get; private set; } = Color.white;
        [field: SerializeField] public bool ReplaceExisting  { get; private set; }
        
        private int frame;
        private bool hasRun;

        private QList<IconScene> scenes = new();
        
        private void Awake()
        {
            foreach(IconScene scene in GetComponentsInChildren<IconScene>(true)) {
                if(scene.Skip)
                    continue;
                
                scenes.Add(scene);
            }
        }

        private void Update()
        {
            frame++;
            if(frame > 10 && !hasRun) {
                hasRun = true;
                StartCoroutine(CaptureAll());
            }
        }

        private IEnumerator CaptureAll()
        {
            for(int i = 0; i < scenes.Count; i++) {
                yield return Capture(i);
            }
        }

        private IEnumerator Capture(int index)
        {
            yield return new WaitForEndOfFrame();
            for(int i = 0; i < scenes.Count; i++) {
                scenes.Array[i].gameObject.SetActive(index == i);
            }
            if(string.IsNullOrEmpty(scenes.Array[index].Path)) {
                Debug.LogError($"Null path on {scenes.Array[index].name}, skipping.");
            }
            if(File.Exists(SavePath + scenes.Array[index].Path + ".png") && !ReplaceExisting) {
                Debug.Log($"Skipping '{scenes.Array[index].Path + ".png"}'");
                yield break;
            }
            scenes.Array[index].Capture(SavePath + scenes.Array[index].Path, IconCutoutColor);
        }
    }
}
