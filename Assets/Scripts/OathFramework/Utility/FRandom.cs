using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OathFramework.Utility
{
    // Do not switch to struct. It breaks shit.
    public sealed class FRandom
    {
        private uint x;
        
        private static readonly ThreadLocal<FRandom> InternalCache = new(() => new FRandom((uint)Random.Range(0, uint.MaxValue)));

        public uint Seed { get; }

        public static FRandom Cache => InternalCache.Value;

        public FRandom(uint seed = 0)
        {
            if(seed == 0) { 
                seed = (uint)Random.Range(0, uint.MaxValue);
            }
            x    = seed;
            Seed = seed;
        }

        public uint UInt()
        {
            x      = unchecked(214013 * x + 2531011);
            uint y = x & 0x7FFFFFFF;
            y     ^= y >> 13;
            y     ^= y << 17;
            y     ^= y >> 5;
            return y;
        }

        public int Int()
        {
            x      = 214013 * x + 2531011;
            uint y = x & 0x7FFFFFFF;
            y     ^= y >> 13;
            y     ^= y << 17;
            y     ^= y >> 5;
            return (int)(y & 0x7FFFFFFF);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Int(int max)
        {
            if(max == 0)
                return 0;
            
            return Int() % max;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Range(int min, int max)
        {
            if(min >= max) 
                throw new ArgumentException("min must be less than max");

            int range = max - min;
            return min + Int() % range;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Float() => Int() / (float)int.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public float Float(float max) => max * Float();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Range(float min, float max) => min + (Float() * (max - min));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Angle() => Range(-Mathf.PI, Mathf.PI);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Angle(float max) => Range(-Mathf.PI, (-Mathf.PI + (Mathf.PI * 2.0f * max)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Vector(out Vector2 vector)
        {
            float angle = Angle();
            vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Vector()
        {
            float angle = Angle();
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
