using OathFramework.Utility;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OathFramework.ProcGen
{
    public class MapTile : MonoBehaviour, ITileSource
    {
        [field: SerializeField] public string Key                     { get; private set; }
        [field: SerializeField] public ushort DefaultID               { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public float MapTileSize              { get; private set; } = 20.0f;
        [field: SerializeField] public int TilesX                     { get; private set; } = 1;
        [field: SerializeField] public int TilesY                     { get; private set; } = 1;
        [field: SerializeField] public SubVariantSet[] SubVariantSets { get; private set; }
        
        public Map Map                       { get; private set; }
        public Map.Tile TileData             { get; private set; }
        public TileTerrainData[] TerrainData { get; private set; } = Array.Empty<TileTerrainData>();

        private readonly LockableOrderedList<IOnTileInstantiated> onInstantiatedCallbacks        = new();
        private readonly LockableOrderedList<IOnAllTilesInstantiated> onAllInstantiatedCallbacks = new();

        private HashSet<Transform> allVariantTransforms = new();

        private void Awake()
        {
            TerrainData = GetComponentsInChildren<TileTerrainData>(true);
            foreach(IOnTileInstantiated callback in GetComponentsInChildren<IOnTileInstantiated>(true)) {
                onInstantiatedCallbacks.AddUnique(callback);
            }
            foreach(IOnAllTilesInstantiated callback in GetComponentsInChildren<IOnAllTilesInstantiated>(true)) {
                onAllInstantiatedCallbacks.AddUnique(callback);
            }
        }

        public void OnTileInstantiated(Map map, Map.Tile data)
        {
            Map      = map;
            TileData = data;
            foreach(TileTerrainData tData in TerrainData) {
                tData.FreeInitTerrainMemory();
            }
            onInstantiatedCallbacks.Lock();
            foreach(IOnTileInstantiated callback in onInstantiatedCallbacks.Current) {
                callback.OnTileInstantiated(map);
            }
            onInstantiatedCallbacks.Unlock();
        }

        public void OnAllTilesInstantiated(Map map)
        {
            onAllInstantiatedCallbacks.Lock();
            foreach(IOnAllTilesInstantiated callback in onAllInstantiatedCallbacks.Current) {
                callback.OnAllTilesInstantiated(map);
            }
            onAllInstantiatedCallbacks.Unlock();
        }

        public void SetupSubVariant(FRandom rand)
        {
            foreach(SubVariantSet svSet in SubVariantSets) {
                foreach(SubVariant sv in svSet.SubVariants) {
                    foreach(Transform t in sv.Objects) {
                        allVariantTransforms.Add(t);
                    }
                }
            }

            List<SubVariant> currentVariants = new();
            foreach(SubVariantSet svSet in SubVariantSets) {
                WeightedRandom<SubVariant> vRand = new(rand);
                foreach(SubVariant sv in svSet.SubVariants) {
                    vRand.Add(sv, sv.RandomWeight);
                }
                currentVariants.Add(vRand.Retrieve());
            }
            foreach(SubVariant sv in currentVariants) {
                foreach(Transform obj in sv.Objects) {
                    obj.gameObject.SetActive(true);
                }
            }
            FreeUnusedSubVariants(currentVariants);
        }

        private void FreeUnusedSubVariants(List<SubVariant> current)
        {
            HashSet<Transform> currentTransforms = new();
            foreach(SubVariant sv in current) {
                foreach(Transform obj in sv.Objects) {
                    currentTransforms.Add(obj);
                }
            }
            foreach(Transform t in allVariantTransforms) {
                if(!currentTransforms.Contains(t)) {
                    Destroy(t.gameObject);
                }
            }
            allVariantTransforms.Clear();
            allVariantTransforms = null;
        }

        public bool IsSourceOf(MapTile tile) => tile != null && tile.Key == Key;

        [Serializable]
        public class SubVariant
        {
            [field: SerializeField] public Transform[] Objects { get; private set; }
            [field: SerializeField] public ushort RandomWeight { get; private set; } = 100;
        }

        [Serializable]
        public class SubVariantSet
        {
            [field: SerializeField] public SubVariant[] SubVariants { get; private set; }
        }
    }

    public interface ITileSource
    {
        bool IsSourceOf(MapTile tile);
    }

    public interface IOnTileInstantiated : ILockableOrderedListElement
    {
        void OnTileInstantiated(Map map);
    }

    public interface IOnAllTilesInstantiated : ILockableOrderedListElement
    {
        void OnAllTilesInstantiated(Map map);
    }
}
