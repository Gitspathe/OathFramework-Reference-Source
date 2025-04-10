using OathFramework.ProcGen.Layers;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace OathFramework.ProcGen
{
    [CreateAssetMenu(fileName = "Tile Rule", menuName = "ScriptableObjects/ProcGen/Tile Rule", order = 1)]
    public class TileRule : ScriptableObject, ITileSource
    {
        [field: SerializeField] public List<Variant> TileVariants { get; private set; }
        
        [field: SerializeField] public ushort RandomWeight        { get; private set; } = 100;
        
        [field: Space(10)]
        [field: SerializeField] public List<ActionSet> ActionSets { get; private set; }

        public TileRuleData GetTileRuleData(Map.Tile tile, Map map, ProcGenLayer layer)
        {
            TileRuleData tileData = new TileRuleData().Initialize(this, layer);
            foreach(ActionSet set in ActionSets) {
                if(!set.Evaluate(this, tileData, tile, map))
                    continue;
                
                set.Execute(this, tileData, tile, map);
            }
            return tileData;
        }

        /// <summary>
        /// Attempts to retrieve a tile to fill the specified space. Primarily used for 1x1 tiles, since this function is simple and does not read ahead.
        /// </summary>
        /// <param name="rand">FRandom rng.</param>
        /// <param name="tile">Tile coords to be filled.</param>
        /// <param name="map">The map.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="tileConfig">Returns the new tile config.</param>
        /// <param name="direction">Returns the direction of the tile to be placed.</param>
        /// <param name="ruleData">Returns RuleData which is populated with various computed fields.</param>
        /// <param name="replace">Whether replacing tiles is allowed.</param>
        /// <returns>True if a new tile config was found.</returns>
        public bool TryRetrieveNext(
            FRandom rand, 
            Map.Tile tile, 
            Map map,
            ProcGenLayer layer, 
            out MapTile tileConfig, 
            out Direction direction, 
            out TileRuleData ruleData,
            bool replace = false)
        {
            ruleData   = null;
            tileConfig = null;
            direction  = Direction.North;
            ruleData   = GetTileRuleData(tile, map, layer);
            if(ruleData == null || !ruleData.IsIncluded)
                return false;
            
            if(!replace) {
                ruleData = ruleData.StripImpossibleDirections(map, tile);
            }
            if(ruleData.Variants.Count == 0)
                return false;
            
            WeightedRandom<Variant> tileRand  = new(rand);
            WeightedRandom<Direction> dirRand = new(rand);
            foreach(Variant v in ruleData.Variants) {
                tileRand.Add(v, v.RandomWeight);
            }
            Variant found = tileRand.Retrieve();
            
            foreach(DirectionWeight dir in found.Directions) {
                dirRand.Add(dir.Direction, dir.RandomWeight);
            }
            tileConfig = found.Tile;
            direction  = dirRand.Retrieve();
            return true;
        }
        
        public bool TryRetrieveNext(
            FRandom rand, 
            Map.Tile tile, 
            Map map, 
            ProcGenLayer layer,
            out MapTile tileConfig, 
            out Direction direction,
            bool replace = false)
        {
            tileConfig = null;
            direction  = Direction.North;
            return TryRetrieveNext(rand, tile, map, layer, out tileConfig, out direction, out _, replace);
        }

        /// <summary>
        /// Attempts to retrieve a tile to fill the space defined by originTile, while accounting for large tiles (>1x1).
        /// To achieve this functionality, this method basically 'reads ahead' based on the largest X or Y size of all variants.
        /// </summary>
        /// <param name="rand">FRandom rng.</param>
        /// <param name="originTile">The origin tile coords which should be filled.</param>
        /// <param name="map">The map.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="tileConfig">Returns the new tile config.</param>
        /// <param name="tile">Returns the new tile space/coords where the tile should be placed.</param>
        /// <param name="direction">Returns the direction of the tile.</param>
        /// <param name="ruleData">Returns RuleData which is populated with various computed fields.</param>
        /// <param name="clampDirection">If true, clamps 'read ahead' direction to the passed value.</param>
        /// <returns>True if a new tile config was found.</returns>
        public bool TryRetrieveNextEx(
            FRandom rand, 
            Map.Tile originTile, 
            Map map,
            ProcGenLayer layer,
            out MapTile tileConfig,
            out Map.Tile tile,
            out Direction direction,
            out TileRuleData ruleData, 
            Direction? clampDirection = null)
        {
            // Tile, Coords, Tile Config.
            WeightedRandom<(Map.Tile, Direction, MapTile, TileRuleData ruleData)> tileRand = new(rand);
            tileConfig             = null;
            tile                   = null;
            direction              = Direction.North;
            ruleData               = null;
            (int x, int y) maxSize = (1, 1);
            GetMaxTileSize(out maxSize.x, out maxSize.y);

            if(clampDirection.HasValue) {
                switch(clampDirection.Value) {
                    case Direction.North: {
                        for(int y = originTile.Y + maxSize.y; y >= originTile.Y; y--) {
                            Check(originTile.X, y);
                        }
                    } break;
                    case Direction.East: {
                        for(int x = originTile.X + maxSize.x; x >= originTile.X; x--) {
                            Check(x, originTile.Y);
                        }
                    } break;
                    case Direction.South: {
                        for(int y = originTile.Y - maxSize.y; y <= originTile.Y; y++) {
                            Check(originTile.X, y);
                        }
                    } break;
                    case Direction.West: {
                        for(int x = originTile.X - maxSize.x; x <= originTile.X; x++) {
                            Check(x, originTile.Y);
                        }
                    } break;
                }
            } else {
                for(int x = originTile.X - maxSize.x; x <= originTile.X + maxSize.x; x++) {
                    for(int y = originTile.Y - maxSize.y; y <= originTile.Y + maxSize.y; y++) {
                        Check(x, y);
                    }
                }
            }
            if(tileRand.Count == 0)
                return false;

            (Map.Tile fTile, Direction fDir, MapTile fConf, TileRuleData rDat) = tileRand.Retrieve();
            tileConfig = fConf;
            tile       = fTile;
            direction  = fDir;
            ruleData   = rDat;
            return true;

            void Check(int tileX, int tileY)
            {
                Map.Tile t = map.GetTile(tileX, tileY);
                if(t == null)
                    return;
                
                List<(Map.Tile, Direction, MapTile, int)> l = new();
                TileRuleData ruleData = GetTileRuleData(t, map, layer).StripImpossibleDirections(map, t);
                if(ruleData == null || !ruleData.IsIncluded || ruleData.Variants.Count == 0)
                    return;

                foreach(Variant v in ruleData.Variants) {
                    foreach(DirectionWeight dir in v.Directions) {
                        l.Add((t, dir.Direction, v.Tile, v.RandomWeight + dir.RandomWeight));
                    }
                }
                foreach((Map.Tile tile, Direction dir, MapTile conf, int weight) tuple in l) {
                    tileRand.Add((tuple.tile, tuple.dir, tuple.conf, ruleData), tuple.weight);
                }
            }
        }
        
        public bool TryRetrieveNextEx(
            FRandom rand, 
            Map.Tile originTile, 
            Map map,
            ProcGenLayer layer,
            out MapTile tileConfig,
            out Map.Tile tile,
            out Direction direction, 
            Direction? clampDirection = null)
        {
            tileConfig = null;
            tile       = null;
            direction  = Direction.North;
            return TryRetrieveNextEx(rand, originTile, map, layer, out tileConfig, out tile, out direction, out _, clampDirection);
        }

        public void GetMaxTileSize(out int largestX, out int largestY)
        {
            largestX = 0;
            largestY = 0;
            foreach(Variant v in TileVariants) {
                if(v.Tile.TilesX > largestX) {
                    largestX = v.Tile.TilesX;
                }
                if(v.Tile.TilesY > largestY) {
                    largestY = v.Tile.TilesY;
                }
            }
        }

        public bool IsSourceOf(MapTile tile)
        {
            if(tile == null)
                return false;

            return tile.TileData.SourceRule == this;
        }
    }

    public class TileRuleData
    {
        public TileRule Rule          { get; private set; }
        public ProcGenLayer Layer     { get; private set; }
        
        public List<Variant> Variants { get; set; } = new();
        public ushort RandomWeight    { get; private set; }
        public bool IsIncluded        { get; private set; }

        public TileRuleData Initialize(TileRule rule, ProcGenLayer layer)
        {
            Rule  = rule;
            Layer = layer;
            Variants.Clear();
            foreach(Variant v in rule.TileVariants) {
                Variants.Add(v.DeepCopy());
            }
            RandomWeight = rule.RandomWeight;
            IsIncluded   = true;
            return this;
        }
        
        public void Clear()
        {
            Variants.Clear();
            RandomWeight = 1;
            IsIncluded   = true;
        }
        
        public TileRuleData StripImpossibleDirections(Map map, Map.Tile mapTile)
        {
            List<Variant> newVariants = new();
            foreach(Variant variant in Variants) {
                Variant newV = variant.DeepCopy();
                newV.Directions.Clear();
                foreach(DirectionWeight dir in variant.Directions) {
                    if(map.IsSpaceFree(mapTile.Parent ?? mapTile, dir.Direction, variant.Tile)) {
                        newV.Directions.Add(dir.DeepCopy());
                    }
                }
                if(newV.Directions.Count > 0) {
                    newVariants.Add(newV);
                }
            }
            Variants.Clear();
            Variants.AddRange(newVariants);
            return this;
        }
        
        public TileRuleData IncludeVariant(Variant variant)
        {
            foreach(Variant v in Variants) {
                if(v.Tile == variant.Tile) {
                    v.RandomWeight = RandomWeight;
                    return this;
                }
            }
            Variants.Add(variant);
            return this;
        }

        public TileRuleData ExcludeMapVariant(MapTile tile)
        {
            for(int i = 0; i < Variants.Count; i++) {
                if(Variants[i].Tile == tile) {
                    Variants.RemoveAt(i);
                    return this;
                }
            }
            return this;
        }

        public TileRuleData SetVariantWeight(MapTile tile, ushort weight)
        {
            foreach(Variant v in Variants) {
                if(v.Tile == tile) {
                    v.RandomWeight = weight;
                }
            }
            return this;
        }
        
        public TileRuleData MultiplyVariantWeight(MapTile tile, float weightMult)
        {
            foreach(Variant v in Variants) {
                if(v.Tile == tile) {
                    v.RandomWeight = (ushort)Mathf.Clamp(v.RandomWeight * weightMult, 0, ushort.MaxValue);
                }
            }
            return this;
        }
        
        public TileRuleData IncludeDirection(DirectionWeight direction)
        {
            foreach(Variant v in Variants) {
                foreach(DirectionWeight weight in v.Directions) {
                    if(weight.Direction == direction.Direction) {
                        weight.RandomWeight = direction.RandomWeight;
                    }
                    return this;
                }
                v.Directions.Add(direction.DeepCopy());
            }
            return this;
        }

        public TileRuleData IncludeDirection(MapTile tile, DirectionWeight direction)
        {
            Variant foundVariant = null;
            foreach(Variant v in Variants) {
                if(v.Tile == tile) {
                    foundVariant = v;
                    break;
                }
            }
            if(foundVariant == null)
                return this;
            
            foreach(DirectionWeight weight in foundVariant.Directions) {
                if(weight.Direction == direction.Direction) {
                    weight.RandomWeight = direction.RandomWeight;
                }
                return this;
            }
            foundVariant.Directions.Add(direction.DeepCopy());
            return this;
        }
        
        public TileRuleData ExcludeDirection(Direction direction)
        {
            foreach(Variant v in Variants) {
                for(int i = 0; i < v.Directions.Count; i++) {
                    if(direction == v.Directions[i].Direction) {
                        v.Directions.RemoveAt(i);
                        break;
                    }
                }
            }
            return this;
        }

        public TileRuleData ExcludeDirection(MapTile tile, Direction direction)
        {
            Variant foundVariant = null;
            foreach(Variant v in Variants) {
                if(v.Tile == tile) {
                    foundVariant = v;
                    break;
                }
            }
            if(foundVariant == null)
                return this;
            
            for(int i = 0; i < foundVariant.Directions.Count; i++) {
                if(direction == foundVariant.Directions[i].Direction) {
                    foundVariant.Directions.RemoveAt(i);
                    return this;
                }
            }
            return this;
        }
        
        public TileRuleData IncludeDirection(Direction direction, ushort weight)
        {
            foreach(Variant v in Variants) {
                bool exists = false;
                foreach(DirectionWeight dir in v.Directions) {
                    if(dir.Direction != direction)
                        continue;

                    dir.RandomWeight = weight;
                    exists           = true;
                }
                if(!exists) {
                    v.Directions.Add(new DirectionWeight(direction, weight));
                }
            }
            return this;
        }
        
        public TileRuleData SetDirectionWeight(Direction direction, ushort weight)
        {
            foreach(Variant v in Variants) {
                foreach(DirectionWeight dir in v.Directions) {
                    if(dir.Direction == direction) {
                        dir.RandomWeight = weight;
                    }
                }
            }
            return this;
        }

        public TileRuleData SetDirectionWeight(MapTile tile, Direction direction, ushort weight)
        {
            if(tile == null)
                throw new ArgumentNullException(nameof(tile));
            
            Variant foundVariant = null;
            foreach(Variant v in Variants) {
                if(v.Tile == tile) {
                    foundVariant = v;
                    break;
                }
            }
            if(foundVariant == null)
                return this;

            bool exists = false;
            foreach(DirectionWeight dir in foundVariant.Directions) {
                if(dir.Direction == direction) {
                    dir.RandomWeight = weight;
                    exists           = true;
                }
            }
            if(!exists) {
                foundVariant.Directions.Add(new DirectionWeight(direction, weight));
            }
            return this;
        }
        
        public TileRuleData MultiplyDirectionWeight(Direction direction, float weightMult)
        {
            foreach(Variant v in Variants) {
                foreach(DirectionWeight dir in v.Directions) {
                    if(dir.Direction == direction) {
                        dir.RandomWeight = (ushort)Mathf.Clamp(dir.RandomWeight * weightMult, 0, ushort.MaxValue);
                    }
                }
            }
            return this;
        }

        public TileRuleData MultiplyDirectionWeight(MapTile tile, Direction direction, float weightMult)
        {
            if(tile == null)
                throw new ArgumentNullException(nameof(tile));
            
            Variant foundVariant = null;
            foreach(Variant v in Variants) {
                if(v.Tile == tile) {
                    foundVariant = v;
                    break;
                }
            }
            if(foundVariant == null)
                return this;
            
            foreach(DirectionWeight dir in foundVariant.Directions) {
                if(dir.Direction == direction) {
                    dir.RandomWeight = (ushort)Mathf.Clamp(dir.RandomWeight * weightMult, 0, ushort.MaxValue);
                }
            }
            return this;
        }

        public TileRuleData SetRandomWeight(ushort weight)
        {
            RandomWeight = weight;
            return this;
        }

        public TileRuleData MultiplyRandomWeight(float weightMult)
        {
            RandomWeight = (ushort)Mathf.Clamp(RandomWeight * weightMult, 0, ushort.MaxValue);
            return this;
        }
        
        public TileRuleData Include()
        {
            IsIncluded = true;
            return this;
        }

        public TileRuleData Exclude()
        {
            IsIncluded = false;
            return this;
        }
    }

    [Serializable]
    public class Variant
    {
        [field: SerializeField] public MapTile Tile                     { get; private set; }
        [field: SerializeField] public ushort RandomWeight              { get; set; } = 100;
        [field: SerializeField] public List<DirectionWeight> Directions { get; private set; } = new() {
            new(Direction.North), new(Direction.East), new(Direction.South), new(Direction.West)
        };

        public Variant DeepCopy()
        {
            Variant copy = new() {
                Tile         = Tile, 
                RandomWeight = RandomWeight,
                Directions   = new List<DirectionWeight>()
            };
            for(int i = 0; i < Directions.Count; i++) {
                copy.Directions.Add(Directions[i].DeepCopy());
            }
            return copy;
        }
    }

    [Serializable]
    public class DirectionWeight
    {
        [field: SerializeField] public Direction Direction { get; private set; }
        [field: SerializeField] public ushort RandomWeight { get; set; } = 100;
        
        public DirectionWeight() {}

        public DirectionWeight(Direction direction, ushort weight = 100)
        {
            Direction    = direction;
            RandomWeight = weight;
        }

        public DirectionWeight DeepCopy()
        {
            return new DirectionWeight(Direction, RandomWeight);
        }
    }

    [Serializable]
    [SuppressMessage("ReSharper", "UseArrayEmptyMethod")]
    public class ActionSet
    {
        [field: Title("Action Set", bold: true)]
        
        [field: SerializeField]
        public bool Conditional           { get; private set; }
        
        [field: SerializeReference, InlineProperty, HideLabel, ShowIf("@Conditional")]
        public List<Condition> Conditions { get; private set; } = new();

        [field: SerializeField, ShowIf("@Conditional")]
        public CompositeType Composite    { get; private set; }

        [field: SerializeReference, InlineProperty, HideLabel]
        public List<Action> Actions       { get; private set; } = new();

        public bool Evaluate(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            if(!Conditional)
                return true;
            
            bool ret = true;
            foreach(Condition c in Conditions) {
                ret = c.Evaluate(rule, data, tile, map);
                if((ret && Composite == CompositeType.Or) || (!ret && Composite == CompositeType.And))
                    break;
            }
            return ret;
        }

        public void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map)
        {
            foreach(Action action in Actions) {
                action.Execute(rule, data, tile, map);
            }
        }
    }

    [Serializable]
    public abstract class Condition
    {
        public abstract bool Evaluate(TileRule rule, TileRuleData data, Map.Tile tile, Map map);
    }

    [Serializable]
    public abstract class Action
    {
        public abstract void Execute(TileRule rule, TileRuleData data, Map.Tile tile, Map map);
    }
    
    public enum CompositeType
    {
        And, 
        Or
    }
}
