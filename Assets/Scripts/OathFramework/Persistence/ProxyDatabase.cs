using OathFramework.Persistence;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Persistence
{
    public static class ProxyDatabase<T> where T : PersistentProxy
    {
        private static Dictionary<string, T> proxies = new();
        
        public static void Register(string key, T proxy)
        {
            if(!proxies.TryAdd(key, proxy)) {
                Debug.LogError($"Attempted to register duplicate proxy for '{key}'");
            }
        }

        public static void Unregister(string key)
        {
            proxies.Remove(key);
        }

        public static bool TryGetProxy(string key, out T proxy)
        {
            return proxies.TryGetValue(key, out proxy);
        }
    }
}
