using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.ProcGen.Layers;
using OathFramework.UI;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;

namespace OathFramework.ProcGen
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private MapConfig testConfig;
        [SerializeField] private GameObject navmeshParent;
        
        private readonly LockableOrderedList<IPostInstantiationCallback> postTilesInstantiated = new();
        
        public MapConfig Config { get; private set; }
        public uint Seed         { get; private set; }
        public Tile[] Tiles     { get; private set; }
        public ushort SizeX     { get; private set; }
        public ushort SizeY     { get; private set; }

        private HashSet<GameObject> tilePrefabCache = new();
        private FRandom rand;
        private int spawnedTileCount;

        private static List<DirectionEx> AllDirections = new() {
            DirectionEx.N, DirectionEx.NE, DirectionEx.E, DirectionEx.SE, DirectionEx.S, DirectionEx.SW, DirectionEx.W, DirectionEx.NW
        };
        
        public static bool AsyncInstantiation { get; private set; } = true;

        private void Awake()
        {
            foreach(IPostInstantiationCallback callback in GetComponentsInChildren<IPostInstantiationCallback>(true)) {
                postTilesInstantiated.AddUnique(callback);
            }
            if(INISettings.GetBool("Performance/AsyncMapTileGen") == false) {
                AsyncInstantiation = false;
            }
        }

        private void OnDestroy()
        {
            NavMesh.RemoveAllNavMeshData();
        }

        [Button("Regenerate")]
        private void EditorRegenerate()
        {
            if(!Application.isPlaying)
                return;

            foreach(Tile t in Tiles) {
                if(t.Instance == null || t.Instance.gameObject == null)
                    continue;
                
                Destroy(t.Instance.gameObject);
            }
            foreach(Tile t in Tiles) {
                t.Clear();
            }
            Stopwatch s = new();
            s.Start();
            _ = Initialize(s, testConfig, FRandom.Cache.UInt());
        }

        public async UniTask Initialize(Stopwatch timer, MapConfig config, uint seed, CancellationToken ct = default)
        {
            if(Game.ExtendedDebug) {
                Debug.Log($"Generating map with seed: {seed}");
            }

            rand   = new FRandom(seed);
            Config = config.DeepCopy();
            Seed   = seed;
            SizeX  = (ushort)Mathf.Clamp(config.Tiles, 0, ushort.MaxValue);
            SizeY  = (ushort)Mathf.Clamp(config.Tiles, 0, ushort.MaxValue);
            Tiles  = new Tile[SizeX * SizeY];
            if(config.Environment != null) {
                config.Environment.Apply();
            }
            for(int x = 0; x < SizeX; x++) {
                for(int y = 0; y < SizeY; y++) {
                    Tiles[y * SizeX + x] = new Tile().Initialize(x, y, this);
                }
            }
            if(AsyncInstantiation) {
                float defaultBudget = AsyncInstantiateOperation.GetIntegrationTimeMS();
                try {
                    AsyncInstantiateOperation.SetIntegrationTimeMS(1000.0f);
                    await Generate(timer, ct);
                } finally {
                    AsyncInstantiateOperation.SetIntegrationTimeMS(defaultBudget);
                }
            } else {
                await Generate(timer, ct);
            }
        }

        private async UniTask Generate(Stopwatch timer, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            LoadingUIScript.SetProgress(NetGame.Msg.GeneratingMapStr, 0.0f);
            
            List<ProcGenLayer> layers  = new();
            HashSet<ushort> priorities = new();
            foreach(ProcGenLayerSO so in Config.ProcGenLayers) {
                ct.ThrowIfCancellationRequested();
                if(so.Data == null)
                    continue;
                
                layers.Add(so.Data);
                if(!priorities.Add(so.Data.Order)) {
                    Debug.LogError($"Duplicate layer priority: {so.Data.Order}. This may cause RNG to malfunction!");
                }
            }
            layers.Sort((x, y) => x.Order.CompareTo(y.Order));
            foreach(ProcGenLayer layer in layers) {
                ct.ThrowIfCancellationRequested();
                layer.Generate(rand, this);
                if(timer.Elapsed.Milliseconds > AsyncFrameBudgets.High) {
                    await UniTask.Yield();
                    timer.Restart();
                }
            }
            LoadingUIScript.SetProgress(NetGame.Msg.GeneratingMapStr, 0.0f);
            await Instantiate(timer, ct);
            
            LoadingUIScript.SetProgress(NetGame.Msg.GeneratingNavMeshStr, 0.9f);
            await GenerateNavMesh(timer, ct);
            
            LoadingUIScript.SetProgress(NetGame.Msg.WaitingForOthersStr, 1.0f);
            
            // GC and clean up assets.
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
        
        private async UniTask Instantiate(Stopwatch timer, CancellationToken ct = default)
        {
            int total    = 0;
            int fullSize = SizeX * SizeY;
            for(int x = 0; x < SizeX; x++) {
                for(int y = 0; y < SizeY; y++) {
                    ct.ThrowIfCancellationRequested();

                    total++;
                    float loadVal = fullSize == 0 || total == 0 ? 0.0f : total / (float)fullSize;
                    LoadingUIScript.SetProgress(NetGame.Msg.GeneratingMapStr, 0.0f + (0.5f * loadVal));
                    
                    Tile tile = GetTile(x, y);
                    if(tile.Instantiated || tile.IsNull)
                        continue;

                    if(tile.Parent != null) {
                        if(tile.Parent.Instantiated) {
                            tile.CopyFromParent(tile.Parent);
                            continue;
                        }
                        await InstantiateTile(timer, tile.Parent);
                        continue;
                    }
                    await InstantiateTile(timer, tile);
                }
            }
            foreach(Tile tile in Tiles) {
                ct.ThrowIfCancellationRequested();
                if(timer.Elapsed.Milliseconds > AsyncFrameBudgets.High) {
                    await UniTask.Yield();
                    timer.Restart();
                }
                if(tile.Instance == null)
                    continue;
                
                tile.Instance.OnAllTilesInstantiated(this);
            }

            postTilesInstantiated.Lock();
            foreach(IPostInstantiationCallback callback in postTilesInstantiated.Current) {
                ct.ThrowIfCancellationRequested();
                await callback.OnPostTilesInstantiated(timer, this);
            }
            postTilesInstantiated.Unlock();
            
            // Clear cache.
            tilePrefabCache.Clear();
            spawnedTileCount = 0;
        }

        private async UniTask GenerateNavMesh(Stopwatch timer, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if(navmeshParent == null) {
                Debug.LogError("No Navmesh parent set.");
                return;
            }
            
            NavMesh.RemoveAllNavMeshData();
            foreach(NavMeshSurface surface in navmeshParent.GetComponentsInChildren<NavMeshSurface>()) {
                List<NavMeshBuildSource> buildSources = new();
                List<NavMeshBuildMarkup> markup       = new();
                Bounds bounds = new(
                    new Vector3((SizeX * Config.TileSize) / 2, 0.0f, -((SizeY * Config.TileSize) / 2)),
                    new Vector3((SizeX * Config.TileSize) + 100.0f, 1000.0f, (SizeY * Config.TileSize) + 100.0f)
                );
                NavMeshBuilder.CollectSources(
                    bounds,
                    surface.layerMask,
                    NavMeshCollectGeometry.PhysicsColliders,
                    surface.defaultArea,
                    markup,
                    buildSources
                );
                NavMeshBuildSettings buildSettings = surface.GetBuildSettings();
                NavMeshData meshData               = new();
                if(AsyncInstantiation) {
                    await NavMeshBuilder.UpdateNavMeshDataAsync(meshData, buildSettings, buildSources, bounds);
                } else {
                    NavMeshBuilder.UpdateNavMeshData(meshData, buildSettings, buildSources, bounds);
                }
                ct.ThrowIfCancellationRequested();
                NavMesh.AddNavMeshData(meshData);
            }
        }

        private async UniTask InstantiateTile(Stopwatch timer, Tile tile)
        {
            if(tile.Prefab == null || tile.IsNull) {
                Debug.LogError($"Tile {tile} has no prefab or is null.");
                return;
            }
            
            // Async budgeting.
            if((spawnedTileCount++ % 10 == 0 || timer.Elapsed.TotalMilliseconds > AsyncFrameBudgets.High) && AsyncInstantiation) {
                await UniTask.Yield();
                timer.Restart();
            }
            
            int sizeX = tile.Prefab.TilesX;
            int sizeY = tile.Prefab.TilesY;
            
            // Adjust size based on rotation
            if(tile.Rotation == Direction.East || tile.Rotation == Direction.West) {
                (sizeX, sizeY) = (sizeY, sizeX); // Swap for East/West rotation
            }
            
            // Adjust the pivot based on rotation
            Vector3 pivotOffset = Vector3.zero;
            float tileSize      = Config.TileSize;
            switch(tile.Rotation) {
                case Direction.North: {
                    pivotOffset = new Vector3(0, 0, 0);
                } break;
                case Direction.East: {
                    pivotOffset = new Vector3(sizeX * tileSize, 0, 0);
                } break;
                case Direction.South: {
                    pivotOffset = new Vector3(sizeX * tileSize, 0, -sizeY * tileSize);
                } break;
                case Direction.West: {
                    pivotOffset = new Vector3(0, 0, -sizeY * tileSize);
                } break;
            }
            
            GameObject go;
            Vector3 position = new Vector3(tile.X * tileSize, 0.0f, -tile.Y * tileSize) + pivotOffset;
            if(!tilePrefabCache.Contains(tile.Prefab.gameObject) && AsyncInstantiation) {
                AsyncInstantiateOperation<GameObject> asyncOp = InstantiateAsync(
                    tile.Prefab.gameObject,
                    position,
                    ProcGenUtil.TileRotationToQuaternion(tile.Rotation)
                );
                await asyncOp;
                await UniTask.Yield();
                go = asyncOp.Result[0];
                tilePrefabCache.Add(tile.Prefab.gameObject);
            } else {
                go = Instantiate(tile.Prefab.gameObject, position, ProcGenUtil.TileRotationToQuaternion(tile.Rotation));
            }
            
            MapTile t = go.GetComponent<MapTile>();
            if(t == null) {
                Debug.LogError($"MapTile at {tile.X}, {tile.Y} has no {nameof(MapTile)} component.");
                return;
            }
            tile.OnInstantiated(t);
            tile.Instance.SetupSubVariant(rand);
            t.OnTileInstantiated(this, tile);
            tile.Instance.name = $"Tile ({tile.X}, {tile.Y})";
        }

        public Tile GetTile(int x, int y)
        {
            if(x < 0 || x >= SizeX || y < 0 || y >= SizeY)
                return null;
            
            return Tiles[y * SizeX + x];
        }

        public bool TryGetTile(int x, int y, out Tile tile)
        {
            tile = null;
            if(x < 0 || x >= SizeX || y < 0 || y >= SizeY)
                return false;
            
            tile = GetTile(x, y);
            return tile != null;
        }

        public bool TryGetTileRelative(Tile source, Direction direction, out Tile adjacent, ushort distance = 1, bool snapToParent = true)
        {
            adjacent = null;
            if(source == null)
                return false;

            // Get tile size based on rotation
            int posX  = source.X;
            int posY  = source.Y;
            int sizeX = source.Prefab != null ? source.Prefab.TilesX : 1;
            int sizeY = source.Prefab != null ? source.Prefab.TilesY : 1;
            if(source.Rotation == Direction.East || source.Rotation == Direction.West) {
                (sizeX, sizeY) = (sizeY, sizeX);
            }
            if(snapToParent && source.Parent != null) {
                posX = source.Parent.X;
                posY = source.Parent.Y;
            }

            // Calculate offsets based on direction and rotation.
            int offsetX = 0;
            int offsetY = 0;
            switch(direction) {
                case Direction.North: {
                    offsetY = -distance;
                } break;
                case Direction.East: {
                    offsetX = (sizeX - 1) + distance;
                } break;
                case Direction.South: {
                    offsetY = (sizeY - 1) + distance;
                } break;
                case Direction.West: {
                    offsetX = -distance;
                } break;
            }

            // Apply offset based on size and rotation.
            int targetX = posX + offsetX;
            int targetY = posY + offsetY;

            // Get the tile at the calculated position.
            return TryGetTile(targetX, targetY, out adjacent) && adjacent != null;
        }

        public bool TryGetTileRelative(Tile source, DirectionEx direction, out Tile adjacent, ushort distance = 1, bool snapToParent = true)
        {
            adjacent = null;
            if(source == null)
                return false;

            // Get tile size based on rotation
            int posX  = source.X;
            int posY  = source.Y;
            int sizeX = source.Prefab != null ? source.Prefab.TilesX : 1;
            int sizeY = source.Prefab != null ? source.Prefab.TilesY : 1;
            if(source.Rotation == Direction.East || source.Rotation == Direction.West) {
                (sizeX, sizeY) = (sizeY, sizeX);
            }
            if(snapToParent && source.Parent != null) {
                posX = source.Parent.X;
                posY = source.Parent.Y;
            }

            // Calculate offsets based on direction and rotation.
            int offsetX = 0;
            int offsetY = 0;
            switch(direction) {
                case DirectionEx.N: {
                    offsetY = -distance;
                } break;
                case DirectionEx.NE: {
                    offsetX = (sizeX - 1) + distance;
                    offsetY = -distance;
                } break;
                case DirectionEx.E: {
                    offsetX = (sizeX - 1) + distance;
                } break;
                case DirectionEx.SE: {
                    offsetX = (sizeX - 1) + distance;
                    offsetY = (sizeY - 1) + distance;
                } break;
                case DirectionEx.S: {
                    offsetY = (sizeY - 1) + distance;
                } break;
                case DirectionEx.SW: {
                    offsetX = -distance;
                    offsetY = (sizeY - 1) + distance;
                } break;
                case DirectionEx.W: {
                    offsetX = -distance;
                } break;
                case DirectionEx.NW: {
                    offsetX = -distance;
                    offsetY = -distance;
                } break;
            }

            // Apply offset based on size and rotation.
            int targetX = posX + offsetX;
            int targetY = posY + offsetY;

            // Get the tile at the calculated position.
            return TryGetTile(targetX, targetY, out adjacent) && adjacent != null;
        }

        public bool TryGetTileRelative(int x, int y, Direction direction, out Tile tile, ushort distance = 1)
        {
            tile = null;
            switch(direction) {
                case Direction.North: {
                    if(y - distance < 0)
                        return false;

                    tile = GetTile(x, y - distance);
                } break;
                case Direction.East: {
                    if(x + distance > SizeX)
                        return false;

                    tile = GetTile(x + distance, y);
                } break;
                case Direction.South: {
                    if(y + distance > SizeY)
                        return false;

                    tile = GetTile(x, y + distance);
                } break;
                case Direction.West: {
                    if(x - distance < 0)
                        return false;

                    tile = GetTile(x - distance, y);
                } break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            return true;
        }
        
        public bool TryGetTileRelative(int x, int y, DirectionEx direction, out Tile tile, ushort distance = 1)
        {
            tile = null;
            switch(direction) {
                case DirectionEx.N: {
                    if(y - distance < 0)
                        return false;

                    tile = GetTile(x, y - distance);
                } break;
                case DirectionEx.NE: {
                    if(y - distance < 0 || x + distance > SizeX)
                        return false;

                    tile = GetTile(x + distance, y - distance);
                } break;
                case DirectionEx.E: {
                    if(x + distance > SizeX)
                        return false;

                    tile = GetTile(x + distance, y);
                } break;
                case DirectionEx.SE: {
                    if(y + distance > SizeY || x + distance > SizeX)
                        return false;

                    tile = GetTile(x + distance, y + distance);
                } break;
                case DirectionEx.S: {
                    if(y + distance > SizeY)
                        return false;

                    tile = GetTile(x, y + distance);
                } break;
                case DirectionEx.SW: {
                    if(y + distance > SizeY || x - distance < 0)
                        return false;

                    tile = GetTile(x - distance, y + distance);
                } break;
                case DirectionEx.W: {
                    if(x - distance < 0)
                        return false;

                    tile = GetTile(x - distance, y);
                } break;
                case DirectionEx.NW: {
                    if(y - distance < 0 || x - distance < 0)
                        return false;

                    tile = GetTile(x - distance, y - distance);
                } break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            return true;
        }

        public Tile CheckForTile(
            Tile source, 
            MapTile check, 
            List<DirectionEx> directions = null, 
            List<Direction> rotations    = null, 
            ushort distance              = 1, 
            bool snapToParent            = true)
        {
            if(source == null || distance == 0)
                return null;

            List<DirectionEx> directionList = directions ?? AllDirections;
            for(ushort i = 1; i < distance + 1; i++) {
                foreach(DirectionEx dir in directionList) {
                    if(TryGetTileRelative(source, dir, out Tile c, i, snapToParent)
                       && c.Prefab == check
                       && (rotations == null || rotations.Contains(c.Rotation)))
                        return c;
                }
            }
            return null;
        }

        public Tile CheckForTile(
            int x, 
            int y, 
            MapTile check, 
            List<DirectionEx> directions = null, 
            List<Direction> rotations    = null, 
            ushort distance              = 1)
        {
            if(distance == 0)
                return null;

            List<DirectionEx> directionList = directions ?? AllDirections;
            for(ushort i = 1; i < distance + 1; i++) {
                foreach(DirectionEx dir in directionList) {
                    if(TryGetTileRelative(x, y, dir, out Tile c, i) 
                       && c.Prefab == check 
                       && (rotations == null || rotations.Contains(c.Rotation)))
                        return c;
                }
            }
            return null;
        }

        public bool CheckForTiles(
            Tile source, 
            List<ITileSource> check, 
            List<DirectionEx> directions = null, 
            List<Direction> rotations    = null, 
            ushort distance              = 1, 
            bool snapToParent            = true)
        {
            if(source == null || distance == 0)
                return false;
            
            List<DirectionEx> directionList = directions ?? AllDirections;
            foreach(ITileSource tile in check) {
                for(ushort i = 1; i < distance + 1; i++) {
                    foreach(DirectionEx dir in directionList) {
                        if(TryGetTileRelative(source, dir, out Tile c, i, snapToParent) 
                           && tile.IsSourceOf(c.Prefab)
                           && (rotations == null || rotations.Contains(c.Rotation)))
                            return true;
                    }
                }
            }
            return false;
        }
        
        public bool CheckForTiles(
            int x, 
            int y, 
            List<MapTile> check, 
            List<DirectionEx> directions = null, 
            List<Direction> rotations    = null, 
            ushort distance              = 1)
        {
            if(distance == 0)
                return false;

            List<DirectionEx> directionList = directions ?? AllDirections;
            foreach(MapTile tile in check) {
                for(ushort i = 1; i < distance + 1; i++) {
                    foreach(DirectionEx dir in directionList) {
                        if(TryGetTileRelative(x, y, dir, out Tile c, i) 
                           && c.Prefab == tile 
                           && (rotations == null || rotations.Contains(c.Rotation)))
                            return true;
                    }
                }
            }
            return false;
        }
        
        public void SetTile(Tile tile, Direction rotation, MapTile conf, TileRule sourceRule, ProcGenLayer sourceLayer)
        {
            if(tile == null || conf == null)
                return;
            
            int x     = tile.X;
            int y     = tile.Y;
            int sizeX = conf.TilesX;
            int sizeY = conf.TilesY;
            if(x < 0 || x + sizeX > SizeX || y < 0 || y + sizeY > SizeY) {
                Debug.LogWarning($"SetTile out of bounds: ({x}, {y}) with size ({sizeX}, {sizeY})");
                return;
            }
            tile.Set(conf, rotation, sourceRule, sourceLayer, null);
            if(sizeX == 1 && sizeY == 1)
                return; // 1x1 - no scaling or setting children needed.

            // Handle rotation - map sizeX and sizeY based on rotation
            int rotatedSizeX = rotation == Direction.East || rotation == Direction.West ? sizeY : sizeX;
            int rotatedSizeY = rotation == Direction.East || rotation == Direction.West ? sizeX : sizeY;

            // Set other tiles as children of the parent tile
            for(int offsetX = 0; offsetX < rotatedSizeX; offsetX++) {
                for(int offsetY = 0; offsetY < rotatedSizeY; offsetY++) {
                    Tile t = GetTile(x + offsetX, y + offsetY);
                    if(t == null)
                        continue;
                    
                    t.Set(conf, rotation, sourceRule, sourceLayer, t != tile ? tile : null);
                }
            }
        }

        public bool IsSpaceFree(Tile tile, Direction rotation, MapTile conf)
        {
            if(tile == null)
                return false;

            return IsSpaceFree(tile, conf != null ? conf.TilesX : 1, conf != null ? conf.TilesY : 1, rotation);
        }
        
        public bool IsSpaceFree(Tile tile, int sizeX, int sizeY, Direction rotation = Direction.North)
        {
            if(tile == null)
                return false;

            int x            = tile.X;
            int y            = tile.Y;
            int rotatedSizeX = rotation == Direction.East || rotation == Direction.West ? sizeY : sizeX;
            int rotatedSizeY = rotation == Direction.East || rotation == Direction.West ? sizeX : sizeY;
            if(x < 0 || x + rotatedSizeX > SizeX || y < 0 || y + rotatedSizeY > SizeY)
                return false;
            if(rotatedSizeX == 1 && rotatedSizeY == 1)
                return GetTile(x, y).IsNull; // 1x1 case - simple check

            // Check tiles.
            for(int offsetX = 0; offsetX < rotatedSizeX; offsetX++) {
                for(int offsetY = 0; offsetY < rotatedSizeY; offsetY++) {
                    Tile t = GetTile(x + offsetX, y + offsetY);
                    if(t == null || !t.IsNull)
                        return false;
                }
            }
            return true;
        }

        public void FindTilesFromTileRule(TileRule rule, List<Tile> tiles, bool includeChildren = true)
        {
            for(int x = 0; x < SizeX; x++) {
                for(int y = 0; y < SizeY; y++) {
                    Tile tile = GetTile(x, y);
                    if(!includeChildren && tile.Parent != null)
                        continue;

                    if(tile.SourceRule == rule) {
                        tiles.Add(tile);
                    }
                }
            }
        }
        
        public void FindTilesFromProcGenLayer(ProcGenLayer layer, List<Tile> tiles, bool includeChildren = true)
        {
            for(int x = 0; x < SizeX; x++) {
                for(int y = 0; y < SizeY; y++) {
                    Tile tile = GetTile(x, y);
                    if(!includeChildren && tile.Parent != null)
                        continue;

                    if(tile.SourceLayer == layer) {
                        tiles.Add(tile);
                    }
                }
            }
        }

        public class Tile
        {
            public int X                    { get; private set; }
            public int Y                    { get; private set; }
            public Direction Rotation       { get; private set; }
            public Tile Parent              { get; private set; }
            public MapTile Prefab           { get; private set; }
            public TileRule SourceRule      { get; private set; }
            public ProcGenLayer SourceLayer { get; private set; }
            
            public bool Instantiated        { get; private set; }
            public MapTile Instance         { get; private set; }
            public Map Map                  { get; private set; }
            
            public Vector2Int WorldPosition => new((int)(X * Map.Config.TileSize), (int)(Y * Map.Config.TileSize));
            public bool IsNull              => Prefab == null && Parent == null;
            
            public Tile Initialize(int x, int y, Map map)
            {
                X   = x;
                Y   = y;
                Map = map;
                return this;
            }
            
            public void CopyFromParent(Tile parent)
            {
                Instantiated = true;
                Parent       = parent;
                Prefab       = parent.Prefab;
                Instance     = parent.Instance;
                SourceRule   = parent.SourceRule;
                SourceLayer  = parent.SourceLayer;
            }

            public void Set(MapTile prefab, Direction rotation, TileRule sourceRule, ProcGenLayer sourceLayer, Tile parent)
            {
                Prefab      = prefab;
                Rotation    = rotation;
                SourceRule  = sourceRule;
                SourceLayer = sourceLayer;
                Parent      = parent;
            }

            public void OnInstantiated(MapTile instance)
            {
                if(instance == null) {
                    Debug.LogError("Null instance.");
                    return;
                }
                Instantiated = true;
                Instance     = instance;
            }

            public void Clear()
            {
                X            = 0;
                Y            = 0;
                Instantiated = false;
                Parent       = null;
                Prefab       = null;
                SourceRule   = null;
                SourceLayer  = null;
                Instance     = null;
                Rotation     = Direction.North;
            }
        }
    }
    
    public interface IPostInstantiationCallback : ILockableOrderedListElement
    {
        UniTask OnPostTilesInstantiated(Stopwatch timer, Map map);
    }
}
