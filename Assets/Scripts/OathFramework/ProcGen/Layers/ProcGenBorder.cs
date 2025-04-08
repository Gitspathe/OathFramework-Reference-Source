using Cysharp.Threading.Tasks;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace OathFramework.ProcGen.Layers
{
    
    [Serializable]
    public class ProcGenBorder : ProcGenLayer
    {
        [field: Header("Corners")]
        [field: SerializeField] public TileRule TopLeft     { get; private set; }
        [field: SerializeField] public TileRule TopRight    { get; private set; }
        [field: SerializeField] public TileRule BottomRight { get; private set; }
        [field: SerializeField] public TileRule BottomLeft  { get; private set; }
        
        [field: Header("Walls")]
        [field: SerializeField] public TileRule North { get; private set; }
        [field: SerializeField] public TileRule East  { get; private set; }
        [field: SerializeField] public TileRule South { get; private set; }
        [field: SerializeField] public TileRule West  { get; private set; }

        [field: SerializeField, Space(10), Header("Alignment")] 
        public bool AlignTiles          { get; private set; } = true;

        [field: SerializeField, ShowIf("@AlignTiles"), LabelText("Align north to...")]
        public Direction AlignmentNorth { get; private set; } = Direction.South;
        
        [field: SerializeField, ShowIf("@AlignTiles"), LabelText("Align east to...")]
        public Direction AlignmentEast  { get; private set; } = Direction.West;
        
        [field: SerializeField, ShowIf("@AlignTiles"), LabelText("Align south to...")]
        public Direction AlignmentSouth { get; private set; } = Direction.North;
        
        [field: SerializeField, ShowIf("@AlignTiles"), LabelText("Align west to...")]
        public Direction AlignmentWest  { get; private set; } = Direction.East;

        public override async UniTask Generate(FRandom rand, Map map)
        {
            for(int x = 0; x < map.SizeX; x++) {
                for(int y = 0; y < map.SizeY; y++) {
                    GenerateTile(rand, x, y, map);
                }
            }
        }

        private void GenerateTile(FRandom rand, int x, int y, Map map)
        {
            TileRule rule = null;
            Direction dir = Direction.North;
            
            // Corners
            if(x == 0 && y == 0) {
                rule = TopLeft;
                dir  = AlignmentNorth;
            } else if(x == map.SizeX-1 && y == 0) {
                rule = TopRight;
                dir  = AlignmentEast;
            } else if(x == map.SizeX-1 && y == map.SizeY-1) {
                rule = BottomRight;
                dir  = AlignmentSouth;
            } else if(x == 0 && y == map.SizeY-1) {
                rule = BottomLeft;
                dir  = AlignmentWest;
            } 
            
            // Walls
            else if(x > 0 && y == 0) {
                rule = North;
                dir  = AlignmentNorth;
            } else if(x == map.SizeX-1 && y > 0) {
                rule = East;
                dir  = AlignmentEast;
            } else if(x > 0 && y == map.SizeY-1) {
                rule = South;
                dir  = AlignmentSouth;
            } else if(x == 0 && y > 0) {
                rule = West;
                dir  = AlignmentWest;
            }
            Map.Tile tile = map.GetTile(x, y);
            
            // Do not generate if it's in the middle of the map, or the rule returned null.
            if(tile == null || rule == null || !rule.TryRetrieveNext(rand, tile, map, this, out MapTile conf, out Direction d))
                return;
            
            map.SetTile(tile, AlignTiles ? dir : d, conf, rule, this);
        }
    }
}
