using Cysharp.Threading.Tasks;
using PrimeTween;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

namespace OathFramework.UI
{ 

    public class LoadingUIScript : MonoBehaviour
    {
        [SerializeField] private Transform loadingTransform;
        [SerializeField] private TextMeshProUGUI taskText;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private Slider progressSlider;

        [Space(10)]

        [SerializeField] private float tipChangeTime = 5.0f;
        [SerializeField] private LocalizedString[] tips;
        [SerializeField] private CanvasGroup canvasGroup;

        private bool showTips;
        private float timeSinceTipChange;
        private int lastTipIndex = -1;
        private CancellationTokenSource loadCts;

        private static CancellationTokenSource cancelTokenSource;
        private static Tween currentTween;

        public float Progress { get; set; }
        
        public static LoadingUIScript Instance { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(LoadingUIScript)} singletons.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _ = Hide(true);
        }

        private void Update()
        {
            progressSlider.value = Progress;
        }

        private static void CancelExistingTask()
        {
            if(currentTween.isAlive) {
                currentTween.Stop();
            }
            cancelTokenSource?.Cancel();
            cancelTokenSource?.Dispose();
            cancelTokenSource = new CancellationTokenSource();
        }

        public static async UniTask Show(bool instant = false)
        {
            CancelExistingTask();
            Instance.loadingTransform.gameObject.SetActive(true);
            if(instant) {
                Instance.canvasGroup.alpha = 1.0f;
                return;
            }
            currentTween               = Tween.Custom(Instance, 0.0f, 1.0f, AnimDuration.Slow, (t, val) => t.canvasGroup.alpha = val);
            Instance.canvasGroup.alpha = 1.0f;
            await currentTween;
        }

        public static async UniTask Hide(bool instant = false)
        {
            CancelExistingTask();
            if(instant) {
                Instance.canvasGroup.alpha = 0.0f;
                Instance.loadingTransform.gameObject.SetActive(false);
                return;
            }
            
            currentTween               = Tween.Custom(Instance, 1.0f, 0.0f, AnimDuration.Slow, (t, val) => t.canvasGroup.alpha = val);
            Instance.canvasGroup.alpha = 0.0f;
            await currentTween;
            Instance.loadingTransform.gameObject.SetActive(false);
        }

        public static void ChangeTip()
        {
            if(Instance.tips.Length == 0)
                return;
            
            int attempts = 0;
            while(attempts < 100) {
                int rand = Random.Range(0, Instance.tips.Length);
                if(rand == Instance.lastTipIndex) {
                    attempts++;
                    continue;
                }

                Instance.tipText.text = Instance.tips[rand].GetLocalizedString();
                Instance.lastTipIndex = rand;
                break;
            }
            Instance.timeSinceTipChange = 0.0f;
        }

        public static void SetProgress(LocalizedString task, float progress)
        {
            Instance.taskText.text = task.GetLocalizedString();
            Instance.Progress      = progress;
        }
        
        public static void StartSceneProgressTask(AsyncOperation operation, bool showTips)
        {
            if(Instance.loadCts != null) {
                Instance.loadCts.Cancel();
                Instance.loadCts.Dispose();
            }

            Instance.loadCts = new CancellationTokenSource();
            Instance.showTips = showTips;
            Instance.tipText.gameObject.SetActive(showTips);
            if(showTips) {
                ChangeTip();
            }
            _ = Instance.SceneLoadAsync(operation, Instance.loadCts.Token);
        }

        private async UniTask SceneLoadAsync(AsyncOperation operation, CancellationToken ct)
        {
            while(!operation.isDone) {
                Progress            = operation.progress * 1.0f;
                timeSinceTipChange += Time.unscaledDeltaTime;
                if(timeSinceTipChange > tipChangeTime) {
                    ChangeTip();
                }
                await UniTask.Yield(ct);
            }
        }
    }

}
