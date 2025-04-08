using UnityEngine;

namespace OathFramework.Utility
{
    public class IconScene : MonoBehaviour
    {
        [field: SerializeField] public bool Skip           { get; private set; } = false;

        [field: Space(10)]
        
        [field: SerializeField] public string Path         { get; private set; }
        [field: SerializeField] public bool UseCutoutColor { get; private set; } = true;
        [field: SerializeField] public bool Crop           { get; private set; } = true;
        [field: SerializeField] public int Smoothing       { get; private set; } = 3;

        public void Capture(string path, Color cutoutColor)
        {
            if(string.IsNullOrEmpty(path) || path == "/" || !path.Contains("Assets/") || path.Contains("~") || path.Contains(".")) {
                Debug.LogError($"Path '{path}' is invalid. Skipping.");
                return;
            }
            zzTransparencyCapture.captureScreenshot(path + ".png", Crop, UseCutoutColor ? cutoutColor : null, Smoothing);
        }
    }
}
