using OathFramework.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI
{

    [RequireComponent(typeof(ScrollRect))]
    public class AutoScrollRect : LoopComponent, ILoopUpdate
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        [SerializeField] private RectTransform viewportRectTransform;
        [SerializeField] private RectTransform contentRectTransform;
        private ScrollRect scrollRect;
        private RectTransform selectedRectTransform;

        public bool IsFocused { get; set; } = true;
        
        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        void ILoopUpdate.LoopUpdate()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if(!IsFocused || !GameControls.UsingController || selected == null || !selected.transform.IsChildOf(contentRectTransform)) 
                return;

            AutoScrollBlocker block = selected.GetComponent<AutoScrollBlocker>();
            if(block != null && (block.Target == this || block.Target == null))
                return;
     
            selectedRectTransform     = selected.GetComponent<RectTransform>();
            Rect viewportRect         = viewportRectTransform.rect;
            Rect selectedRect         = selectedRectTransform.rect;
            Rect selectedRectWorld    = selectedRect.Transform(selectedRectTransform);
            Rect selectedRectViewport = selectedRectWorld.InverseTransform(viewportRectTransform);
            float outsideOnTop        = selectedRectViewport.yMax - viewportRect.yMax;
            float outsideOnBottom     = viewportRect.yMin - selectedRectViewport.yMin;
            if(outsideOnTop < 0) {
                outsideOnTop = 0;
            }
            if(outsideOnBottom < 0) {
                outsideOnBottom = 0;
            }

            float delta = outsideOnTop > 0 ? outsideOnTop : -outsideOnBottom;
            if(delta == 0) 
                return;
           
            Rect contentRect         = contentRectTransform.rect;
            Rect contentRectWorld    = contentRect.Transform(contentRectTransform);
            Rect contentRectViewport = contentRectWorld.InverseTransform(viewportRectTransform);
            float overflow           = contentRectViewport.height - viewportRect.height;
            float unitsToNormalized  = 1.0f / overflow;
            scrollRect.verticalNormalizedPosition += delta * unitsToNormalized;
        }
    }
    
    internal static class RectExtensions 
    {
        /// <summary>
        /// Transforms a rect from the transform local space to world space.
        /// </summary>
        public static Rect Transform(this Rect r, Transform transform) 
            => new() { 
                min = transform.TransformPoint(r.min),
                max = transform.TransformPoint(r.max)
            };

        /// <summary>
        /// Transforms a rect from world space to the transform local space
        /// </summary>
        public static Rect InverseTransform(this Rect r, Transform transform) 
            => new() {
                min = transform.InverseTransformPoint(r.min),
                max = transform.InverseTransformPoint(r.max)
            };
    }

}
