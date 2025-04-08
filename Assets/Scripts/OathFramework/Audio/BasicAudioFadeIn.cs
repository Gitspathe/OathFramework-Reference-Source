using OathFramework.Core;
using UnityEngine;

namespace OathFramework.Audio
{

    [RequireComponent(typeof(AudioSource))]
    public class BasicAudioFadeIn : LoopComponent, ILoopUpdate
    {
        [SerializeField] private AnimationCurve fadeInCurve;

        private AudioSource source;
        private float time;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        void ILoopUpdate.LoopUpdate()
        {
            time += Time.deltaTime;
            source.volume = fadeInCurve.Evaluate(time);
        }
    }

}
