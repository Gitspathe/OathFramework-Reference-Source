using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Audio;

namespace OathFramework.Audio
{

    [CreateAssetMenu(fileName = "Audio Params", menuName = "ScriptableObjects/AudioParams", order = 1)]
    public class AudioParams : ScriptableObject, IAudioSourceModifier
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioMixerGroup mixerGroup;
        [SerializeField, Range(0, 256)] private int priority = 128;

        [Space(5)]

        [SerializeField, Range(0.0f, 1.0f)] private float volume = 1.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float volumeRandom;

        [Space(5)]

        [SerializeField, Range(-3.0f, 3.0f)] private float pitch = 1.0f;
        [SerializeField, Range(0.0f, 1.0f)]  private float pitchRandom;

        [Space(5)]

        [SerializeField, MaxValue(int.MaxValue)] private float minDistance = 10;
        [SerializeField, MaxValue(int.MaxValue)] private float maxDistance = 50;

        [Space(5)]

        [SerializeField, Range(0.0f, 1.0f)] private float spatialBlend = 1.0f;

        [Space(10), Header("Modules")] 
        [SerializeReference] private AudioModule[] modules;
        
        public AudioSource ApplyToAudioSource(AudioSource source, out float duration, bool play = true, AudioOverrides overrides = null)
        {
            FRandom rand                 = FRandom.Cache;
            AudioClip clip               = clips.Length == 1 ? clips[0] : clips[rand.Range(0, clips.Length - 1)];
            source.clip                  = clip;
            source.outputAudioMixerGroup = mixerGroup;
            source.volume                = volume;
            source.pitch                 = pitch;
            source.maxDistance           = maxDistance;
            source.minDistance           = minDistance;
            source.spatialBlend          = spatialBlend;
            source.priority              = priority;
            if(volumeRandom > 0.001f) {
                float randVolume = volume * (1.0f + rand.Range(-volumeRandom, volumeRandom));
                source.volume = randVolume;
            }
            if(pitchRandom > 0.001f) {
                float randPitch = pitch * (1.0f + rand.Range(-pitchRandom, pitchRandom));
                source.pitch = randPitch;
            }
            duration = (clip.length * source.pitch) + 0.25f; // Slight duration padding prevents sounds from randomly not playing.
            foreach(AudioModule module in modules) {
                module.Apply(source, ref duration);
            }
            overrides?.Apply(source, ref duration);
            if(play) {
                source.Play();
            }
            return source;
        }
    }

    [Serializable]
    public abstract class AudioModule
    {
        public abstract void Apply(AudioSource source, ref float duration);
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
    public class HighPassModule : AudioModule
    {
        [Range(10.0f, 22000.0f)] public float cutoffFrequency = 5000.0f;
        [Range(1.0f, 10.0f)]     public float resonance       = 1.0f;
        
        public override void Apply(AudioSource source, ref float duration)
        {
            if(!source.TryGetComponent(out AudioHighPassFilter filter))
                return;

            filter.cutoffFrequency    = cutoffFrequency;
            filter.highpassResonanceQ = resonance;
            filter.enabled            = true;
        }
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
    public class LowPassModule : AudioModule
    {
        [Range(10.0f, 22000.0f)] public float cutoffFrequency = 5007.7f;
        [Range(1.0f, 10.0f)]     public float resonance       = 1.0f;
        
        public override void Apply(AudioSource source, ref float duration)
        {
            if(!source.TryGetComponent(out AudioLowPassFilter filter))
                return;

            filter.cutoffFrequency   = cutoffFrequency;
            filter.lowpassResonanceQ = resonance;
            filter.enabled           = true;
        }
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
    public class ReverbModule : AudioModule
    {
        [Range(-10000.0f, 0.0f)]    public float dryLevel         = 0.0f;
        [Range(-10000.0f, 0.0f)]    public float room             = 0.0f;
        [Range(-10000.0f, 0.0f)]    public float roomHF           = 0.0f;
        [Range(-10000.0f, 0.0f)]    public float roomLF           = 0.0f;
        [Range(0.1f, 20.0f)]        public float decayTime        = 1.0f;
        [Range(0.1f, 2.0f)]         public float decayHFRatio     = 0.5f;
        [Range(-10000.0f, 1000.0f)] public float reflectionsLevel = -10000.0f;
        [Range(0.0f, 0.3f)]         public float reflectionsDelay = 0.0f;
        [Range(-10000.0f, 2000.0f)] public float reverbLevel      = 0.0f;
        [Range(0.0f, 0.1f)]         public float reverbDelay      = 0.04f;
        [Range(1000.0f, 20000.0f)]  public float hfReference      = 5000.0f;
        [Range(20.0f, 1000.0f)]     public float lfReference      = 250.0f;
        [Range(0.0f, 100.0f)]       public float diffusion        = 100.0f;
        [Range(0.0f, 100.0f)]       public float density          = 100.0f;
        
        public override void Apply(AudioSource source, ref float duration)
        {
            if(!source.TryGetComponent(out AudioReverbFilter filter))
                return;

            filter.dryLevel         = dryLevel;
            filter.room             = room;
            filter.roomHF           = roomHF;
            filter.roomLF           = roomLF;
            filter.decayTime        = decayTime;
            filter.decayHFRatio     = decayHFRatio;
            filter.reflectionsLevel = reflectionsLevel;
            filter.reflectionsDelay = reflectionsDelay;
            filter.reverbLevel      = reverbLevel;
            filter.reverbDelay      = reverbDelay;
            filter.hfReference      = hfReference;
            filter.lfReference      = lfReference;
            filter.diffusion        = diffusion;
            filter.density          = density;
            filter.enabled          = true;
            duration               += reverbDelay + decayTime;
        }
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
    public class ChorusModule : AudioModule
    {
        [Range(0.0f, 1.0f)]   public float dryMix  = 0.5f;
        [Range(0.0f, 1.0f)]   public float wetMix1 = 0.5f;
        [Range(0.0f, 1.0f)]   public float wetMix2 = 0.5f;
        [Range(0.0f, 1.0f)]   public float wetMix3 = 0.5f;
        [Range(0.1f, 100.0f)] public float delay   = 40.0f;
        [Range(0.0f, 20.0f)]  public float rate    = 0.8f;
        [Range(0.0f, 1.0f)]   public float depth   = 0.03f;
        
        public override void Apply(AudioSource source, ref float duration)
        {
            if(!source.TryGetComponent(out AudioChorusFilter filter))
                return;

            filter.dryMix  = dryMix;
            filter.wetMix1 = wetMix1;
            filter.wetMix2 = wetMix2;
            filter.wetMix3 = wetMix3;
            filter.delay   = delay;
            filter.rate    = rate;
            filter.depth   = depth;
            filter.enabled = true;
            duration      += delay;
        }
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
    public class DistortionModule : AudioModule
    {
        [Range(0.0f, 1.0f)] public float level  = 0.5f;
        
        public override void Apply(AudioSource source, ref float duration)
        {
            if(!source.TryGetComponent(out AudioDistortionFilter filter))
                return;

            filter.distortionLevel = level;
            filter.enabled         = true;
        }
    }
    
    [Serializable]
    [SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
    public class EchoModule : AudioModule
    {
        [Range(10.0f, 5000.0f)] public float delay  = 500.0f;
        [Range(0.0f, 1.0f)] public float decayRatio = 0.5f;
        [Range(0.0f, 1.0f)] public float dryMix     = 1.0f;
        [Range(0.0f, 1.0f)] public float wetMix     = 1.0f;

        public override void Apply(AudioSource source, ref float duration)
        {
            if(!source.TryGetComponent(out AudioEchoFilter filter))
                return;

            filter.delay      = delay;
            filter.decayRatio = decayRatio;
            filter.dryMix     = dryMix;
            filter.wetMix     = wetMix;
            filter.enabled    = true;
            duration         += filter.delay * (filter.decayRatio * duration);
        }
    }
    
    [Serializable]
    public class AudioOverrides
    {
        [SerializeField, SerializeReference] private List<AudioOverride> overrides = new();

        public static AudioOverrides NoSpatialBlend { get; } = new(new SpatialBlendAudioOverride(0.0f));

        public AudioOverrides() { }
        
        public AudioOverrides(params AudioOverride[] overrides)
        {
            this.overrides.AddRange(overrides);
        }

        public AudioOverrides(List<AudioOverride> overrides)
        {
            foreach(AudioOverride @override in overrides) {
                this.overrides.Add(@override);
            }
        }

        public void Apply(AudioSource source, ref float duration)
        {
            foreach(AudioOverride @override in overrides) {
                @override.Apply(source, ref duration);
            }
        }
    }

    [Serializable]
    public abstract class AudioOverride
    {
        public abstract void Apply(AudioSource source, ref float duration);
    }

    [Serializable]
    public class ClipAudioOverride : AudioOverride
    {
        [field: SerializeField] public AudioClip[] Clips { get; set; }

        public ClipAudioOverride() { }
        
        public ClipAudioOverride(AudioClip[] clips)
        {
            Clips = clips;
        }

        public override void Apply(AudioSource source, ref float duration)
        {
            float curDurationMult = 1.0f;
            if(!ReferenceEquals(source.clip, null)) {
                curDurationMult = duration == 0.0f ? 0.0f : duration / source.clip.length;
            }
            
            AudioClip clip = Clips[0];
            if(Clips.Length > 1) {
                clip = Clips[FRandom.Cache.Range(0, Clips.Length - 1)];
            }
            source.clip = clip;
            duration    = source.clip.length * curDurationMult;
        }
    }

    [Serializable]
    public class MixerGroupAudioOverride : AudioOverride
    {
        [field: SerializeField] public AudioMixerGroup MixerGroup { get; set; }

        public MixerGroupAudioOverride() { }

        public MixerGroupAudioOverride(AudioMixerGroup mixerGroup)
        {
            MixerGroup = mixerGroup;
        }

        public override void Apply(AudioSource source, ref float duration)
        {
            source.outputAudioMixerGroup = MixerGroup;
        }
    }

    [Serializable]
    public class VolumeAudioOverride : AudioOverride
    {
        [field: SerializeField] public OverrideFunction Func                 { get; set; }
        [field: SerializeField, Range(0.0f, 1.0f)] public float Volume       { get; set; } = 1.0f;
        [field: SerializeField, Range(0.0f, 1.0f)] public float VolumeRandom { get; set; }

        public VolumeAudioOverride() { }
        
        public VolumeAudioOverride(float volume, float volumeRandom, OverrideFunction func = OverrideFunction.Set)
        {
            Volume       = volume;
            VolumeRandom = volumeRandom;
            Func         = func;
        }

        public override void Apply(AudioSource source, ref float duration)
        {
            float volume = Volume;
            if(VolumeRandom > 0.001f) {
                volume *= 1.0f + FRandom.Cache.Range(-VolumeRandom, VolumeRandom);
            }
            switch(Func) {
                case OverrideFunction.Set:
                    source.volume = volume;
                    break;
                case OverrideFunction.Add:
                    source.volume += volume;
                    break;
                case OverrideFunction.Subtract:
                    source.volume -= volume;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public class PitchAudioOverride : AudioOverride
    {
        [field: SerializeField] public OverrideFunction Func                { get; set; }
        [field: SerializeField, Range(0.0f, 1.0f)] public float Pitch       { get; set; } = 1.0f;
        [field: SerializeField, Range(0.0f, 1.0f)] public float PitchRandom { get; set; }

        public PitchAudioOverride() { }
        
        public PitchAudioOverride(float pitch, float pitchRandom, OverrideFunction func = OverrideFunction.Set)
        {
            Pitch       = pitch;
            PitchRandom = pitchRandom;
            Func        = func;
        }

        public override void Apply(AudioSource source, ref float duration)
        {
            float curDurationMult = 1.0f;
            float originalPitch   = 1.0f;
            if(!ReferenceEquals(source.clip, null)) {
                curDurationMult = duration == 0.0f ? 0.0f : duration / source.clip.length;
                originalPitch   = source.pitch;
            }
            
            float pitch = Pitch;
            if(PitchRandom > 0.001f) {
                pitch *= 1.0f + FRandom.Cache.Range(-PitchRandom, PitchRandom);
            }
            switch(Func) {
                case OverrideFunction.Set:
                    source.pitch = pitch;
                    break;
                case OverrideFunction.Add:
                    source.pitch += pitch;
                    break;
                case OverrideFunction.Subtract:
                    source.pitch -= pitch;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if(ReferenceEquals(source.clip, null) || originalPitch == 0.0f || pitch == 0.0f)
                return;
            
            duration = source.clip.length * (curDurationMult * (source.pitch / originalPitch));
        }
    }

    [Serializable]
    public class DistanceAudioOverride : AudioOverride
    {
        [field: SerializeField] public OverrideFunction FuncMin     { get; set; }
        [field: SerializeField] public OverrideFunction FuncMax     { get; set; }
        [field: SerializeField, Min(0.0f)] public float MinDistance { get; set; } = 1.0f;
        [field: SerializeField, Min(0.0f)] public float MaxDistance { get; set; } = 10.0f;

        public DistanceAudioOverride() { }
        
        public DistanceAudioOverride(float minDistance, float maxDistance, 
            OverrideFunction funcMin = OverrideFunction.Set, OverrideFunction funcMax = OverrideFunction.Set)
        {
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            FuncMin     = funcMin;
            FuncMax     = funcMax;
        }

        public override void Apply(AudioSource source, ref float duration)
        {
            switch(FuncMin) {
                case OverrideFunction.Set:
                    source.minDistance = MinDistance;
                    break;
                case OverrideFunction.Add:
                    source.minDistance += MinDistance;
                    break;
                case OverrideFunction.Subtract:
                    source.minDistance -= MinDistance;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            switch(FuncMax) {
                case OverrideFunction.Set:
                    source.maxDistance = MaxDistance;
                    break;
                case OverrideFunction.Add:
                    source.maxDistance += MaxDistance;
                    break;
                case OverrideFunction.Subtract:
                    source.maxDistance -= MaxDistance;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public class SpatialBlendAudioOverride : AudioOverride
    {
        [field: SerializeField] public OverrideFunction Func                 { get; set; }
        [field: SerializeField, Range(0.0f, 1.0f)] public float SpatialBlend { get; set; } = 1.0f;

        public SpatialBlendAudioOverride() { }
        
        public SpatialBlendAudioOverride(float spatialBlend, OverrideFunction func = OverrideFunction.Set)
        {
            SpatialBlend = spatialBlend;
            Func         = func;
        }

        public override void Apply(AudioSource source, ref float duration)
        {
            switch(Func) {
                case OverrideFunction.Set:
                    source.spatialBlend = SpatialBlend;
                    break;
                case OverrideFunction.Add:
                    source.spatialBlend += SpatialBlend;
                    break;
                case OverrideFunction.Subtract:
                    source.spatialBlend -= SpatialBlend;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public class PriorityAudioOverride : AudioOverride
    {
        [field: SerializeField] public OverrideFunction Func       { get; set; }
        [field: SerializeField, Range(0, 255)] public int Priority { get; set; } = 128;
        
        public PriorityAudioOverride() { }

        public PriorityAudioOverride(int priority, OverrideFunction func = OverrideFunction.Set)
        {
            Priority = priority;
            Func     = func;
        }

        public override void Apply(AudioSource source, ref float duration)
        {
            switch(Func) {
                case OverrideFunction.Set:
                    source.priority = Priority;
                    break;
                case OverrideFunction.Add:
                    source.priority += Priority;
                    break;
                case OverrideFunction.Subtract:
                    source.priority -= Priority;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public class ModuleAudioOverride : AudioOverride
    {
        [field: SerializeReference] public AudioModule Module { get; set; }
        
        public ModuleAudioOverride() { }

        public ModuleAudioOverride(AudioModule module)
        {
            Module = module;
        }

        public override void Apply(AudioSource source, ref float duration)
        {
            Module.Apply(source, ref duration);
        }
    }
    
    [Serializable]
    public class EntityAudioClip
    {
        [field: SerializeField] public string ID          { get; private set; }
        [field: SerializeField] public AudioParams Params { get; set; }
        [field: SerializeField] public bool FollowEntity  { get; set; } = true;
    }

    [Serializable]
    public class EntityLoopAudioClip
    {
        [field: SerializeField] public string ID            { get; private set; }
        [field: SerializeField] public EntityAudioLoop Loop { get; set; }
    }
    
    public enum OverrideFunction { Set, Add, Subtract }
}
