using Cysharp.Threading.Tasks;
using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.Core.GameEvents
{
    public sealed class ScreenTransitionEvent : Event<ScreenTransitionParams>
    {
        [SerializeField] private Image image;
        
        public override Event.Type EventType => Event.Type.ScreenTransition;
        public bool IsFadingIn  { get; private set; }
        public bool IsFadingOut { get; private set; }

        private static Func<bool> isCompleteDelegate;
        private static Func<bool> isFadingInDelegate;
        private static Func<bool> isFadingOutDelegate;
        private static ScreenTransitionEvent instance;
        
        public override async UniTask<Event<ScreenTransitionParams>> WaitForCompletion()
        {
            await UniTask.WaitUntil(isCompleteDelegate, PlayerLoopTiming.LastUpdate)
                .SuppressCancellationThrow();
            return this;
        }

        public async UniTask WaitForFadeIn()
        {
            await UniTask.WaitUntil(isFadingInDelegate, PlayerLoopTiming.LastUpdate)
                .SuppressCancellationThrow();
        }

        public async UniTask WaitForFadeOut()
        {
            await UniTask.WaitUntil(isFadingOutDelegate, PlayerLoopTiming.LastUpdate)
                .SuppressCancellationThrow();
        }

        protected override void OnInitialize()
        {
            if(instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(ScreenTransitionEvent)} singleton.");
                Destroy(this);
                return;
            }

            isCompleteDelegate  = () => instance.IsComplete;
            isFadingInDelegate  = () => instance.IsFadingIn  || instance.IsComplete;
            isFadingOutDelegate = () => instance.IsFadingOut || instance.IsComplete;
            instance = this;
        }

        protected override void OnDeactivate(bool complete)
        {
            IsFadingIn  = false;
            IsFadingOut = false;
            image.gameObject.SetActive(Input.Order == ScreenTransitionOrder.FadeOut);
        }

        protected override void OnActivate()
        {
            IsFadingIn  = false;
            IsFadingOut = false;
            image.gameObject.SetActive(Input.Order == ScreenTransitionOrder.FadeIn);
            Task = Execute();
        }

        private async UniTask Execute()
        {
            switch(Input.Order) {
                case ScreenTransitionOrder.FadeIn:
                    while(IsActive) {
                        await ExecuteFadeIn();
                    }
                    break;
                case ScreenTransitionOrder.FadeOutThenIn:
                    while(IsActive) {
                        await ExecuteFadeOutThenIn();
                    }
                    break;
                case ScreenTransitionOrder.FadeOut:
                    while(IsActive) {
                        await ExecuteFadeOut();
                    }
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async UniTask ExecuteFadeOutThenIn()
        {
            IsFadingOut = true;
            await Tween.Color(image, Color.black, new TweenSettings(Input.FadeOut, Ease.OutExpo));
            await UniTask.Delay(TimeSpan.FromSeconds(Input.Hold), cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();
            
            IsFadingIn = true;
            await Tween.Color(image, Color.clear, new TweenSettings(Input.FadeIn, Ease.OutExpo));

            Deactivate(true);
        }

        private async UniTask ExecuteFadeIn()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Input.Hold), cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();

            IsFadingIn = true;
            await Tween.Color(image, Color.clear, new TweenSettings(Input.FadeIn, Ease.OutExpo));

            Deactivate(true);
        }
        
        private async UniTask ExecuteFadeOut()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Input.Hold), cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();

            IsFadingOut = true;
            await Tween.Color(image, Color.black, new TweenSettings(Input.FadeOut, Ease.OutExpo));

            Deactivate(true);
        }
    }
    
    public readonly struct ScreenTransitionParams
    {
        public ScreenTransitionOrder Order { get; }
        public float FadeOut               { get; }
        public float Hold                  { get; }
        public float FadeIn                { get; }

        public float TimeToFadeIn => FadeOut + Hold;
        public float Duration     => TimeToFadeIn + FadeIn;

        public ScreenTransitionParams(ScreenTransitionOrder order, float fadeOut, float hold, float fadeIn)
        {
            Order   = order;
            FadeOut = fadeOut;
            Hold    = hold;
            FadeIn  = fadeIn;
        }
    }
    
    public enum ScreenTransitionOrder
    {
        FadeOutThenIn,
        FadeIn,
        FadeOut
    }
}
