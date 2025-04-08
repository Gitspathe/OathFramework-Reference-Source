using System;
using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Projectiles;
using OathFramework.UI.Info;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OathFramework.EquipmentSystem
{
    public abstract class Equippable : MonoBehaviour
    {
        public abstract EquippableTypes Type                                       { get; }
        
        [field: SerializeField] public string EquippableName                       { get; private set; }
        [field: SerializeField] public UIEquippableInfo UIInfo                     { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public string EquippableKey                        { get; private set; }
        [field: SerializeField] public ushort DefaultID                            { get; private set; }
        [field: SerializeField] public bool LoseOnEmpty                            { get; private set; }
        [field: SerializeField] public bool Temporary                              { get; private set; }
        [field: SerializeField, ShowIf("@!Temporary")] public EquipmentSlot Slot   { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public SoundEffectPair[] SoundEffects              { get; private set; }

        [field: Space(10)]
        
        [field: SerializeField] public EquippableThirdPersonModel ThirdPersonModel { get; private set; }
        [field: SerializeField] public EquippableHolsteredModel HolsteredModel     { get; private set; }
        [field: SerializeField] public float SwapIKSuppressTime                    { get; private set; } = 0.1f;
        
        public ushort ID                                                           { get; set; }
        
        public T As<T>() where T : Equippable                             => this as T;
        public T GetStatsAs<T>() where T : EquippableStats                => GetRootStats() as T;
        public T GetStatsInterface<T>() where T : class, IEquippableStats => GetRootStats() as T;

        public abstract EquippableStats GetRootStats();

        [Button("Auto Assign ID")]
        private void AutoID()
        {
            bool found = false;
            if(!IsIDTaken(this)) {
                Debug.Log("ID is already unique.");
                return;
            }
            for(ushort i = 10; i < ushort.MaxValue; i++) {
                if(IsIDTaken(this, i))
                    continue;

                DefaultID = i;
                found     = true;
                break;
            }
            if(!found) {
                Debug.LogError("No unique ID found.");
            }
        }

        private bool IsIDTaken(Equippable equippable, int? id = null)
        {
            if(SceneManager.GetActiveScene().name != "_MAIN")
                throw new Exception("Active scene is not _MAIN.");

            foreach(Equippable e in FindObjectsByType<Equippable>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if(e != null && e == equippable)
                    continue;
                if(!id.HasValue && e.DefaultID == DefaultID)
                    return true;
                if(id.HasValue && e.DefaultID == id.Value)
                    return true;
            }
            return false;
        }
        
        public void RegisterInfo()
        {
            if(UIInfo == null)
                return;
            
            UIInfo          = UIInfo.DeepCopy();
            UIInfo.Template = this;
            UIInfoManager.RegisterEquippableInfo(EquippableKey, UIInfo);
        }

        public void ApplySounds(EntityAudio playerAudio)
        {
            foreach(SoundEffectPair sfx in SoundEffects) {
                playerAudio.SetAudioClip(sfx.soundID, sfx.@params);
            }
        }

        public void TakenOut(EntityEquipment equipment)
        {
            OnTakeOut(equipment);
        }

        public void PutAway(EntityEquipment equipment)
        {
            if(Game.IsQuitting)
                return;
            
            OnPutAway(equipment);
        }

        public virtual void RegisterToProjectileProvider(ProjectileProvider projectileProvider) { }
        public virtual void UnregisterFromProjectileProvider(ProjectileProvider projectileProvider) { }
        
        private EquippableModel CloneModel(IEquipmentUserController player, ModelPerspective perspective)
        {
            switch(perspective) {
                case ModelPerspective.ThirdPerson:
                    return ThirdPersonModel != null ? ThirdPersonModel.Clone(player, this) : null;
                case ModelPerspective.Holstered:
                    return HolsteredModel != null ? HolsteredModel.Clone(player, this) : null;

                default: {
                    Debug.LogError("Attempted to clone an invalid equippable perspective.");
                    return null;
                }
            }
        }

        public T CloneModel<T>(IEquipmentUserController player, ModelPerspective perspective) where T : EquippableModel 
            => (T)CloneModel(player, perspective);

        protected virtual void OnTakeOut(EntityEquipment equipment) { }
        protected virtual void OnPutAway(EntityEquipment equipment) { }
    }
    
    [Serializable]
    public abstract class EquippableStats : IEquippableStats
    {
        [field: Header("Equippable Stats (Base)")]
        [field: Header("Animation Timings")]

        [field: SerializeField] public float PutAwayTime           { get; set; } = 0.25f;
        [field: SerializeField] public float TakeOutTime           { get; set; } = 0.25f;

        [field: Header("Effects")]
        
        [field: SerializeField] public List<HitEffectInfo> Effects { get; set; } = new();
        
        [field: Header("Mechanics")]
        
        [field: SerializeField] public ushort AmmoCapacity         { get; set; } = 30;

        public abstract float GetTimeBetweenUses(float useRateMult);
        
        public T As<T>() where T : EquippableStats => (T)this;

        public virtual void CopyTo(EquippableStats other)
        {
            other.PutAwayTime  = PutAwayTime;
            other.TakeOutTime  = TakeOutTime;
            other.AmmoCapacity = AmmoCapacity;
            other.Effects.Clear();
            foreach(HitEffectInfo effect in Effects) {
                other.Effects.Add(effect);
            }
        }
    }
    
    [Serializable]
    public class SoundEffectPair
    {
        public string soundID;
        public AudioParams @params;
    }

    public enum EquippableAnimSets : byte
    {
        Unarmed  = 0,
        Pistol   = 1,
        Rifle    = 2,
        Shotgun  = 3,
        Grenade  = 4,
        SawedOff = 5
    }

    public enum EquippableTypes : byte
    {
        None            = 0,
        Ranged          = 1,
        RangedMultiShot = 2,
        Grenade         = 3,
    }
}
