using Cysharp.Threading.Tasks;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.ProcGen.Layers
{
    [Serializable]
    public class ProcGenFiller : ProcGenLayer
    {
        [field: SerializeField] public Direction[] BranchDirections { get; private set; } = {
            Direction.North, Direction.East, Direction.South, Direction.West
        };
        
        [field: SerializeField] public List<TileRule> TileRules     { get; private set; }
        
        [field: SerializeField] public Modes Mode                   { get; private set; }

        [field: SerializeField, MinMaxSlider(0.0f, 1.0f, true), ShowIf("@Mode == Modes.Random"), Space(20)]
        public Vector2 RandomStartX                                 { get; private set; } = new(0.0f, 1.0f);

        [field: SerializeField, MinMaxSlider(0.0f, 1.0f, true), ShowIf("@Mode == Modes.Random")]
        public Vector2 RandomStartY                                 { get; private set; } = new(0.0f, 1.0f);
        
        public override async UniTask Generate(FRandom rand, Map map)
        {
            switch(Mode) {
                case Modes.Random: {
                    GenerateRandom(rand, map);
                } break;
                case Modes.FromPOI: {
                    GenerateFromPOI(rand, map);
                } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            FillEmpty(rand, map);
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

        private void FillEmpty(FRandom rand, Map map)
        {
            for(int x = 0; x < map.SizeX; x++) {
                for(int y = 0; y < map.SizeY; y++) {
                    Map.Tile t = map.GetTile(x, y);
                    if(t == null || !t.IsNull || !TryGet(rand, t, map, out MapTile conf, out Direction dir, out TileRuleData ruleData))
                        continue;
                    
                    map.SetTile(t, dir, conf, ruleData.Rule, this);
                }
            }
        }

        private void GenerateRandom(FRandom rand, Map map)
        {
            if(!GenerateStart(rand, map, out Map.Tile startTile))
                return;
            
            GenerateNodes(startTile, rand, map);
        }

        private bool TryGet(
            FRandom rand, 
            Map.Tile tile, 
            Map map, 
            out MapTile tileConfig,
            out Direction direction, 
            out TileRuleData ruleData,
            bool replace = false)
        {
            ruleData   = null;
            tileConfig = null;
            direction  = Direction.North;
            WeightedRandom<(MapTile, Direction, TileRuleData)> randVal = new(rand);
            foreach(TileRule rule in TileRules) {
                if(!rule.TryRetrieveNext(rand, tile, map, this, out tileConfig, out direction, out ruleData, replace))
                    continue;
                
                randVal.Add((tileConfig, direction, ruleData), ruleData.RandomWeight);
            }
            if(randVal.Count == 0)
                return false;

            (tileConfig, direction, ruleData) = randVal.Retrieve();
            return true;
        }
        
        private bool TryGetEx(
            FRandom rand, 
            Map.Tile tile, 
            Map map, 
            out MapTile tileConfig,
            out Map.Tile newTileSpot,
            out Direction direction, 
            out TileRuleData ruleData)
        {
            ruleData    = null;
            tileConfig  = null;
            newTileSpot = null;
            direction   = Direction.North;
            WeightedRandom<(MapTile, Map.Tile, Direction, TileRuleData)> randVal = new(rand);
            foreach(TileRule rule in TileRules) {
                if(!rule.TryRetrieveNextEx(rand, tile, map, this, out tileConfig, out newTileSpot, out direction, out ruleData))
                    continue;
                
                randVal.Add((tileConfig, newTileSpot, direction, ruleData), ruleData.RandomWeight);
            }
            if(randVal.Count == 0)
                return false;

            (tileConfig, newTileSpot, direction, ruleData) = randVal.Retrieve();
            return true;
        }

        private bool GenerateStart(FRandom rand, Map map, out Map.Tile startTile)
        {
            int x1 = (int)(map.SizeX * RandomStartX.x);
            int x2 = (int)(map.SizeX * RandomStartX.y);
            int y1 = (int)(map.SizeY * RandomStartY.x);
            int y2 = (int)(map.SizeY * RandomStartY.y);
            List<(int, int)> startCoords = new();
            MapTile startTileConf        = null;
            TileRuleData ruleData        = null;
            for(int x = x1; x <= x2; x++) {
                for (int y = y1; y <= y2; y++) {
                    startCoords.Add((x, y));
                }
            }
            Randomize(rand, startCoords);

            bool foundStart = false;
            startTile       = null;
            while(true) {
                if(startCoords.Count == 0)
                    break;

                (int x, int y) = startCoords[0];
                startTile      = map.GetTile(x, y);
                startCoords.RemoveAt(0);
                if(startTile == null || !startTile.IsNull || !TryGet(rand, startTile, map, out startTileConf, out _, out ruleData))
                    continue;

                foundStart = true;
                break;
            }
            if(!foundStart) {
                Debug.LogError($"Failed to find starting coords on layer {nameof(ProcGenFiller)}.");
                return false;
            }
            map.SetTile(startTile, Direction.North, startTileConf, ruleData.Rule, this);
            return true;
        }
        
        private void GenerateFromPOI(FRandom rand, Map map)
        {
            
        }

        private void GenerateNodes(Map.Tile startTile, FRandom rand, Map map)
        {
            HashSet<Map.Tile> visited = new();
            ExecBranch(Direction.North, startTile);
            return;

            void ExecNodeIteration(Direction direction, Map.Tile tile)
            {
                bool found = TryGetEx(rand, tile, map, out MapTile conf, out Map.Tile newTile, out Direction dir, out TileRuleData ruleData);
                if(!found || visited.Contains(newTile))
                    return;
                
                map.SetTile(newTile, dir, conf, ruleData.Rule, this);
                visited.Add(newTile);
                foreach (Direction branchDir in BranchDirections) {
                    ExecBranch(branchDir, newTile);
                }
            }

            void ExecBranch(Direction branchDir, Map.Tile tile)
            {
                int stepX = 0;
                int stepY = 0;
                switch(branchDir) {
                    case Direction.North: {
                        stepY = 1;
                    } break;
                    case Direction.East: {
                        stepX = 1;
                    } break;
                    case Direction.South: {
                        stepY = -1;
                    } break;
                    case Direction.West: {
                        stepX = -1;
                    } break;
                }

                int tileWidth  = tile.Prefab?.TilesX ?? 1;
                int tileHeight = tile.Prefab?.TilesY ?? 1;

                // Adjust based on rotation.
                int rotatedWidth  = tileWidth;
                int rotatedHeight = tileHeight;
                if(tile.Rotation == Direction.East || tile.Rotation == Direction.West) {
                    rotatedWidth  = tileHeight;
                    rotatedHeight = tileWidth;
                }

                // Move once in the branch direction to get the next tile's top-left corner.
                int checkX = tile.X + (stepX * rotatedWidth);
                int checkY = tile.Y + (stepY * rotatedHeight);
                Map.Tile nextTile = map.GetTile(checkX, checkY);
                if(nextTile != null && nextTile.IsNull && !visited.Contains(nextTile)) {
                    ExecNodeIteration(branchDir, nextTile);
                }
            }
        }

        public enum Modes
        {
            Random, 
            FromPOI
        }
    }
}
