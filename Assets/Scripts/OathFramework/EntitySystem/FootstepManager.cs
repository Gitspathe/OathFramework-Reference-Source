using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using OathFramework.Attributes;
using OathFramework.Audio;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.Pooling;
using OathFramework.Settings;
using Sirenix.OdinInspector;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem
{ 

    public sealed class FootstepManager : Subsystem, IMaterialPreloaderDataProvider
    {
        public static FootstepManager Instance { get; private set; }

        [SerializeField] private AudioMixerGroup mixerGroup;
        [SerializeField] private GameObject impulseEffectPrefab;
        [SerializeField] private AnimationCurve impulseFalloff;
        [ArrayElementTitle, SerializeField] private List<FootstepCollection> collections = new();

        [Space(10)]

        [SerializeField] private LayerMask raycastLayerMask;

        private AudioOverrides audioSpatialOverride;

        private Dictionary<FootstepType, FootstepCollection> dictionary = new();

        public LayerMask RaycastLayerMask => raycastLayerMask;

        public override string Name    => "Footstep Manager";
        public override uint LoadOrder => SubsystemLoadOrders.FootstepManager;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple '{nameof(FootstepManager)}' singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(this);
            foreach(FootstepCollection collection in collections) {
                if(dictionary.ContainsKey(collection.type)) {
                    Debug.LogError($"Duplicate footstep type '{collection.type}' found. Skipping.");
                    continue;
                }

                collection.Initialize();
                dictionary.Add(collection.type, collection);
            }

            audioSpatialOverride = AudioOverrides.NoSpatialBlend;
            Instance = this;
            return UniTask.CompletedTask;
        }

        public static void CreateFootstep(FootstepParams fParams, FootstepMaterial material, Vector3 position, bool spatialized)
        {
            FootstepType footstepType = fParams.footstepType;
            if(!Instance.dictionary.TryGetValue(footstepType, out FootstepCollection collection)) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"No footsteps for type {footstepType} found.");
                }
                return;
            }
            if(!collection.Dictionary.TryGetValue(material, out FootstepData fData)) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"No footstep for material {material} found in footstep type {footstepType}.");
                }
                return;
            }

            AudioSource source = AudioPool.Retrieve(position, fData.Params, overrides: spatialized ? null : Instance.audioSpatialOverride);
            source.outputAudioMixerGroup = Instance.mixerGroup;
            if(!ReferenceEquals(fParams.additionalSound, null)) {
                CreateAdditionalSound(position, fParams);
            }
            if(fData.CreateEffect) {
                CreateEffect(position, fData);
            }
            if(fParams.impulse > 0.001f) {
                CreateImpulse(position, fParams);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CreateAdditionalSound(Vector3 position, FootstepParams fParams)
        {
            AudioSource addSource = AudioPool.Retrieve(position, fParams.additionalSound);
            addSource.outputAudioMixerGroup = Instance.mixerGroup;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CreateEffect(Vector3 position, FootstepData data)
        {
            PoolManager.Retrieve(data.EffectPoolParams.Prefab, position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CreateImpulse(Vector3 position, FootstepParams fParams)
        {
            PoolableGameObject go = PoolManager.Retrieve(Instance.impulseEffectPrefab, position);
            go.GetComponent<CameraImpulseEffectGO>().Initialize(fParams.impulseRadius, fParams.impulse * 0.35f, fParams.impulse, Instance.impulseFalloff);
        }

        Material[] IMaterialPreloaderDataProvider.GetMaterials()
        {
            List<Material> materials = new();
            foreach(FootstepCollection collection in collections) {
                foreach(FootstepData fData in collection.footstepMaterials) {
                    if(fData.EffectPoolParams == null || fData.EffectPoolParams.Prefab == null)
                        continue;

                    foreach(Renderer render in fData.EffectPoolParams.Prefab.GetComponentsInChildren<Renderer>(true)) {
                        materials.AddRange(render.sharedMaterials);
                    }
                }
            }
            return materials.ToArray();
        }
    }

    [Serializable]
    public class FootstepCollection : IArrayElementTitle
    {
        public FootstepType type;
        [ArrayElementTitle] public List<FootstepData> footstepMaterials = new();

        public Dictionary<FootstepMaterial, FootstepData> Dictionary { get; private set; } = new();

        string IArrayElementTitle.Name => type.ToString();

        public void Initialize()
        {
            foreach(FootstepData data in footstepMaterials) {
                foreach(FootstepMaterial mat in data.Materials) {
                    if(!Dictionary.TryAdd(mat, data)) {
                        Debug.LogError($"Duplicate footstep data for material type '{data.Materials}' found.");
                    }
                }
                if(data.CreateEffect && !PoolManager.IsPrefabPooled(data.EffectPoolParams.Prefab)) {
                    PoolManager.RegisterPool(new PoolManager.GameObjectPool(data.EffectPoolParams), true);
                }
            }
        }
    }

    [Serializable]
    public class FootstepData : IArrayElementTitle
    {
        [field: SerializeField] public FootstepMaterial[] Materials { get; private set; }
        [field: SerializeField] public AudioParams Params           { get; private set; }
        [field: SerializeField] public bool CreateEffect            { get; private set; }
        
        [field: SerializeField, ShowIf(nameof(CreateEffect))] 
        public PoolParams EffectPoolParams                          { get; private set; }

        string IArrayElementTitle.Name {
            get {
                string s = "";
                for(int i = 0; i < Materials.Length; i++) {
                    s += Materials[i].ToString();
                    if(i + 1 < Materials.Length) {
                        s += ", ";
                    }
                }
                return s;
            }
        }
    }

    public enum FootstepType : byte
    {
        Human        = 0,
        SmallMonster = 1,
        LargeMonster = 2,
    }

    public enum FootstepMaterial : byte
    {
        Default       = 0,
        Grass         = 1,
        Stone         = 2,
        Metal         = 3,
        Concrete      = 4,
        Dirt          = 5
    }

}
