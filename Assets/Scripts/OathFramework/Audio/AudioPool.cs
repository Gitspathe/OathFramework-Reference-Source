using Cysharp.Threading.Tasks;
using UnityEngine;
using OathFramework.Core;
using OathFramework.Pooling;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.Audio
{ 

    public sealed class AudioPool : Subsystem
    {
        [SerializeField] private PoolParams audioPoolParams;
        
        public static AudioPool Instance { get; private set; }

        public override string Name    => "Audio Pool";
        public override uint LoadOrder => SubsystemLoadOrders.AudioPool;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(AudioPool)} singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            PoolManager.RegisterPool(new PoolManager.GameObjectPool(audioPoolParams), true);
            return UniTask.CompletedTask;
        }

        public static AudioSource Retrieve(
            Vector3 position, 
            IAudioSourceModifier @params, 
            bool play = true, 
            AudioOverrides overrides = null)
        {
            PoolableGameObject go      = PoolManager.Retrieve(Instance.audioPoolParams.Prefab, position, null, null);
            PoolableAudioSource source = go.GetComponent<PoolableAudioSource>();
            @params.ApplyToAudioSource(source.Source, out float duration, play, overrides);
            source.SetLife(duration);
            return source.Source;
        }

        public static AudioSource Retrieve(
            Transform parent, 
            Vector3 offset, 
            IAudioSourceModifier @params, 
            bool play = true, 
            AudioOverrides overrides = null)
        {
            PoolableGameObject go      = PoolManager.Retrieve(Instance.audioPoolParams.Prefab, offset, null, null, parent);
            PoolableAudioSource source = go.GetComponent<PoolableAudioSource>();
            @params.ApplyToAudioSource(source.Source, out float duration, play, overrides);
            source.SetLife(duration);
            return source.Source;
        }
    }

    public interface IAudioSourceModifier
    {
        AudioSource ApplyToAudioSource(AudioSource source, out float duration, bool play = true, AudioOverrides overrides = null);
    }

}
