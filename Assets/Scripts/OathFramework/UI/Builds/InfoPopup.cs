using OathFramework.Core;
using OathFramework.Progression;
using OathFramework.UI.Builds;
using OathFramework.UI.Info;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OathFramework.UI
{
    public class InfoPopup : LoopComponent, 
        ILoopUpdate, IResetGameStateCallback
    {
        public GameObject mainPanel;
        public RectTransform content;
        public Vector2 offset = new(10, -10);

        [SerializeField] private Canvas canvas;
        private RectTransform curTransform;
        
        public static DetailsView Details { get; private set; }
        public static bool IsVisible      { get; private set; }
        public static InfoPopup Instance  { get; private set; }

        public InfoPopup Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(InfoPopup)} singleton.");
                Destroy(gameObject);
                return null;
            }
            Instance = this;
            Details  = GetComponent<DetailsView>().Initialize();
            GameCallbacks.Register((IResetGameStateCallback)this);
            return this;
        }

        void ILoopUpdate.LoopUpdate()
        {
            Process();
        }

        public void Show(in PlayerBuildData buildData)
        {
            mainPanel.gameObject.SetActive(true);
            Details.SetupLevelDetails(in buildData);
            IsVisible = true;
            Process();
        }

        public void Show(UIInfo info, RectTransform rectTransform)
        {
            curTransform = rectTransform;
            if(info == null) {
                Hide();
                return;
            }
            
            mainPanel.gameObject.SetActive(true);
            Details.SetupDetails(info);
            IsVisible = true;
            Process();
        }

        public void Hide()
        {
            curTransform = null;
            Details.SetupDetails(null);
            IsVisible = false;
            mainPanel.gameObject.SetActive(false);
            Process();
        }

        private void Process()
        {
            if(IsVisible && Camera.main != null) {
                ShowUIAtPoint(GameControls.UsingController ? curTransform.position : Mouse.current.position.ReadValue());
            } else {
                mainPanel.gameObject.SetActive(false);
            }
        }

        private void ShowUIAtPoint(Vector2 screenPosition)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 screenSize  = new(Screen.width, Screen.height);
            Vector2 elementSize = content.sizeDelta * content.lossyScale;
            screenPosition += new Vector2(offset.x, offset.y - elementSize.y);
            AdjustPivot(screenPosition);
            float clampedX   = Mathf.Clamp(screenPosition.x, 0 + elementSize.x * content.pivot.x, screenSize.x - (elementSize.x/2));
            float clampedY   = Mathf.Clamp(screenPosition.y, 0 + elementSize.y * content.pivot.y, screenSize.y - elementSize.y);
            content.position = new Vector2(clampedX, clampedY);
            if(!content.gameObject.activeSelf) {
                content.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
            }
        }

        private void AdjustPivot(Vector2 screenPosition)
        {
            Vector2 canvasSize  = ((RectTransform)canvas.transform).sizeDelta;
            Vector2 elementSize = content.sizeDelta * content.lossyScale;
            float pivotX        = content.pivot.x;
            float pivotY        = content.pivot.y;

            // Horizontal adjustment
            if(screenPosition.x + elementSize.x > canvasSize.x) {
                pivotX = 1f;
            } else if(screenPosition.x < elementSize.x * (1 - content.pivot.x)) {
                pivotX = 0f;
            }

            // Vertical adjustment
            if(screenPosition.y - elementSize.y < 0) {
                pivotY = 0f;
            } else if(screenPosition.y + elementSize.y * content.lossyScale.y > canvasSize.y) {
                pivotY = 1f;
            }
            content.pivot = new Vector2(pivotX, pivotY);
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            Hide();
        }
    }
}
