using OathFramework.Networking;
using OathFramework.Core;
using OathFramework.Persistence;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public partial class EntityTargeting : NetworkBehaviour, IPoolableComponent, IPersistableComponent
    {
        public Entity Entity        { get; private set; }
        public Entity CurrentTarget { get; private set; }
        public int TargetedLevel    { get; private set; }
        public bool ValidState => EntityManager.Instance != null && !Entity.IsDummy && Game.State == GameState.InGame;

        [field: SerializeField] public int ThreatLevel { get; private set; }
        private HashSet<Entity> TargetedBy             { get; set; } = new();
        
        public PoolableGameObject PoolableGO { get; set; }
        
        private void Awake()
        {
            Entity = GetComponent<Entity>();
        }

        private void OnEnable()
        {
            if(!ValidState)
                return;
            
            EntityManager.Instance.RegisterEntity(Entity);
        }

        private void OnDisable()
        {
            if(Game.State == GameState.Quitting)
                return;
            
            ChangeTarget(null);
            EntityManager.Instance.UnregisterEntity(Entity);
        }

        public void OnRetrieve()
        {
            
        }

        public void ChangeTarget(Entity target)
        {
            if(CurrentTarget != null) {
                CurrentTarget.Targeting.TargetedBy.Remove(Entity);
                CurrentTarget.Targeting.UpdateTargetedLevel();
            }
            CurrentTarget = target;
            if(CurrentTarget != null) {
                CurrentTarget.Targeting.TargetedBy.Add(Entity);
                CurrentTarget.Targeting.UpdateTargetedLevel();
            }
        }

        private void UpdateTargetedLevel()
        {
            TargetedLevel = 0;
            foreach(Entity entity in TargetedBy) {
                TargetedLevel += entity.Targeting.ThreatLevel;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDistance(Entity other)
        {
            Vector3[] positions = EntityManager.Instance.Positions;
            return EntityJobs.Distance(positions[Entity.Index], positions[other.Index]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetDistances(List<EntityDistance> distancesRef, EntityTeams team, bool sort = true)
        {
            if(!EntityManager.TryGetEntitiesByTeam(team, out HashSet<Entity> entities))
                return;
            
            Vector3[] positions = EntityManager.Instance.Positions;
            foreach(Entity other in entities) {
                distancesRef.Add(new EntityDistance(other, EntityJobs.Distance(positions[Entity.Index], positions[other.Index])));
            }
            if(sort) {
                distancesRef.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetDistances(QList<EntityDistance> distancesRef, EntityTeams team)
        {
            if(!EntityManager.TryGetEntitiesByTeam(team, out HashSet<Entity> entities))
                return;
            
            Vector3[] positions = EntityManager.Instance.Positions;
            foreach(Entity other in entities) {
                distancesRef.Add(new EntityDistance(other, EntityJobs.Distance(positions[Entity.Index], positions[other.Index])));
            }
        }
        
        public void OnReturn(bool initialization)
        {
            if(!ValidState)
                return;
            
            ChangeTarget(null);
            TargetedBy.Clear();
            UpdateTargetedLevel();
        }
    }

}
