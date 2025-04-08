using Cysharp.Threading.Tasks;
using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.ProcGen.Layers
{
    [Serializable]
    public abstract class ProcGenLayer : ITileSource
    {
        [field: SerializeField] public ushort Order { get; private set; } = 100;
        
        public abstract UniTask Generate(FRandom rand, Map map);

        public bool IsSourceOf(MapTile tile)
        {
            if(tile == null || tile.TileData == null)
                return false;
            
            return tile.TileData.SourceLayer == this;
        }
    }
}
