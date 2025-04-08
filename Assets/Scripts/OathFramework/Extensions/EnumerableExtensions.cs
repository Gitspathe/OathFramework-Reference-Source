using OathFramework.EntitySystem;
using System;
using System.Collections.Generic;

namespace OathFramework.Extensions
{

    public static class EnumerableExtensions
    {
        public static bool Contains(this EntityTeams[] source, EntityTeams check)
        {
            for(int i = 0; i < source.Length; i++) {
                if(source[i] == check)
                    return true;
            }
            return false;
        }
        
        public static bool Contains<T>(this T[] source, T check)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for(int i = 0; i < source.Length; i++) {
                if(comparer.Equals(source[i], check))
                    return true;
            }
            return false;
        }
    }

}
