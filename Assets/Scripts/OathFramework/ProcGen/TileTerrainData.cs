using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace OathFramework.ProcGen
{
    public class TileTerrainData : MonoBehaviour
    {
        [field: SerializeField] public TerrainTemplate Template { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public Terrain Terrain          { get; set; }
        [field: SerializeField] public string DataPath          { get; set; }
        
        [field: SerializeField] public ushort OffsetX           { get; private set; }
        [field: SerializeField] public ushort OffsetY           { get; private set; }
        
        public TerrainData TerrainData => Terrain?.terrainData;

        private void OnValidate()
        {
            Terrain = GetComponent<Terrain>();
            if(Terrain != null && Terrain.terrainData != null && Template != null && Template.Data != null) {
                ProcGenUtil.ApplyTemplate(Template, Terrain.terrainData);
            }
        }

        public void FreeInitTerrainMemory()
        {
            if(Terrain.gameObject.TryGetComponent(out TerrainCollider col)) {
                Destroy(col);
            }
        }

        public void FreeIntermediateTerrainMemory()
        {
            Destroy(Terrain);
        }
    }
}
