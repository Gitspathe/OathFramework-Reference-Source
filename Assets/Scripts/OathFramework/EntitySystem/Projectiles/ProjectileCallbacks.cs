using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{
    public class ProjectileCallbacks
    {
        private LockableHashSet<IOnPreProjectileSpawned> preSpawnedCallbacks = new();
        private LockableHashSet<IOnProjectileSpawned> spawnedCallbacks       = new();
        private LockableHashSet<IOnProjectileDespawned> despawnedCallbacks   = new();
        
        public ProjectileCallbacksAccessor Access { get; private set; }

        public ProjectileCallbacks()
        {
            Access = new ProjectileCallbacksAccessor(this);
        }

        public void Register(IOnPreProjectileSpawned callback) => preSpawnedCallbacks.Add(callback);
        public void Register(IOnProjectileSpawned callback) => spawnedCallbacks.Add(callback);
        public void Register(IOnProjectileDespawned callback) => despawnedCallbacks.Add(callback);

        public void Unregister(IOnPreProjectileSpawned callback) => preSpawnedCallbacks.Remove(callback);
        public void Unregister(IOnProjectileSpawned callback) => spawnedCallbacks.Remove(callback);
        public void Unregister(IOnProjectileDespawned callback) => despawnedCallbacks.Remove(callback);

        public sealed class ProjectileCallbacksAccessor : CallbackAccessor
        {
            private ProjectileCallbacks callbacks;

            public ProjectileCallbacksAccessor(ProjectileCallbacks callbacks)
            {
                this.callbacks = callbacks;
            }
            
            public void CallProjectilePreSpawned(AccessToken token, ref ProjectileParams @params, ref IProjectileData data)
            {
                EnsureAccess(token);
                if(callbacks.preSpawnedCallbacks.Count == 0)
                    return;
                
                callbacks.preSpawnedCallbacks.Lock();
                foreach(IOnPreProjectileSpawned preSpawn in callbacks.preSpawnedCallbacks.Current) {
                    try {
                        preSpawn.OnPreProjectileSpawned(ref @params, ref data);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.preSpawnedCallbacks.Unlock();
            }
        
            public void CallProjectileSpawned(AccessToken token, IProjectile projectile)
            {
                EnsureAccess(token);
                if(callbacks.spawnedCallbacks.Count == 0)
                    return;
                
                callbacks.spawnedCallbacks.Lock();
                foreach(IOnProjectileSpawned spawn in callbacks.spawnedCallbacks.Current) {
                    try {
                        spawn.OnProjectileSpawned(projectile);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.spawnedCallbacks.Unlock();
            }
            
            public void CallProjectileDespawned(AccessToken token, IProjectile projectile, bool missed)
            {
                EnsureAccess(token);
                if(callbacks.despawnedCallbacks.Count == 0)
                    return;
                
                callbacks.despawnedCallbacks.Lock();
                foreach(IOnProjectileDespawned spawn in callbacks.despawnedCallbacks.Current) {
                    try {
                        spawn.OnProjectileDespawned(projectile, missed);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.despawnedCallbacks.Unlock();
            }
        }
    }

    public interface IOnPreProjectileSpawned
    {
        void OnPreProjectileSpawned(ref ProjectileParams @params, ref IProjectileData data);
    }

    public interface IOnProjectileSpawned
    {
        void OnProjectileSpawned(IProjectile projectile);
    }

    public interface IOnProjectileDespawned
    {
        void OnProjectileDespawned(IProjectile projectile, bool missed);
    }
}
