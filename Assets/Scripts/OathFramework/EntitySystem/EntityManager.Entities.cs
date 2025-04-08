using OathFramework.Core;
using OathFramework.Pooling;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AI;

namespace OathFramework.EntitySystem
{
    public sealed partial class EntityManager
    {
        private HashSet<Entity> allEntities                             = new();
        private Dictionary<EntityTeams, HashSet<Entity>> entitiesByTeam = new();
        private HashSet<Entity> toAdd                                   = new();
        private HashSet<Entity> toRemove                                = new();
        private bool isLocked;
        private bool parallel = true;

        private JobHandle prepareJobHandle;
        private JobHandle entityUpdateJobHandle;
        private NativeArray<Vector3> positions;
        
        [field: SerializeField] public int ProcessAIFrames  { get; private set; } = 5;
        [field: SerializeField] public int ReachCheckFrames { get; private set; } = 6;
        
        private static int curProcessAIFrame;
        private static int curReachCheckFrame;
        private static ObjectPool<NavMeshPath> pathsPool;
        private int allEntitiesArrayCount;
        
        [NonSerialized] public Vector3[] Positions       = new Vector3[1024];
        [NonSerialized] public Entity[] AllEntitiesArray = new Entity[1024];

        public static bool TryGetEntitiesByTeam(EntityTeams team, out HashSet<Entity> entities) 
            => Instance.entitiesByTeam.TryGetValue(team, out entities);

        public static int GetProcessAIFrame()
        {
            int frame = curProcessAIFrame++;
            if(curProcessAIFrame > Instance.ProcessAIFrames) {
                curProcessAIFrame = 0;
            }
            return frame;
        }

        public static int GetReachCheckFrame()
        {
            int frame = curReachCheckFrame++;
            if(curReachCheckFrame > Instance.ReachCheckFrames) {
                curReachCheckFrame = 0;
            }
            return frame;
        }

        public void RegisterEntity(Entity entity)
        {
            if(isLocked) {
                toAdd.Add(entity);
                return;
            }
            
            RegisterEntityInternal(entity);
        }

        public void UnregisterEntity(Entity entity)
        {
            if(isLocked) {
                if(toAdd.Contains(entity)) {
                    toAdd.Remove(entity);
                    return;
                }
                toRemove.Add(entity);
                return;
            }

            UnregisterEntityInternal(entity);
        }

        private void CleanUp()
        {
            foreach(Entity e in toAdd) {
                RegisterEntityInternal(e);
            }
            foreach(Entity e in toRemove) {
                UnregisterEntityInternal(e);
            }
            toAdd.Clear();
            toRemove.Clear();
            
            if(AllEntitiesArray.Length <= allEntities.Count) {
                int newSize      = AllEntitiesArray.Length * 2;
                AllEntitiesArray = new Entity[newSize];
                Positions        = new Vector3[newSize];
            } else {
                Array.Clear(AllEntitiesArray, 0, allEntitiesArrayCount);
            }
        }

        private void RegisterEntityInternal(Entity entity)
        {
            allEntities.Add(entity);
            if(!entitiesByTeam.TryGetValue(entity.Team, out HashSet<Entity> entities)) {
                entities = new HashSet<Entity>();
                entitiesByTeam.Add(entity.Team, entities);
            }
            entities.Add(entity);
        }

        private void UnregisterEntityInternal(Entity entity)
        {
            allEntities.Remove(entity);
            if(entitiesByTeam.TryGetValue(entity.Team, out HashSet<Entity> nodes)) {
                nodes.Remove(entity);
            }
        }
        
        private void DoEntityPreprocessing()
        {
            if(Game.State == GameState.Quitting)
                return;
            
            isLocked = true;
            CleanUp();

            allEntitiesArrayCount = allEntities.Count;
            positions             = new NativeArray<Vector3>(allEntitiesArrayCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            
            int iter = 0;
            foreach(Entity entity in allEntities) {
                AllEntitiesArray[iter] = entity;
                positions[iter]        = entity.transform.position;
                Positions[iter]        = positions[iter];
                iter++;
            }
            if(!parallel) {
                for(int i = 0; i < allEntitiesArrayCount; i++) {
                    AllEntitiesArray[i].Index = i;
                }
            }
            
            int count  = allEntitiesArrayCount;
            EntityJobs.PrepareJob prepareJob = new();
            EntityJobs.EntityUpdateJob updateJob = new();
            if(!parallel) {
                for(int i = 0; i < count; i++) { 
                    AllEntitiesArray[i].ParallelUpdate();
                }
                isLocked = false;
                return;
            }

            prepareJobHandle      = prepareJob.Schedule(count, 8);
            entityUpdateJobHandle = updateJob.Schedule(count, 8, prepareJobHandle);
        }

        private void FinishEntityPreprocessing()
        {
            if(Game.State == GameState.Quitting)
                return;
            
            entityUpdateJobHandle.Complete();
            isLocked = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NavMeshPath RetrievePath() => pathsPool.Retrieve();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReturnPath(NavMeshPath path)
        {
            path.ClearCorners();
            pathsPool.Return(path);
        }
    }
}
