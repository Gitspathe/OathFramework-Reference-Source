using UnityEditor;
using UnityEngine;

namespace OathFramework.Editor
{

    public class RectTransformSnapAnchors
    {
        [MenuItem("Tools/UI/Anchor Around Object")]
        [MenuItem("CONTEXT/RectTransform/Anchor Current Position")]
        private static void uGUIAnchorAroundObject()
        {
            foreach(GameObject o in Selection.gameObjects) {
                if(o == null) {
                    return;
                }

                RectTransform r = o.GetComponent<RectTransform>();

                if(r == null || r.parent == null) {
                    continue;
                }

                Undo.RecordObject(o, "SnapAnchors");
                AnchorRect(r);
            }
        }

        private static void AnchorRect(RectTransform r)
        {
            RectTransform p = r.parent.GetComponent<RectTransform>();

            Vector2 offsetMin = r.offsetMin;
            Vector2 offsetMax = r.offsetMax;
            Vector2 _anchorMin = r.anchorMin;
            Vector2 _anchorMax = r.anchorMax;

            float parent_width = p.rect.width;
            float parent_height = p.rect.height;

            Vector2 anchorMin = new Vector2(_anchorMin.x + (offsetMin.x / parent_width),
                _anchorMin.y + (offsetMin.y / parent_height));
            Vector2 anchorMax = new Vector2(_anchorMax.x + (offsetMax.x / parent_width),
                _anchorMax.y + (offsetMax.y / parent_height));

            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;

            r.offsetMin = new Vector2(0, 0);
            r.offsetMax = new Vector2(0, 0);
            r.pivot = new Vector2(0.5f, 0.5f);
        }

        [MenuItem("CONTEXT/RectTransform/Fill Parent")]
        private static void FillParent()
        {
            RectTransform rect = Selection.activeTransform.GetComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }
    }

}
