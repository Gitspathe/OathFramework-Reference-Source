#if UNITY_EDITOR
using UnityEngine;

namespace OathFramework.Utility
{ 
    public class EditorHelperScript : MonoBehaviour
    {
        [SerializeField] private int framerateLimitEditor = 60;

        private void Awake()
        {
            if(framerateLimitEditor > 0) {
                Debug.Log($"Editor target framerate set to {framerateLimitEditor}.");
                Application.targetFrameRate = framerateLimitEditor;
            }
        }
    }
}
#endif
