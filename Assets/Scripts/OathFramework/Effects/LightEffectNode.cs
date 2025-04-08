using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.Effects
{
    [RequireComponent(typeof(Light))]
    public class LightEffectNode : EffectNode
    {
        [SerializeField] private AnimationCurve intensityCurve;

        private Light theLight;
        private float startIntensity;

        private void Awake()
        {
            theLight       = GetComponent<Light>();
            startIntensity = theLight.intensity;
        }

        protected override void OnSetColor(ParticleSystem.MinMaxGradient? color)
        {
            if(color.HasValue) {
                theLight.color = color.Value.color;
            }
        }
        
        protected override void OnShow()
        {
            theLight.intensity = startIntensity;
        }

        protected override void OnDissipate(float duration)
        {
            if(duration == 0.0f)
                return;
            
            _ = Fade(duration);
        }

        private async UniTask Fade(float duration)
        {
            float curTime = 0.0f;
            while(true) {
                curTime += Time.deltaTime;
                if(curTime > duration || !enabled)
                    break;
                
                theLight.intensity = startIntensity * intensityCurve.Evaluate(curTime / duration);
                if(await UniTask.Yield(Game.ResetCancellation.Token).SuppressCancellationThrow())
                    return;
            }
        }

        protected override void OnHide() {}
    }
}
