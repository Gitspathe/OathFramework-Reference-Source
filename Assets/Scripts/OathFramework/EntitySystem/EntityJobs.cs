using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public static class EntityJobs
    {
        private static EntityManager cEntityMgrInst;

        public static void SetEntityManagerInstance(EntityManager manager)
        {
            cEntityMgrInst = manager;
        }
        
        public struct PrepareJob : IJobParallelFor
        {
            public void Execute(int index)
            {
                Entity entity = cEntityMgrInst.AllEntitiesArray[index];
                entity.Index  = index;
            }
        }
        
        public struct EntityUpdateJob : IJobParallelFor
        {
            public void Execute(int index) => cEntityMgrInst.AllEntitiesArray[index].ParallelUpdate();
        }
        
        [BurstCompile(FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float Distance(Vector3* a, Vector3* b)
        {
            double num1 = a->x - b->x;
            double num2 = a->y - b->y;
            double num3 = a->z - b->z;
            return (float)Math.Sqrt(num1 * num1 + num2 * num2 + num3 * num3);
        }
        
        [BurstCompile(FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vector3 a, Vector3 b)
        {
            double num1 = a.x - b.x;
            double num2 = a.y - b.y;
            double num3 = a.z - b.z;
            return (float)Math.Sqrt(num1 * num1 + num2 * num2 + num3 * num3);
        }
    }

    public struct EntityDistance
    {
        public Entity Entity;
        public float Distance;

        public EntityDistance(Entity entity, float distance)
        {
            Entity = entity;
            Distance = distance;
        }
    }
}
