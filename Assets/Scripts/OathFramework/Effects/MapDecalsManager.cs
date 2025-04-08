using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.Effects
{
    public class MapDecalsManager : Subsystem
    {
        public override string Name    => "Map Decals";
        public override uint LoadOrder => SubsystemLoadOrders.MapDecalsManager;
        
        [field: SerializeField] public LayerMask RaycastLayers { get; private set; }
        [field: SerializeField] public int MaxDecals           { get; private set; } = 50;
        
        [SerializeField] private MapDecalParamsCollection[] collection;
        
        private static Database database = new();
        private static Dictionary<(MapDecalParams, bool, Color?), Material> materialLookup = new();
        
        public static MapDecalsManager Instance { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(EffectManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
            
            foreach(MapDecalParamsCollection mdParams in collection) {
                foreach(MapDecalParams @params in mdParams.collection) {
                    if(!Register(@params, out ushort _))
                        continue;
                    
                    PoolManager.RegisterPool(new PoolManager.GameObjectPool(@params.PrefabPool), true);
                }
            }
            return UniTask.CompletedTask;
        }
        
        public static bool Register(MapDecalParams decal, out ushort id)
        {
            id = default;
            if(database.RegisterWithID(decal.Key, decal, decal.DefaultID)) {
                decal.ID = decal.DefaultID;
                id       = decal.ID;
                return true;
            }
            if(database.Register(decal.Key, decal, out ushort retID)) {
                decal.ID = retID;
                id       = decal.ID;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(MapDecalParams)} '{decal.Key}'.");
            return false;
        }

        public static bool TryGetMapDecalParams(string key, out MapDecalParams decalParams, out ushort id) 
            => database.TryGet(key, out decalParams, out id);

        public static bool TryGetMapDecalParams(ushort id, out MapDecalParams decalParams, out string key) 
            => database.TryGet(id, out decalParams, out key);

        public static Material GetDerivedMaterial(MapDecalParams @params, Material originalMat, bool highQuality, Color? color, out bool isNew)
        {
            isNew = false;
            if(materialLookup.TryGetValue((@params, highQuality, color), out Material material))
                return material;

            isNew = true;
            Material newMat = Instantiate(originalMat);
            materialLookup.Add((@params, highQuality, color), newMat);
            return newMat;
        }

        public static GameObject CreateMapDecal(MapDecalParams @params, Vector3 position, Quaternion rotation = default, Color? color = null)
        {
            PoolableGameObject go = PoolManager.Retrieve(@params.PrefabPool.Prefab, position, rotation);
            if(go.TryGetComponent(out IColorable colorable)) {
                colorable.SetColor(color);
            }
            return go.gameObject;
        }
        
        private sealed class Database : Database<string, ushort, MapDecalParams>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
