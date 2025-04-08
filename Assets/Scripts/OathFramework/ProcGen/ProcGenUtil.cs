using System;
using System.Threading.Tasks;
using UnityEngine;

namespace OathFramework.ProcGen
{
    public static class ProcGenUtil
    {
        public static bool ParallelOperations = true;
        
        public static void ApplyTemplate(TerrainTemplate template, TerrainData other)
        {
            if(template == null || other == null)
                return;
            
            TerrainData templateData           = template.Data;
            TerrainData otherData              = other;
            TreePrototype[] treePrototypes     = new TreePrototype[templateData.treePrototypes.Length];
            DetailPrototype[] detailPrototypes = new DetailPrototype[templateData.detailPrototypes.Length];
            TerrainLayer[] terrainLayers       = new TerrainLayer[templateData.terrainLayers.Length];
            for(int i = 0; i < templateData.treePrototypes.Length; i++) {
                treePrototypes[i] = new TreePrototype(templateData.treePrototypes[i]);
            }
            for(int i = 0; i < templateData.detailPrototypes.Length; i++) {
                detailPrototypes[i] = new DetailPrototype(templateData.detailPrototypes[i]);
            }
            for(int i = 0; i < templateData.terrainLayers.Length; i++) {
                terrainLayers[i] = templateData.terrainLayers[i];
            }
            otherData.treePrototypes   = treePrototypes;
            otherData.detailPrototypes = detailPrototypes;
            otherData.terrainLayers    = terrainLayers;
        }
        
        public static Quaternion TileRotationToQuaternion(Direction rotation)
        {
            switch(rotation) {
                case Direction.East:
                    return Quaternion.Euler(0.0f, 90.0f, 0.0f);
                case Direction.South:
                    return Quaternion.Euler(0.0f, 180.0f, 0.0f);
                case Direction.West:
                    return Quaternion.Euler(0.0f, 270.0f, 0.0f);
                
                case Direction.North:
                default:
                    return Quaternion.identity;
            }
        }

        public static Direction InverseDirection(Direction dir)
        {
            switch(dir) {
                case Direction.North:
                    return Direction.South;
                case Direction.East:
                    return Direction.West;
                case Direction.South:
                    return Direction.North;
                case Direction.West:
                    return Direction.East;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }
        
        public static Vector2Int RotateOffset(int offsetX, int offsetY, Direction direction, int tileWidth, int tileHeight)
        {
            switch(direction) {
                case Direction.North:
                    return new Vector2Int(offsetX, offsetY);
                case Direction.East:
                    return new Vector2Int(tileHeight - offsetY - 1, offsetX);
                case Direction.South:
                    return new Vector2Int(tileWidth - offsetX - 1, tileHeight - offsetY - 1);
                case Direction.West:
                    return new Vector2Int(offsetY, tileWidth - offsetX - 1);

                default:
                    Debug.LogError("Invalid direction");
                    return new Vector2Int(offsetX, offsetY);
            }
        }
        
        public static void InitTerrain(Terrain mainTerrain, Map map)
        {
            TerrainData mainData         = mainTerrain.terrainData;
            GameObject go                = mainTerrain.gameObject;
            mainData.heightmapResolution = map.Config.TileHeightmapRes * map.SizeX;
            mainData.alphamapResolution  = map.Config.TileAlphamapRes * map.SizeX;
            mainData.size                = new Vector3(map.Config.TileSize * map.SizeX, 100.0f, map.Config.TileSize * map.SizeY);
            go.transform.position        = new Vector3(
                go.transform.position.x,
                go.transform.position.y,
                -mainData.size.z
            );
            mainData.SetDetailResolution(map.Config.TileDetailmapRes * map.SizeX, 32);
        }
        
        public static void StitchTile(StitchingContext context, TerrainData tileData, Map map, Vector2Int coords, Direction direction)
        {
            StitchHeightmap(context, tileData, map, coords, direction);
            StitchSplatmap(context, tileData, map, coords, direction);
            StitchDetails(context, tileData, map, coords, direction);
            PlaceTrees(context.MainTerrain, tileData, map, coords, direction);
        }
        
        public static void StitchHeightmap(StitchingContext context, TerrainData tileData, Map map, Vector2Int coords, Direction direction)
        {
            TerrainData mainData     = context.MainTerrain.terrainData;
            int tileRes              = tileData.heightmapResolution;
            float tileHeightmapScale = (tileData.heightmapResolution - 1) / map.Config.TileSize;
            Vector2Int worldPos      = new(
                Mathf.FloorToInt(coords.x * map.Config.TileSize),
                Mathf.FloorToInt(coords.y * map.Config.TileSize)
            );
            Vector2Int tileHeightmapOffset = new(
                Mathf.FloorToInt(worldPos.x * tileHeightmapScale),
                Mathf.FloorToInt((map.SizeY * map.Config.TileSize - worldPos.y - map.Config.TileSize) * tileHeightmapScale)
            );
            float[,] tileHeights    = tileData.GetHeights(0, 0, tileRes, tileRes);
            float[,] rotatedHeights = RotateHeights(context, tileHeights, direction);
            for(int y = 0; y < tileRes; y++) {
                for(int x = 0; x < tileRes; x++) {
                    tileHeights[tileRes - 1 - y, x] = rotatedHeights[y, x];
                }
            }
            mainData.SetHeightsDelayLOD(tileHeightmapOffset.x, tileHeightmapOffset.y, tileHeights);
        }
        
        public static void StitchSplatmap(StitchingContext context, TerrainData tileData, Map map, Vector2Int coords, Direction direction)
        {
            TerrainData mainData      = context.MainTerrain.terrainData;
            int tileRes               = tileData.alphamapResolution;
            float tileScale           = tileRes / map.Config.TileSize;
            Vector2Int worldPos       = new(
                Mathf.FloorToInt(coords.x * map.Config.TileSize), 
                Mathf.FloorToInt(coords.y * map.Config.TileSize)
            );
            Vector2Int offset         = new(
                Mathf.FloorToInt(worldPos.x * tileScale), 
                Mathf.FloorToInt((map.SizeY * map.Config.TileSize - worldPos.y - map.Config.TileSize) * tileScale)
            );
            float[,,] tileSplatmap    = tileData.GetAlphamaps(0, 0, tileRes, tileRes);
            float[,,] rotatedSplatmap = RotateSplatmap(context, tileSplatmap, direction);
            mainData.SetAlphamaps(offset.x, offset.y, rotatedSplatmap);
        }
        
        public static void StitchDetails(StitchingContext context, TerrainData tileData, Map map, Vector2Int coords, Direction direction)
        {
            TerrainData mainData     = context.MainTerrain.terrainData;
            int tileRes              = tileData.detailResolution;
            Vector2Int tileMapOffset = new(
                Mathf.FloorToInt(coords.x * tileRes),
                Mathf.FloorToInt(((map.SizeY - 1) * tileRes) - (coords.y * tileRes))
            );
            for(int layer = 0; layer < tileData.detailPrototypes.Length; layer++) {
                int[,] tileDetails    = tileData.GetDetailLayer(0, 0, tileRes, tileRes, layer);
                int[,] rotatedDetails = RotateDetailLayer(context, tileDetails, direction);
                mainData.SetDetailLayer(tileMapOffset.x, tileMapOffset.y, layer, rotatedDetails);
            }
        }
        
        public static void PlaceTrees(Terrain mainTerrain, TerrainData tileData, Map map, Vector2Int coords, Direction direction)
        {
            float fullSize     = map.Config.TileSize * map.SizeX;
            Vector3 tileOffset = new(coords.x * map.Config.TileSize / fullSize, 0, coords.y * map.Config.TileSize / fullSize);
            foreach(TreeInstance tree in tileData.treeInstances) {
                Vector3 rotatedPos = RotateTreePosition(tree.position, direction);
                Vector3 worldPos   = rotatedPos * (map.Config.TileSize / fullSize) + tileOffset;
                worldPos           = new Vector3(worldPos.x, worldPos.y, 1f - worldPos.z);
                mainTerrain.AddTreeInstance(new TreeInstance {
                    position       = worldPos,
                    widthScale     = tree.widthScale,
                    heightScale    = tree.heightScale,
                    rotation       = tree.rotation,
                    color          = tree.color,
                    lightmapColor  = tree.lightmapColor,
                    prototypeIndex = tree.prototypeIndex
                });
            }
        }

        public static void Finalize(Terrain mainTerrain)
        {
            mainTerrain.terrainData.SyncHeightmap();
            mainTerrain.terrainData.RefreshPrototypes();
            mainTerrain.Flush();
            if(mainTerrain.gameObject.TryGetComponent(out TerrainCollider col)) {
                col.terrainData = mainTerrain.terrainData;
            }
        }

        private static float[,] RotateHeights(StitchingContext context, float[,] heights, Direction direction)
        {
            int size         = heights.GetLength(0);
            float[,] rotated = context.TileHeights;
            if(ParallelOperations) {
                Parallel.For(0, size, y => {
                    for(int x = 0; x < size; x++) {
                        switch (direction) {
                            case Direction.North:
                                rotated[y, x] = heights[size - 1 - y, x];
                                break;
                            case Direction.East:
                                rotated[y, x] = heights[x, y];
                                break;
                            case Direction.South:
                                rotated[y, x] = heights[y, size - 1 - x];
                                break;
                            case Direction.West:
                                rotated[y, x] = heights[size - 1 - x, size - 1 - y];
                                break;
                        }
                    }
                });
                return rotated;
            } 
            for(int y = 0; y < size; y++) {
                for(int x = 0; x < size; x++) {
                    switch (direction) {
                        case Direction.North:
                            rotated[y, x] = heights[size - 1 - y, x];
                            break;
                        case Direction.East:
                            rotated[y, x] = heights[x, y];
                            break;
                        case Direction.South:
                            rotated[y, x] = heights[y, size - 1 - x];
                            break;
                        case Direction.West:
                            rotated[y, x] = heights[size - 1 - x, size - 1 - y];
                            break;
                    }
                }
            }
            return rotated;
        }

        private static float[,,] RotateSplatmap(StitchingContext context, float[,,] splatmap, Direction direction)
        {
            int size          = splatmap.GetLength(0);
            int layers        = splatmap.GetLength(2);
            float[,,] rotated = context.TileSplatmaps;
            if(ParallelOperations) {
                Parallel.For(0, size, y => {
                    for(int x = 0; x < size; x++) {
                        for(int l = 0; l < layers; l++) {
                            switch(direction) {
                                case Direction.North:
                                    rotated[x, y, l] = splatmap[x, y, l];
                                    break;
                                case Direction.East:
                                    rotated[x, y, l] = splatmap[y, size - 1 - x, l];
                                    break;
                                case Direction.South:
                                    rotated[x, y, l] = splatmap[size - 1 - x, size - 1 - y, l];
                                    break;
                                case Direction.West:
                                    rotated[x, y, l] = splatmap[size - 1 - y, x, l];
                                    break;
                            }
                        }
                    }
                });
                return rotated;
            }
            for(int y = 0; y < size; y++) {
                for(int x = 0; x < size; x++) {
                    for(int l = 0; l < layers; l++) {
                        switch(direction) {
                            case Direction.North:
                                rotated[x, y, l] = splatmap[x, y, l];
                                break;
                            case Direction.East:
                                rotated[x, y, l] = splatmap[y, size - 1 - x, l];
                                break;
                            case Direction.South:
                                rotated[x, y, l] = splatmap[size - 1 - x, size - 1 - y, l];
                                break;
                            case Direction.West:
                                rotated[x, y, l] = splatmap[size - 1 - y, x, l];
                                break;
                        }
                    }
                }
            }
            return rotated;
        }

        private static int[,] RotateDetailLayer(StitchingContext context, int[,] details, Direction direction)
        {
            int size       = details.GetLength(0);
            int[,] rotated = context.TileDetails;
            if(ParallelOperations) {
                Parallel.For(0, size, y => {
                    for(int x = 0; x < size; x++) {
                        switch (direction) {
                            case Direction.North:
                                rotated[x, y] = details[x, y];
                                break;
                            case Direction.East:
                                rotated[x, y] = details[y, size - 1 - x];
                                break;
                            case Direction.South:
                                rotated[x, y] = details[size - 1 - x, size - 1 - y];
                                break;
                            case Direction.West:
                                rotated[x, y] = details[size - 1 - y, x];
                                break;
                        }
                    }
                });
                return rotated;
            }
            for(int y = 0; y < size; y++) {
                for(int x = 0; x < size; x++) {
                    switch (direction) {
                        case Direction.North:
                            rotated[x, y] = details[x, y];
                            break;
                        case Direction.East:
                            rotated[x, y] = details[y, size - 1 - x];
                            break;
                        case Direction.South:
                            rotated[x, y] = details[size - 1 - x, size - 1 - y];
                            break;
                        case Direction.West:
                            rotated[x, y] = details[size - 1 - y, x];
                            break;
                    }
                }
            }
            return rotated;
        }

        private static Vector3 RotateTreePosition(Vector3 position, Direction direction)
        {
            switch(direction) {
                case Direction.East:
                    return new Vector3(position.z, position.y, position.x);
                case Direction.South:
                    return new Vector3(1f - position.x, position.y, position.z);
                case Direction.West:
                    return new Vector3(1f - position.z, position.y, 1f-position.x);
                case Direction.North:
                default:
                    return new Vector3(position.x, position.y, 1f - position.z);
            }
        }
    }
    
    public sealed class StitchingContext
    {
        public Terrain MainTerrain;
        
        public float[,] TileHeights;
        public float[,,] TileSplatmaps;
        public int[,] TileDetails;

        public StitchingContext(Terrain mainTerrain, TerrainData tileData)
        {
            MainTerrain   = mainTerrain;
            TileHeights   = new float[tileData.heightmapResolution, tileData.heightmapResolution];
            TileSplatmaps = new float[tileData.alphamapWidth, tileData.alphamapHeight, tileData.alphamapLayers];
            TileDetails   = new int[tileData.detailWidth, tileData.detailHeight];
        }
    }
}
