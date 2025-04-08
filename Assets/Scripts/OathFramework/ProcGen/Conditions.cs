using System;
using OathFramework.ProcGen.Layers;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.ProcGen
{
    [Serializable]
    public class OtherTileCheck : Condition
    {
        [field: SerializeField] public Mode CheckType { get; private set; }
        
        [field: SerializeField, ShowIf("@CheckType == OtherTileCheck.Mode.TileDistance || CheckType == OtherTileCheck.Mode.TileExists")]
        public List<MapTile> Tiles         { get; private set; }
        
        [field: SerializeField, ShowIf("@CheckType == OtherTileCheck.Mode.TileFromRuleDistance || CheckType == OtherTileCheck.Mode.TileFromRuleExists")] 
        public List<TileRule> Rules        { get; private set; }
        
        [field: SerializeField, ShowIf("@CheckType == OtherTileCheck.Mode.TileFromLayerDistance || CheckType == OtherTileCheck.Mode.TileFromLayerExists")] 
        public List<ProcGenLayerSO> Layers { get; private set; }

        [field: SerializeField, ShowIf("@CheckType == OtherTileCheck.Mode.TileFromRuleDistance || CheckType == OtherTileCheck.Mode.TileFromLayerDistance || CheckType == OtherTileCheck.Mode.TileDistance")]
        public ushort Distance             { get; private set; } = 1;

        [field: SerializeField, ShowIf("@CheckType == OtherTileCheck.Mode.TileFromRuleDistance || CheckType == OtherTileCheck.Mode.TileFromLayerDistance || CheckType == OtherTileCheck.Mode.TileDistance")]
        public bool AllDirections          { get; private set; } = true;

        [field: SerializeField, ShowIf("@(CheckType == OtherTileCheck.Mode.TileFromRuleDistance || CheckType == OtherTileCheck.Mode.TileFromLayerDistance || CheckType == OtherTileCheck.Mode.TileDistance) && !AllDirections")]
        public List<DirectionEx> Directions { get; private set; } = new()
            { DirectionEx.NW, DirectionEx.N, DirectionEx.NE, DirectionEx.E, DirectionEx.SE, DirectionEx.S, DirectionEx.SW, DirectionEx.W };
        
        [field: SerializeField, ShowIf("@CheckType == OtherTileCheck.Mode.TileFromRuleDistance || CheckType == OtherTileCheck.Mode.TileFromLayerDistance || CheckType == OtherTileCheck.Mode.TileDistance")]
        public bool AllTileRotations       { get; private set; } = true;
        
        [field: SerializeField, ShowIf("@(CheckType == OtherTileCheck.Mode.TileFromRuleDistance || CheckType == OtherTileCheck.Mode.TileFromLayerDistance || CheckType == OtherTileCheck.Mode.TileDistance) && !AllTileRotations")]
        public List<Direction> TileRotations   { get; private set; } = new()
            { Direction.North, Direction.East, Direction.South, Direction.West };
        
        public override bool Evaluate(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            List<ITileSource> sources = new();
            switch(CheckType) {
                case Mode.TileDistance:
                    sources.AddRange(Tiles);
                    return map.CheckForTiles(tile, sources, AllDirections ? null : Directions, AllTileRotations ? null : TileRotations, Distance);
                case Mode.TileFromRuleDistance: {
                    sources.AddRange(Rules);
                    return map.CheckForTiles(tile, sources, AllDirections ? null : Directions, AllTileRotations ? null : TileRotations, Distance);
                }
                case Mode.TileFromLayerDistance: {
                    foreach(ProcGenLayerSO so in Layers) {
                        sources.Add(so.Data);
                    }
                    return map.CheckForTiles(tile, sources, AllDirections ? null : Directions, AllTileRotations ? null : TileRotations, Distance);
                }
                case Mode.TileExists: {
                    foreach(MapTile check in Tiles) {
                        foreach(Map.Tile t in map.Tiles) {
                            if(t.Prefab == check && (AllTileRotations || TileRotations.Contains(t.Rotation)))
                                return true;
                        }
                    }
                    return false;
                }
                case Mode.TileFromRuleExists: {
                    foreach(TileRule check in Rules) {
                        foreach(Map.Tile t in map.Tiles) {
                            if(t.SourceRule == check && (AllTileRotations || TileRotations.Contains(t.Rotation)))
                                return true;
                        }
                    }
                    return false;
                }
                case Mode.TileFromLayerExists: {
                    foreach(ProcGenLayerSO check in Layers) {
                        foreach(Map.Tile t in map.Tiles) {
                            if(t.SourceLayer == check.Data && (AllTileRotations || TileRotations.Contains(t.Rotation)))
                                return true;
                        }
                    }
                    return false;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum Mode : byte
        {
            TileDistance          = 0,
            TileFromRuleDistance  = 1,
            TileFromLayerDistance = 2,
            TileExists            = 3,
            TileFromRuleExists    = 4,
            TileFromLayerExists   = 5
        }
    }
}
