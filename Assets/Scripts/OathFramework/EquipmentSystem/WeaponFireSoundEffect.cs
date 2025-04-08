using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{

    public class WeaponFireSoundEffect : LoopComponent, 
        IWeaponModelInit, IEquipmentUseCallback, ILoopUpdate
    {
        [field: SerializeField] public AudioParams FireSound    { get; private set; }
        [field: SerializeField] public bool DoMechanicalEffects { get; private set; }
     
        [field: SerializeField, ShowIf(nameof(DoMechanicalEffects))]
        public AnimationCurve AddPitch                 { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(DoMechanicalEffects))]
        public AudioParams MechanicalEffect            { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(DoMechanicalEffects))]
        public AnimationCurve MechanicalEffectVolume   { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(DoMechanicalEffects))]
        public AnimationCurve MechanicalEffectAddPitch { get; private set; }
        
        [field: SerializeField] public bool PlayTail   { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(PlayTail))]
        public AudioParams TailSound                   { get; private set; }

        [field: SerializeField, ShowIf(nameof(PlayTail))]
        public float TailDelay                         { get; private set; } = 0.2f;
        
        private IEquipmentUserController owner;
        
        private AudioOverrides fireAudioOverrides;
        private SpatialBlendAudioOverride fireSpatialOverride;
        private PitchAudioOverride firePitchOverride;
        
        private AudioOverrides mechAudioOverrides;
        private SpatialBlendAudioOverride mechSpatialOverride;
        private VolumeAudioOverride mechVolumeAudioOverride;
        private PitchAudioOverride mechPitchAudioOverride;

        private AudioOverrides tailAudioOverrides;
        private SpatialBlendAudioOverride tailSpatialOverride;
        private QList<float> tailDelays;
        private QList<float> tailDelaysCopy;
        
        private void Awake()
        {
            fireSpatialOverride = new SpatialBlendAudioOverride();
            if(DoMechanicalEffects) {
                fireSpatialOverride     = new SpatialBlendAudioOverride();
                firePitchOverride       = new PitchAudioOverride(0.0f, 0.0f, OverrideFunction.Add);
                fireAudioOverrides      = new AudioOverrides(
                    fireSpatialOverride, firePitchOverride
                );
                mechSpatialOverride     = new SpatialBlendAudioOverride();
                mechVolumeAudioOverride = new VolumeAudioOverride();
                mechPitchAudioOverride  = new PitchAudioOverride(0.0f, 0.0f, OverrideFunction.Add);
                mechAudioOverrides      = new AudioOverrides(
                    mechSpatialOverride, mechVolumeAudioOverride, mechPitchAudioOverride
                );
            } else {
                fireAudioOverrides = new AudioOverrides(fireSpatialOverride);
            }
            if(PlayTail) {
                tailDelays          = new QList<float>(16);
                tailDelaysCopy      = new QList<float>(16);
                tailSpatialOverride = new SpatialBlendAudioOverride();
                tailAudioOverrides  = new AudioOverrides(tailSpatialOverride);
            }
        }
        
        private void OnDisable()
        {
            if(PlayTail) {
                tailDelays.Clear();
                tailDelaysCopy.Clear();
            }
            if(owner != null) {
                owner.Equipment.Callbacks.Unregister((IEquipmentUseCallback)this);
            }
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            if(!PlayTail)
                return;
            
            tailDelaysCopy.Clear();
            for(int i = 0; i < tailDelays.Count; i++) {
                float delay = tailDelays.Array[i] - Time.deltaTime;
                if(delay <= 0.0f) {
                    PlayTailSound();
                } else {
                    tailDelaysCopy.Add(delay);
                }
            }
            tailDelays.Clear();
            tailDelays.AddRange(tailDelaysCopy);
        }

        void IWeaponModelInit.OnInitialized(IEquipmentUserController owner)
        {
            this.owner = owner;
            owner.Equipment.Callbacks.Register((IEquipmentUseCallback)this);
        }

        private void PlayMechanicalSound(float curvePos, bool noSpatialBlend)
        {
            mechVolumeAudioOverride.Volume = MechanicalEffectVolume.Evaluate(curvePos);
            if(mechVolumeAudioOverride.Volume < 0.1f)
                return;
            
            mechSpatialOverride.SpatialBlend = noSpatialBlend ? 0.0f : 1.0f;
            mechPitchAudioOverride.Pitch     = MechanicalEffectAddPitch.Evaluate(curvePos);
            AudioPool.Retrieve(transform, Vector3.zero, MechanicalEffect, overrides: mechAudioOverrides);
        }

        private void PlayTailSound()
        {
            bool noSpatialBlend = false;
            if(owner is PlayerController player) {
                noSpatialBlend = player.Mode == PlayerControllerMode.Playing || player.Mode == PlayerControllerMode.Spectating;
            }
            tailSpatialOverride.SpatialBlend = noSpatialBlend ? 0.0f : 1.0f;
            AudioPool.Retrieve(transform, Vector3.zero, TailSound, overrides: tailAudioOverrides);
        }

        void IEquipmentUseCallback.OnEquipmentUse(EntityEquipment equipment, Equippable equippable, int ammo)
        {
            if(ReferenceEquals(FireSound, null))
                return;

            bool noSpatialBlend = false;
            if(owner is PlayerController player) {
                noSpatialBlend = player.Mode == PlayerControllerMode.Playing || player.Mode == PlayerControllerMode.Spectating;
            }
            if(DoMechanicalEffects) {
                int clip       = equippable.As<EquippableRanged>().Stats.AmmoCapacity;
                float curvePos = ammo - 1 <= 0 ? 1.0f : 1.0f - ((ammo - 1) / (float)clip);
                firePitchOverride.Pitch = AddPitch.Evaluate(curvePos);
                if(!ReferenceEquals(MechanicalEffect, null)) {
                    PlayMechanicalSound(curvePos, noSpatialBlend);
                }
            }
            if(PlayTail) {
                tailDelays.Add(TailDelay);
            }
            fireSpatialOverride.SpatialBlend = noSpatialBlend ? 0.0f : 1.0f;
            AudioPool.Retrieve(transform, Vector3.zero, FireSound, overrides: fireAudioOverrides);
        }
    }

}
