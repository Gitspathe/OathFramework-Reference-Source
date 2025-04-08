using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.ProcGen;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem 
{ 

    public class TerrainFootstepSurface : MonoBehaviour, IFootstepSurface, IPostInstantiationCallback
    {
        [SerializeField] private bool initializeImmediately = true;
        [SerializeField] private List<TerrainFootstepMaterial> materials = new();

        private Dictionary<int, FootstepMaterial> materialLookup = new();
        private Terrain terrain;

        private TerrainData terrainData;
        private int mapWidth;
        private int mapHeight;
        private float[,,] splatMapData;
        private int numTextures;
        
        uint ILockableOrderedListElement.Order => 100;

        private void Awake()
        {
            if(initializeImmediately) {
                Initialize();
            }
        }

        private void Initialize()
        {
            terrain      = GetComponent<Terrain>();
            terrainData  = terrain.terrainData;
            mapWidth     = terrainData.alphamapWidth;
            mapHeight    = terrainData.alphamapHeight;
            splatMapData = terrainData.GetAlphamaps(0, 0, mapWidth, mapHeight);
            numTextures  = splatMapData.Length / (mapWidth * mapHeight);
            materialLookup.Clear();
            foreach(TerrainFootstepMaterial material in materials) {
                if(materialLookup.ContainsKey(material.splatmapIndex)) {
                    Debug.LogError($"Duplicate footstep for splatmap index '{material.splatmapIndex}' found on '{gameObject.name}'. Skipping.");
                    continue;
                }
                materialLookup.Add(material.splatmapIndex, material.material);
            }
        }

        private Vector3 ConvertToSplatMapCoordinate(Vector3 playerPos)
        {
            Vector3 vecRet      = new();
            Vector3 terPosition = terrain.transform.position;
            TerrainData data    = terrain.terrainData;
            vecRet.x = ((playerPos.x - terPosition.x) / data.size.x) * data.alphamapWidth;
            vecRet.z = ((playerPos.z - terPosition.z) / data.size.z) * data.alphamapHeight;
            return vecRet;
        }

        private int GetActiveTerrainTextureIndex(Vector3 position)
        {
            Vector3 terrainCord = ConvertToSplatMapCoordinate(position);
            int ret             = 0;
            float comp          = 0f;
            for(int i = 0; i < numTextures; i++) {
                if(comp < splatMapData[(int)terrainCord.z, (int)terrainCord.x, i]) { 
                    ret = i;
                }
            }
            return ret;
        }

        public FootstepMaterial GetFootstepMaterial(Vector3 position)
        {
            int index = GetActiveTerrainTextureIndex(position);
            if(!materialLookup.TryGetValue(index, out FootstepMaterial material)) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"No material found for splatmap index '{index}' found on '{gameObject.name}'");
                }
                return FootstepMaterial.Default;
            }
            return material;
        }
        
        UniTask IPostInstantiationCallback.OnPostTilesInstantiated(Stopwatch timer, Map map)
        {
            Initialize();
            return UniTask.CompletedTask;
        }

        [Serializable]
        public class TerrainFootstepMaterial
        {
            public int splatmapIndex;
            public FootstepMaterial material;
        }
    }

}
