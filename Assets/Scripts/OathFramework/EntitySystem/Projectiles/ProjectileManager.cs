using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using OathFramework.Attributes;
using OathFramework.Core;
using OathFramework.EquipmentSystem;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem.Projectiles
{ 

    public sealed class ProjectileManager : Subsystem
    {
        [ArrayElementTitle, SerializeField] private List<HitSurfaceData> hitSurfaceMaterials = new();
        [SerializeField] private List<ProjectileTemplate> projectiles;
        
        private Dictionary<HitSurfaceMaterial, GameObject> hitSurfaceDictionary = new();
        private static Database projectileDB = new();

        public static ProjectileManager Instance { get; private set; }

        public override string Name    => "Projectile Manager";
        public override uint LoadOrder => SubsystemLoadOrders.ProjectileManager;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple '{nameof(ProjectileManager)}' singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            foreach(ProjectileTemplate template in projectiles) {
                if(!RegisterProjectileTemplate(template, out ushort id))
                    continue;
                
                PoolManager.RegisterPool(new PoolManager.GameObjectPool(template.PoolParams), true);
                template.ID = id;
            }
            foreach(HitSurfaceData data in hitSurfaceMaterials) {
                if(hitSurfaceDictionary.ContainsKey(data.Material)) {
                    Debug.LogError($"Duplicate hit surface data for material type '{data.Material}' found. Skipping.");
                    continue;
                }
                hitSurfaceDictionary.Add(data.Material, data.PoolParams.Prefab);
                if(!PoolManager.IsPrefabPooled(data.PoolParams.Prefab)) {
                    PoolManager.RegisterPool(new PoolManager.GameObjectPool(data.PoolParams), true);
                }
            }
            return UniTask.CompletedTask;
        }
        
        public static bool RegisterProjectileTemplate(ProjectileTemplate pTemplate, out ushort id)
        {
            id = 0;
            if(projectileDB.RegisterWithID(pTemplate.LookupKey, pTemplate, pTemplate.DefaultID)) {
                id = pTemplate.DefaultID;
                return true;
            }
            if(projectileDB.Register(pTemplate.LookupKey, pTemplate, out ushort retID)) {
                id = retID;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(ProjectileTemplate)} lookup '{pTemplate.LookupKey}'.");
            return false;
        }

        public static void CreateDecal(HitSurfaceMaterial material, Vector3 position, Vector3 normal, bool playSound = true, Transform parent = null)
        {
            if(!Instance.hitSurfaceDictionary.TryGetValue(material, out GameObject decalPrefab)) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"No decal for material {material} found.");
                }
                return;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            
            // TODO: Dynamic / parent objects are probably broken. remember the android dilemma!
            if(!ReferenceEquals(parent, null)) {
                //rotation *= Quaternion.Inverse(parent.rotation);
                position -= parent.position;
            }
            PoolManager.Retrieve(decalPrefab, position, rotation, null, parent);
        }

        public static bool TryGetProjectileTemplate(ushort projectileID, out ProjectileTemplate template)
        {
            if(projectileDB.TryGet(projectileID, out template, out _))
                return true;
            
            if(Game.ExtendedDebug) {
                Debug.LogWarning($"No projectile for type'{projectileID}' found.");
            }
            return false;
        }

        public static IProjectile CreateProjectile(
            in ProjectileParams @params,
            Entity source,
            bool isOwner,
            EntityTeams[] targets,
            in IProjectileData data)
        {
            if(!projectileDB.TryGet(@params.ProjectileID, out ProjectileTemplate template, out _)) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"No projectile for type'{@params.ProjectileID}' found.");
                }
                return null;
            }
            if(targets == null || targets.Length == 0) {
                targets = EntityTypes.AllTypes;
            }
            PoolableGameObject go  = PoolManager.Retrieve(template.PoolParams.Prefab, @params.Origin, @params.Rotation);
            IProjectile projectile = go.GetComponent<IProjectile>();
            projectile.Initialize(isOwner, targets, in data);
            return projectile;
        }
        
        private sealed class Database : Database<string, ushort, ProjectileTemplate>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }

    [Serializable]
    public class HitSurfaceData : IArrayElementTitle
    {
        [field: SerializeField] public HitSurfaceMaterial Material { get; private set; }
        [field: SerializeField] public PoolParams PoolParams       { get; private set; }

        string IArrayElementTitle.Name => Material.ToString();
    }

    public interface IProjectileDataProvider
    {
        IProjectileData GetProjectileData(Entity entity, ushort extraData);
    }

}
