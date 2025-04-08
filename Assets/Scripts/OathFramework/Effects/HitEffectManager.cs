using Cysharp.Threading.Tasks;
using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Pooling;
using OathFramework.Settings;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.Effects
{
    public sealed class HitEffectManager : Subsystem, IMaterialPreloaderDataProvider
    {
        public override string Name    => "Hit Effect Manager";
        public override uint LoadOrder => SubsystemLoadOrders.HitEffectManager;
        public static HitEffectManager Instance { get; private set; }

        [SerializeField] private HitEffectParamsCollection[] collection;

        private static Database database = new();
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(HitEffectManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
            
            foreach(HitEffectParamsCollection heParams in collection) {
                foreach(HitEffectParams @params in heParams.collection) {
                    if(!Register(@params, out ushort _))
                        continue;
                    
                    PoolManager.RegisterPool(new PoolManager.GameObjectPool(@params.PrefabPool), true);
                }
            }
            return UniTask.CompletedTask;
        }
        
        public static bool Register(HitEffectParams effect, out ushort id)
        {
            id = default;
            if(database.RegisterWithID(effect.Key, effect, effect.DefaultID)) {
                effect.ID = effect.DefaultID;
                id        = effect.ID;
                return true;
            }
            if(database.Register(effect.Key, effect, out ushort retID)) {
                effect.ID = retID;
                id        = effect.ID;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(HitEffectParams)} '{effect.Key}'.");
            return false;
        }

        public static bool TryGetHitEffectParams(string key, out HitEffectParams effectParams, out ushort id)
        {
            return database.TryGet(key, out effectParams, out id);
        }

        public static bool TryGetHitEffectParams(ushort id, out HitEffectParams hitEffectParams, out string key)
        {
            return database.TryGet(id, out hitEffectParams, out key);
        }

        public static GameObject CreateEffect(
            Transform transform,
            bool playSound, 
            in DamageValue damageVal, 
            in HitEffectValue hitVal, 
            Color? color = null, 
            bool @static = false)
        {
            bool b = damageVal.GetInstigator(out Entity instigator);
            return CreateEffect(transform, playSound, damageVal.HitPosition, b ? instigator : null, hitVal, color, @static);
        }
        
        public static GameObject CreateEffect(
            Transform transform, 
            bool playSound, 
            in HealValue healVal, 
            in HitEffectValue hitVal, 
            Color? color = null,
            bool @static = false)
        {
            bool b = healVal.GetInstigator(out Entity instigator);
            return CreateEffect(transform, playSound, transform.position, b ? instigator : null, hitVal, color, @static);
        }
        
        public static GameObject CreateEffect(
            Transform transform,
            bool playSound, 
            Vector3 hitPosition, 
            Entity instigator, 
            in HitEffectValue hitVal, 
            Color? color = null, 
            bool @static = false)
        {
            hitVal.Deconstruct(out HitEffectParams @params);
            if(ReferenceEquals(@params, null))
                return null;
            
            if(playSound && !ReferenceEquals(@params.Audio, null)) {
                CreateAudio(hitPosition, @params.Audio);
            }
            if(ReferenceEquals(@params.Prefab, null))
                return null;

            Vector3 position    = hitPosition - transform.position;
            Quaternion rotation = Quaternion.identity;
            if(@params.RotateToSource && !ReferenceEquals(instigator, null)) {
                Vector3 targetDir = instigator.transform.position - transform.position;
                Quaternion target = Quaternion.LookRotation(targetDir);
                rotation          = Quaternion.Euler(0.0f, target.eulerAngles.y, 0.0f) * Quaternion.Inverse(transform.rotation);
            }

            GameObject visual = CreateVisual(@static ? null : transform, position, rotation, @params.Prefab);
            if(visual.TryGetComponent(out IColorable colorable)) {
                colorable.SetColor(color);
            }
            return visual;
        }

        private static void CreateAudio(Vector3 position, AudioParams @params)
        {
            AudioPool.Retrieve(position, @params);
        }
        
        private static GameObject CreateVisual(Transform transform, Vector3 position, Quaternion rotation, GameObject effect)
        {
            return PoolManager.Retrieve(effect, position, rotation, null, transform).gameObject;
        }
        
        Material[] IMaterialPreloaderDataProvider.GetMaterials()
        {
            List<Material> materials = new();
            foreach(HitEffectParamsCollection visual in collection) {
                foreach(HitEffectParams @params in visual.collection) {
                    foreach(Renderer render in @params.PrefabPool.Prefab.GetComponentsInChildren<Renderer>(true)) {
                        materials.AddRange(render.sharedMaterials);
                    }
                    foreach(IMaterialPreloaderDataProvider prov in @params.PrefabPool.Prefab.GetComponentsInChildren<IMaterialPreloaderDataProvider>(true)) {
                        materials.AddRange(prov.GetMaterials());
                    }
                }
            }
            return materials.ToArray();
        }
        
        private sealed class Database : Database<string, ushort, HitEffectParams>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
    
    [Serializable]
    public partial class HitEffectInfo
    {
        public HitSurfaceMaterial materials;
        
        [ValueDropdown("GetAllParams", DoubleClickToConfirm = true, OnlyChangeValueOnConfirm = true)]
        public string hitEffectParams;
        
        public HitEffectValue ToValue() => new(hitEffectParams);

        public bool ContainsMaterial(HitSurfaceMaterial material) 
            => (materials & material) != 0;
        
#if UNITY_EDITOR
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static IEnumerable GetAllParams() => AssetStringDropdownDB.GetValues<HitEffectParams>();
#endif
    }
}
