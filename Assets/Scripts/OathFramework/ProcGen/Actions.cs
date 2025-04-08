using Sirenix.OdinInspector;
using UnityEngine;
using System;

namespace OathFramework.ProcGen
{
    [Serializable]
    public class Include : Action
    {
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            data.Include();
        }
    }
    
    [Serializable]
    public class Exclude : Action
    {
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            data.Exclude();
        }
    }
    
    [Serializable]
    public class IncludeVariant : Action
    {
        [field: SerializeField] public Variant[] Variants { get; private set; }
        
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            foreach(Variant v in Variants) {
                data.IncludeVariant(v);
            }
        }
    }
    
    [Serializable]
    public class ExcludeMapTile : Action
    {
        [field: SerializeField] public MapTile[] Tiles { get; private set; }
        
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            foreach(MapTile v in Tiles) {
                data.ExcludeMapVariant(v);
            }
        }
    }
    
    [Serializable]
    public class SetVariantWeight : Action
    {
        [field: SerializeField] public MapTile[] Tiles { get; private set; }
        [field: SerializeField] public ushort Weight   { get; private set; } = 100;
        
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            foreach(MapTile t in Tiles) {
                data.SetVariantWeight(t, Weight);
            }
        }
    }
    
    [Serializable]
    public class MultiplyVariantWeight : Action
    {
        [field: SerializeField] public MapTile[] Tiles { get; private set; }
        [field: SerializeField] public float Value     { get; private set; } = 1.0f;
        
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            foreach(MapTile t in Tiles) {
                data.MultiplyVariantWeight(t, Value);
            }
        }
    }
    
    [Serializable]
    public class SetRuleWeight : Action
    {
        [field: SerializeField] public ushort Weight { get; private set; } = 100;
        
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            data.SetRandomWeight(Weight);
        }
    }
    
    [Serializable]
    public class MultiplyRuleWeight : Action
    {
        [field: SerializeField] public float Value { get; private set; } = 1.0f;
        
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            data.MultiplyRandomWeight(Value);
        }
    }
    
    [Serializable]
    public class IncludeDirectionWeight : Action
    {
        [field: SerializeField] public DirectionWeight[] Directions { get; private set; }
        [field: SerializeField] public bool AffectAllTiles          { get; private set; } = true;
        
        [field: SerializeField, ShowIf("@!AffectAllTiles")]
        public MapTile[] Tiles                                      { get; private set; }
        
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            if(AffectAllTiles || Tiles == null || Tiles.Length == 0) {
                foreach(DirectionWeight dir in Directions) {
                    data.IncludeDirection(dir);
                }
                return;
            }
            foreach(MapTile t in Tiles) {
                foreach(DirectionWeight dir in Directions) {
                    data.IncludeDirection(t, dir);
                }
            }
        }
    }
    
    [Serializable]
    public class ExcludeDirection : Action
    {
        [field: SerializeField] public Direction[] Directions { get; private set; }
        [field: SerializeField] public bool AffectAllTiles    { get; private set; } = true;
        
        [field: SerializeField, ShowIf("@!AffectAllTiles")]
        public MapTile[] Tiles                                { get; private set; }
        
        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            if(AffectAllTiles || Tiles == null || Tiles.Length == 0) {
                foreach(Direction dir in Directions) {
                    data.ExcludeDirection(dir);
                }
                return;
            }
            foreach(MapTile t in Tiles) {
                foreach(Direction dir in Directions) {
                    data.ExcludeDirection(t, dir);
                }
            }
        }
    }
    
    [Serializable]
    public class SetDirectionWeight : Action
    {
        [field: SerializeField] public Direction[] Directions { get; private set; } = {
            Direction.North, Direction.East, Direction.South, Direction.West
        };
        
        [field: SerializeField] public ushort Weight          { get; private set; } = 100;
        [field: SerializeField] public bool AffectAllTiles    { get; private set; } = true;
        
        [field: SerializeField, ShowIf("@!AffectAllTiles")]
        public MapTile[] Tiles                                { get; private set; }

        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            if(AffectAllTiles || Tiles == null || Tiles.Length == 0) {
                foreach(Direction dir in Directions) {
                    data.SetDirectionWeight(dir, Weight);
                }
                return;
            }
            foreach(MapTile t in Tiles) {
                foreach(Direction dir in Directions) {
                    data.SetDirectionWeight(t, dir, Weight);
                }
            }
        }
    }
    
    [Serializable]
    public class MultiplyDirectionWeight : Action
    {
        [field: SerializeField] public Direction[] Directions { get; private set; } = {
            Direction.North, Direction.East, Direction.South, Direction.West
        };
        
        [field: SerializeField] public float Value            { get; private set; } = 1.0f;
        [field: SerializeField] public bool AffectAllTiles    { get; private set; } = true;
        
        [field: SerializeField, ShowIf("@!AffectAllTiles")]
        public MapTile[] Tiles                                { get; private set; }

        public override void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            if(AffectAllTiles || Tiles == null || Tiles.Length == 0) {
                foreach(Direction dir in Directions) {
                    data.MultiplyDirectionWeight(dir, Value);
                }
                return;
            }
            foreach(MapTile t in Tiles) {
                foreach(Direction dir in Directions) {
                    data.MultiplyDirectionWeight(t, dir, Value);
                }
            }
        }
    }
    
    
}
