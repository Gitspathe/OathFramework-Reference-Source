using OathFramework.Utility;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace OathFramework.Pooling
{ 

    [RequireComponent(typeof(AudioSource), typeof(PoolableGameObject), typeof(DestroyAfterTime))]
    public class PoolableAudioSource : MonoBehaviour, IPoolableComponent
    {
        public AudioSource Source { get; private set; }
        
        private DestroyAfterTime ttl;

        public AudioHighPassFilter HighPass              { get; private set; }
        public AudioLowPassFilter LowPass                { get; private set; }
        public AudioReverbFilter Reverb                  { get; private set; }
        public AudioChorusFilter Chorus                  { get; private set; }
        public AudioDistortionFilter Distortion          { get; private set; }
        public AudioEchoFilter Echo                      { get; private set; }

        PoolableGameObject IPoolableComponent.PoolableGO { get; set; }

        private void Awake()
        {
            Source             = GetComponent<AudioSource>();
            ttl                = GetComponent<DestroyAfterTime>();
            HighPass           = GetComponent<AudioHighPassFilter>();
            LowPass            = GetComponent<AudioLowPassFilter>();
            Reverb             = GetComponent<AudioReverbFilter>();
            Chorus             = GetComponent<AudioChorusFilter>();
            Distortion         = GetComponent<AudioDistortionFilter>();
            Echo               = GetComponent<AudioEchoFilter>();
            HighPass.enabled   = false;
            LowPass.enabled    = false;
            Reverb.enabled     = false;
            Chorus.enabled     = false;
            Distortion.enabled = false;
            Echo.enabled       = false;
        }

        void IPoolableComponent.OnRetrieve() { }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            HighPass.enabled   = false;
            LowPass.enabled    = false;
            Reverb.enabled     = false;
            Chorus.enabled     = false;
            Distortion.enabled = false;
            Echo.enabled       = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLife(float duration)
        {
            ttl.Set(duration);
        }
    }

}
