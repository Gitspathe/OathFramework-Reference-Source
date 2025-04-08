using Cysharp.Threading.Tasks;
using OathFramework.Core;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.Audio
{
    public class UIAudio : Subsystem
    {
        [field: SerializeField] public AudioParams NavSound             { get; private set; }
        [field: SerializeField] public AudioParams ConfirmSound         { get; private set; }
        [field: SerializeField] public AudioParams BackSound            { get; private set; }
        [field: SerializeField] public AudioParams SliderIncrementSound { get; private set; }
        [field: SerializeField] public AudioParams SliderDecrementSound { get; private set; }
        
        public static UIAudio Instance { get; private set; }

        public override string Name    => "UI Audio";
        public override uint LoadOrder => SubsystemLoadOrders.UIAudio;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(UIAudio)} singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            return UniTask.CompletedTask;
        }

        public static void PlaySound(UISounds sound, UIAudioHandler.Overrides[] overrides)
        {
            AudioParams @override = GetOverride(sound, overrides);
            if(!ReferenceEquals(@override, null)) {
                AudioPool.Retrieve(Vector3.zero, @override);
                return;
            }
            switch(sound) {
                case UISounds.Nav: {
                    AudioPool.Retrieve(Vector3.zero, Instance.NavSound);
                } break;
                case UISounds.Confirm: {
                    AudioPool.Retrieve(Vector3.zero, Instance.ConfirmSound);
                } break;
                case UISounds.Back: {
                    AudioPool.Retrieve(Vector3.zero, Instance.BackSound);
                } break;
                case UISounds.SliderIncrement: {
                    AudioPool.Retrieve(Vector3.zero, Instance.SliderIncrementSound);
                } break;
                case UISounds.SliderDecrement: {
                    AudioPool.Retrieve(Vector3.zero, Instance.SliderDecrementSound);
                } break;
                default:
                    return;
            }
        }

        private static AudioParams GetOverride(UISounds sound, UIAudioHandler.Overrides[] overrides)
        {
            if(overrides == null || overrides.Length == 0)
                return null;
            
            for(int i = 0; i < overrides.Length; i++) {
                if(sound == overrides[i].SoundType) {
                    return overrides[i].OverrideSound;
                }
            }
            return null;
        }
    }

    public enum UISounds
    {
        Nav, 
        Confirm, 
        Back, 
        SliderIncrement, 
        SliderDecrement
    }
}
