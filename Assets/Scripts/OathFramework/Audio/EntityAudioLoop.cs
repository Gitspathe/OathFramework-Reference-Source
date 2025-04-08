using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;

namespace OathFramework.Audio
{

    [RequireComponent(typeof(AudioSource))]
    public class EntityAudioLoop : MonoBehaviour
    {
        private AudioSource source;
        private float baseVolume;
        private State state;
        private float curTimer;

        private void Awake()
        {
            source     = GetComponent<AudioSource>();
            baseVolume = source.volume;
        }

        public void FadeIn(float time, bool spatial)
        {
            source.spatialBlend = spatial ? 1.0f : 0.0f;
            if(time < 0.001f) {
                source.Play();
                source.volume = baseVolume;
                return;
            }
            state = State.FadeIn;
            _     = FadeInTask(time);
        }

        public void FadeOut(float time)
        {
            if(time < 0.001f) {
                source.Stop();
                return;
            }
            state = State.FadeOut;
            _     = FadeOutTask(time);
        }

        private async UniTask FadeInTask(float time)
        {
            while(state == State.FadeIn) {
                source.Play();
                curTimer = 0.0f;
                while(curTimer < time) {
                    curTimer += Time.deltaTime;
                    source.volume = baseVolume * (curTimer / time);
                    await UniTask.Yield();
                }
                state = State.None;
            }
        }

        private async UniTask FadeOutTask(float time)
        {
            while(state == State.FadeOut) {
                curTimer = 0.0f;
                while(curTimer < time) {
                    curTimer += Time.deltaTime;
                    source.volume = baseVolume - (baseVolume * (curTimer / time));
                    await UniTask.Yield();
                }
                source.Stop();
                state = State.None;
            }
        }

        private enum State { None, FadeIn, FadeOut }
    }

}
