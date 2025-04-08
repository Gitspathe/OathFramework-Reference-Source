using OathFramework.Core;
using OathFramework.ProcGen.Layers;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.ProcGen
{
    [CreateAssetMenu(fileName = "Map Config", menuName = "ScriptableObjects/ProcGen/Map Config", order = 1)]
    public class MapConfig : ScriptableObject
    {
        [field: SerializeField] public string Key                          { get; private set; }
        [field: SerializeField] public ushort DefaultID                    { get; private set; }

        [field: Space(10)]
        
        [field: SerializeField] public EnvironmentSettings Environment     { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public TerrainTemplate TerrainTemplate     { get; private set; }
        [field: SerializeField] public float TileSize                      { get; private set; } = 20.0f;
        [field: SerializeField] public int TileHeightmapRes                { get; private set; } = 65;
        [field: SerializeField] public int TileDetailmapRes                { get; private set; } = 65;
        [field: SerializeField] public int TileAlphamapRes                 { get; private set; } = 65;

        [field: Space(10)]
        
        [field: InfoBox("For terrain to work properly, Tiles-1 * TileHeightmapRes must equal a multiple of 2 (512, 1024, 2048, etc...)")]
        [field: SerializeField] public ushort Tiles                        { get; private set; } = 10;
        
        [field: Space(10), SerializeField, InlineEditor]
        public List<ProcGenLayerSO> ProcGenLayers                          { get; private set; }
        
        public ushort ID { get; set; }

        public MapConfig DeepCopy()
        {
            List<ProcGenLayerSO> layers = new();
            foreach(ProcGenLayerSO layer in ProcGenLayers) {
                layers.Add(Instantiate(layer));
            }
            MapConfig conf     = Instantiate(this);
            conf.ProcGenLayers = layers;
            conf.Environment   = Instantiate(Environment);
            return conf;
        }
    }
    
    public enum Direction
    {
        North = 0,
        East  = 1,
        South = 2,
        West  = 3,
    }

    public enum DirectionEx
    {
        N  = 0,
        NE = 1,
        E  = 2,
        SE = 3,
        S  = 4,
        SW = 5,
        W  = 6,
        NW = 7,
    }
}
