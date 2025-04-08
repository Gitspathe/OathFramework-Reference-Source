using Cysharp.Threading.Tasks;
using OathFramework.Utility;
using OathFramework.Extensions;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.ProcGen.Layers
{
    public class ProcGenRoads : ProcGenLayer
    {
        [field: SerializeField] public TileRule Straight              { get; private set; }
        [field: SerializeField] public TileRule Turn                  { get; private set; }
        [field: SerializeField] public TileRule ThreeWay              { get; private set; }
        [field: SerializeField] public TileRule FourWay               { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public TileRule StraightEnd           { get; private set; }
        [field: SerializeField] public TileRule StraightBorder        { get; private set; }
        
        [field: SerializeField, MinMaxSlider(0.0f, 1.0f, true), Space(20)]
        public Vector2 RandomStartX                                   { get; private set; } = new(0.25f, 0.75f);

        [field: SerializeField, MinMaxSlider(0.0f, 1.0f, true)]
        public Vector2 RandomStartY                                   { get; private set; } = new(0.25f, 0.75f);

        [field: SerializeField] public RoadType[] PossibleStartPieces { get; private set; } = {
            RoadType.ThreeWay, RoadType.FourWay
        };

        public override async UniTask Generate(FRandom rand, Map map)
        {
            if(!GenerateStart(rand, map, out Map.Tile startTile))
                return;
            
            GenerateNodes(startTile, rand, map);
        }

        private void GenerateNodes(Map.Tile startTile, FRandom rand, Map map)
        {
            foreach(Direction dir in GetPossibleRoadDirections(startTile)) {
                if(!map.TryGetTileRelative(startTile, dir, out Map.Tile tile))
                    continue;

                ExecNodeIteration(dir, tile);
            }
            return;

            void ExecNodeIteration(Direction direction, Map.Tile tile, int depth = 0)
            {
                // TODO: To generate three-ways, I need to check multiple adjacent tiles, relative to the tested tile.
                
                bool isBorder;
                switch(direction) {
                    case Direction.North: {
                        isBorder = tile.Y == map.SizeY - 1;
                    } break;
                    case Direction.East: {
                        isBorder = tile.X == map.SizeX - 1;
                    } break;
                    case Direction.South: {
                        isBorder = tile.Y == 0;
                    } break;
                    case Direction.West: {
                        isBorder = tile.X == 0;
                    } break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }
                TileRule rule;
                if(!isBorder && depth == 4) {
                    rule  = FourWay;
                    depth = 0;
                } else {
                    rule = isBorder ? StraightBorder : Straight;
                    depth++;
                }
                if(!rule.TryRetrieveNext(rand, tile, map, this, out MapTile conf, out Direction _))
                    return;
                
                map.SetTile(tile, direction, conf, rule, this);
                foreach(Direction d in GetPossibleRoadDirections(tile)) {
                    if(!map.TryGetTileRelative(tile, d, out Map.Tile newTile))
                        continue;
                    
                    ExecNodeIteration(d, newTile, depth);
                }
            }
        }

        private TileRule GetRandomStartTileRule(FRandom rand, Map map)
        {
            WeightedRandom<TileRule> tRand = new(rand);
            if(PossibleStartPieces.Contains(RoadType.Straight)) {
                tRand.Add(Straight, Straight.RandomWeight);
            }
            if(PossibleStartPieces.Contains(RoadType.Turn)) {
                tRand.Add(Turn, Turn.RandomWeight);
            }
            if(PossibleStartPieces.Contains(RoadType.ThreeWay)) {
                tRand.Add(ThreeWay, ThreeWay.RandomWeight);
            }
            if(PossibleStartPieces.Contains(RoadType.FourWay)) {
                tRand.Add(FourWay, FourWay.RandomWeight);
            }
            if(PossibleStartPieces.Contains(RoadType.StraightEnd)) {
                tRand.Add(StraightEnd, StraightEnd.RandomWeight);
            }
            if(PossibleStartPieces.Contains(RoadType.StraightBorder)) {
                tRand.Add(StraightBorder, StraightBorder.RandomWeight);
            }
            return tRand.Retrieve();
        }
        
        private bool GenerateStart(FRandom rand, Map map, out Map.Tile startTile)
        {
            startTile              = null;
            TileRule startTileRule = GetRandomStartTileRule(rand, map);
            MapTile startTileConf  = null;
            if(startTileRule == null) {
                Debug.LogError($"Failed to find a valid starting tile rule.");
                return false;
            }
            
            int x1 = (int)(map.SizeX * RandomStartX.x);
            int x2 = (int)(map.SizeX * RandomStartX.y);
            int y1 = (int)(map.SizeY * RandomStartY.x);
            int y2 = (int)(map.SizeY * RandomStartY.y);
            List<(int, int)> startCoords = new();
            for(int x = x1; x <= x2; x++) {
                for (int y = y1; y <= y2; y++) {
                    startCoords.Add((x, y));
                }
            }
            Randomize(rand, startCoords);

            bool foundStart = false;
            while(true) {
                if(startCoords.Count == 0)
                    break;

                (int x, int y) = startCoords[0];
                startTile      = map.GetTile(x, y);
                startCoords.RemoveAt(0);
                if(startTile == null || !startTileRule.TryRetrieveNext(rand, startTile, map, this, out startTileConf, out Direction _, out TileRuleData _))
                    continue;

                foundStart = true;
                break;
            }
            if(!foundStart) {
                Debug.LogError($"Failed to find starting coords on layer {nameof(ProcGenFiller)}.");
                return false;
            }
            map.SetTile(startTile, Direction.North, startTileConf, startTileRule, this);
            return true;
        }
        
        private void Randomize(FRandom rand, List<(int, int)> coords)
        {
            List<(int, int)> copy = new();
            while(coords.Count > 0) {
                int i = rand.Int(coords.Count);
                copy.Add(coords[i]);
                coords.RemoveAt(i);
            }
            coords.AddRange(copy);
        }

        private Direction[] GetPossibleRoadDirections(Map.Tile tile)
        {
            if(tile.SourceLayer != this) {
                Debug.LogError($"Tile at {tile.X}, {tile.Y} does not belong to this road layer. The operation is aborted.");
                return Array.Empty<Direction>();
            }

            Direction dir = tile.Rotation;
            if(tile.SourceRule == Straight)
                return GetPossibleRoadDirections(RoadType.Straight, dir);
            if(tile.SourceRule == Turn)
                return GetPossibleRoadDirections(RoadType.Turn, dir);
            if(tile.SourceRule == ThreeWay)
                return GetPossibleRoadDirections(RoadType.ThreeWay, dir);
            if(tile.SourceRule == FourWay)
                return GetPossibleRoadDirections(RoadType.FourWay, dir);
            if(tile.SourceRule == StraightEnd)
                return GetPossibleRoadDirections(RoadType.StraightEnd, dir);
            if(tile.SourceRule == StraightBorder)
                return GetPossibleRoadDirections(RoadType.StraightBorder, dir);
            
            return Array.Empty<Direction>();
        }

        private Direction[] GetPossibleRoadDirections(RoadType type, Direction dir)
        {
            switch(type) {
                case RoadType.StraightBorder: 
                case RoadType.Straight: {
                    switch(dir) {
                        case Direction.North: {
                            return new[] { Direction.North, Direction.South };
                        }
                        case Direction.East: {
                            return new[] { Direction.East, Direction.West };
                        }
                        case Direction.South: {
                            return new[] { Direction.North, Direction.South };
                        }
                        case Direction.West: {
                            return new[] { Direction.East, Direction.West };
                        }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
                    }
                }
                case RoadType.Turn: {
                    switch(dir) {
                        case Direction.North: {
                            return new[] { Direction.South, Direction.East };
                        }
                        case Direction.East: {
                            return new[] { Direction.West, Direction.South };
                        }
                        case Direction.South: {
                            return new[] { Direction.North, Direction.West };
                        }
                        case Direction.West: {
                            return new[] { Direction.East, Direction.North };
                        }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
                    }
                }
                case RoadType.ThreeWay: {
                    switch(dir) {
                        case Direction.North: {
                            return new[] { Direction.South, Direction.East, Direction.West };
                        }
                        case Direction.East: {
                            return new[] { Direction.West, Direction.North, Direction.South };
                        }
                        case Direction.South: {
                            return new[] { Direction.North, Direction.East, Direction.West };
                        }
                        case Direction.West: {
                            return new[] { Direction.East, Direction.North, Direction.South };
                        }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
                    }
                }
                case RoadType.FourWay: {
                    return new[] { Direction.North, Direction.East, Direction.South, Direction.West };
                }
                case RoadType.StraightEnd: {
                    return Array.Empty<Direction>();
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public enum RoadType
        {
            Straight, Turn, ThreeWay, FourWay, StraightEnd, StraightBorder
        }
    }
}
