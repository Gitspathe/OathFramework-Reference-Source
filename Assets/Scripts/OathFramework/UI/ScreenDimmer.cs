using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI
{
    public class ScreenDimmer : MonoBehaviour
    {
        [SerializeField] private Image image;

        private static Tween currentTween;
        public static bool State { get; private set; }

        private static int dimmers;
        public static int Dimmers {
            get => dimmers;
            set {
                dimmers = value;
                if((State && value > 0) || (!State && value == 0))
                    return;
                
                _ = value > 0 ? Instance.FadeIn() : Instance.FadeOut();
            }
        }

        public static ScreenDimmer Instance { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(ScreenDimmer)} singletons.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        
        private async UniTask FadeIn()
        {
            if(currentTween.isAlive) {
                currentTween.Stop();
            }
            
            State = true;
            float start  = image.color.a;
            float time   = Mathf.Abs(start) < 0.01f ? AnimDuration.Medium : AnimDuration.Medium * (start / 0.55f);
            currentTween = Tween.Custom(
                this,
                start, 
                0.55f, 
                time, 
                (t, val) => t.image.color = new Color(0.0f, 0.0f, 0.0f, val), 
                useUnscaledTime: true
            );
            await currentTween;
        }

        private async UniTask FadeOut()
        {
            if(currentTween.isAlive) {
                currentTween.Stop();
            }
            
            State       = false;
            float start = image.color.a;
            if(Mathf.Abs(start) < 0.01f) {
                image.color = Color.clear;
                return;
            }
            currentTween = Tween.Custom(
                this, 
                start, 
                0.0f, 
                AnimDuration.Medium * (start / 0.55f), 
                (t, val) => t.image.color = new Color(0.0f, 0.0f, 0.0f, val), 
                useUnscaledTime: true
            );
            await currentTween;
        }
    }
}
