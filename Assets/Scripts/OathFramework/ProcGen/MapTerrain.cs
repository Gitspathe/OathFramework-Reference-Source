using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.UI;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.ProcGen
{
    public class MapTerrain : MonoBehaviour, IPostInstantiationCallback
    {
        uint ILockableOrderedListElement.Order => 100;
        
        [SerializeField] private Terrain terrain;

        private HashSet<Map.Tile> processed = new();

        async UniTask IPostInstantiationCallback.OnPostTilesInstantiated(Stopwatch timer, Map map)
        {
            terrain.terrainData = Instantiate(terrain.terrainData);
            terrain.terrainData.size = new Vector3(
                map.Config.TileSize * map.Config.Tiles, 
                100.0f, 
                map.Config.TileSize * map.Config.Tiles
            );
            ProcGenUtil.ApplyTemplate(map.Config.TerrainTemplate, terrain.terrainData);
            ProcGenUtil.InitTerrain(terrain, map);
            int total    = 0;
            int fullSize = map.Tiles.Length;

            if(!TryFindFirstTerrainTile(map, out Map.Tile tile)) {
                Debug.LogError("Failed to find a valid terrain map tile. Aborting");
                return;
            }
            TerrainData data         = tile.Instance.TerrainData[0].TerrainData;
            StitchingContext context = new(terrain, data);

            // Verification.
            // foreach(Map.Tile t in map.Tiles) {
            //     if(t.Instance == null)
            //         continue;
            //     
            //     foreach(TileTerrainData td in t.Instance.TerrainData) {
            //         TerrainData dat = td.TerrainData;
            //         if(dat == null)
            //             continue;
            //
            //         if(dat.heightmapResolution != terrain.terrainData.heightmapResolution 
            //            || dat.alphamapWidth    != terrain.terrainData.alphamapWidth
            //            || dat.alphamapHeight   != terrain.terrainData.alphamapHeight
            //            || dat.alphamapLayers   != terrain.terrainData.alphamapLayers 
            //            || dat.detailHeight     != terrain.terrainData.detailHeight 
            //            || dat.detailWidth      != terrain.terrainData.detailResolution) {
            //             Throw();
            //         }
            //     }
            // }
            
            foreach(Map.Tile t in map.Tiles) {
                total++;
                float loadVal = fullSize == 0 || total == 0 ? 0.5f : total / (float)fullSize;
                LoadingUIScript.SetProgress(NetGame.Msg.GeneratingMapPostInitStr, 0.5f + (0.4f * loadVal));
                if(t.IsNull || t.Instance.TerrainData == null || t.Instance.TerrainData.Length == 0 || t.Parent != null || processed.Contains(t))
                    continue;
                
                ProcessTile(map, t, context);
                if(timer.Elapsed.Milliseconds > AsyncFrameBudgets.High) {
                    await UniTask.Yield();
                    timer.Restart();
                }
            }
            ProcGenUtil.Finalize(terrain);
            return;
            
            void Throw()
            {
                throw new Exception("Tile TerrainData does not match template TerrainData.");
            }
        }

        private bool TryFindFirstTerrainTile(Map map, out Map.Tile tile)
        {
            tile = null;
            for(int y = 0; y < map.SizeY; y++) {
                for(int x = 0; x < map.SizeX; x++) {
                    tile = map.GetTile(x, y);
                    if(tile == null || tile.Instance == null || tile.Instance.TerrainData.Length == 0)
                        continue;

                    return true;
                }
            }
            return false;
        }

        private void ProcessTile(Map map, Map.Tile tile, StitchingContext context)
        {
            foreach(TileTerrainData t in tile.Instance.TerrainData) {
                // Adjust the offsets based on the tile's rotation
                Vector2Int adjustedOffset = ProcGenUtil.RotateOffset(t.OffsetX, t.OffsetY, tile.Rotation, tile.Instance.TilesX, tile.Instance.TilesY);
                Vector2Int pos            = new(tile.X + adjustedOffset.x, tile.Y + adjustedOffset.y);
                Map.Tile mTile            = map.GetTile(pos.x, pos.y);
                if(processed.Contains(mTile))
                    continue;
                
                ProcGenUtil.StitchTile(context, t.TerrainData, map, pos, tile.Rotation);
                processed.Add(mTile);
                t.FreeIntermediateTerrainMemory();
            }
        }
    }
}
