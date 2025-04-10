using Cysharp.Threading.Tasks;
using OathFramework.Networking;
using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.Core
{
    public static class GameCallbacks
    {
        private static LockableOrderedList<IInitialized> initializedCallbacks             = new();
        private static LockableHashSet<ILoadLevelCompleted> levelLoadedCallbacks          = new();
        private static LockableHashSet<IPlayerDeathCallback> deathCallbacks               = new();
        private static LockableHashSet<IPlayerSpawnCallback> spawnCallbacks               = new();
        private static LockableHashSet<IPlayerConnectedCallback> connectedCallbacks       = new();
        private static LockableHashSet<IPlayerDisconnectedCallback> disconnectedCallbacks = new();
        private static LockableHashSet<IResetGameStateCallback> resetStateCallbacks       = new();
        private static LockableHashSet<IGameQuitCallback> quitCallbacks                   = new();
        private static LockableHashSet<IGameExitCallback> exitCallbacks                   = new();
        
        public static GameCallbacksAccess Access { get; private set; } = new();

        public static void Register(IInitialized callback) => initializedCallbacks.Add(callback);
        public static void Register(ILoadLevelCompleted callback) => levelLoadedCallbacks.Add(callback);
        public static void Register(IPlayerDeathCallback callback) => deathCallbacks.Add(callback);
        public static void Register(IPlayerSpawnCallback callback) => spawnCallbacks.Add(callback);
        public static void Register(IPlayerConnectedCallback callback) => connectedCallbacks.Add(callback);
        public static void Register(IPlayerDisconnectedCallback callback) => disconnectedCallbacks.Add(callback);
        public static void Register(IResetGameStateCallback callback) => resetStateCallbacks.Add(callback);
        public static void Register(IGameQuitCallback callback) => quitCallbacks.Add(callback);
        public static void Register(IGameExitCallback callback) => exitCallbacks.Add(callback);

        public static void Unregister(IInitialized callback) => initializedCallbacks.Remove(callback);
        public static void Unregister(ILoadLevelCompleted callback) => levelLoadedCallbacks.Remove(callback);
        public static void Unregister(IPlayerDeathCallback callback) => deathCallbacks.Remove(callback);
        public static void Unregister(IPlayerSpawnCallback callback) => spawnCallbacks.Remove(callback);
        public static void Unregister(IPlayerConnectedCallback callback) => connectedCallbacks.Remove(callback);
        public static void Unregister(IPlayerDisconnectedCallback callback) => disconnectedCallbacks.Remove(callback);
        public static void Unregister(IResetGameStateCallback callback) => resetStateCallbacks.Remove(callback);
        public static void Unregister(IGameQuitCallback callback) => quitCallbacks.Remove(callback);
        public static void Unregister(IGameExitCallback callback) => exitCallbacks.Remove(callback);
        
        public sealed class GameCallbacksAccess : CallbackAccessor
        {
            public async UniTask OnGameInitialized(AccessToken token)
            {
                EnsureAccess(token);
                if(initializedCallbacks.Count == 0)
                    return;
                
                initializedCallbacks.Lock();
                foreach(IInitialized callback in initializedCallbacks.Current) {
                    try {
                        await callback.OnGameInitialized();
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                initializedCallbacks.Unlock();
            }
            
            public void OnLevelLoaded(AccessToken token)
            {
                EnsureAccess(token);
                if(levelLoadedCallbacks.Count == 0)
                    return;
                
                levelLoadedCallbacks.Lock();
                foreach(ILoadLevelCompleted callback in levelLoadedCallbacks.Current) {
                    try {
                        callback.OnLevelLoaded();
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                levelLoadedCallbacks.Unlock();
            }
            
            public void OnPlayerDeath(AccessToken token, NetClient client)
            {
                EnsureAccess(token);
                if(deathCallbacks.Count == 0)
                    return;
                
                deathCallbacks.Lock();
                foreach(IPlayerDeathCallback callback in deathCallbacks.Current) {
                    try {
                        callback.OnPlayerDeath(client);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                deathCallbacks.Unlock();
            }
            
            public void OnPlayerSpawned(AccessToken token, NetClient client)
            {
                EnsureAccess(token);
                if(spawnCallbacks.Count == 0)
                    return;
                
                spawnCallbacks.Lock();
                foreach(IPlayerSpawnCallback callback in spawnCallbacks.Current) {
                    try {
                        callback.OnPlayerSpawned(client);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                spawnCallbacks.Unlock();
            }
            
            public void OnPlayerConnected(AccessToken token, NetClient client)
            {
                EnsureAccess(token);
                if(connectedCallbacks.Count == 0)
                    return;
                
                connectedCallbacks.Lock();
                foreach(IPlayerConnectedCallback callback in connectedCallbacks.Current) {
                    try {
                        callback.OnPlayerConnected(client);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                connectedCallbacks.Unlock();
            }
            
            public void OnPlayerDisconnected(AccessToken token, NetClient client)
            {
                EnsureAccess(token);
                if(disconnectedCallbacks.Count == 0)
                    return;
                
                disconnectedCallbacks.Lock();
                foreach(IPlayerDisconnectedCallback callback in disconnectedCallbacks.Current) {
                    try {
                        callback.OnPlayerDisconnected(client);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                disconnectedCallbacks.Unlock();
            }

            public void OnResetGameState(AccessToken token)
            {
                EnsureAccess(token);
                if(resetStateCallbacks.Count == 0)
                    return;
                
                resetStateCallbacks.Lock();
                foreach(IResetGameStateCallback callback in resetStateCallbacks.Current) {
                    try {
                        callback.OnResetGameState();
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                resetStateCallbacks.Unlock();
            }
            
            public void OnGameQuit(AccessToken token)
            {
                EnsureAccess(token);
                if(quitCallbacks.Count == 0)
                    return;
                
                quitCallbacks.Lock();
                foreach(IGameQuitCallback callback in quitCallbacks.Current) {
                    try {
                        callback.OnGameQuit();
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                quitCallbacks.Unlock();
            }
            
            public void OnGameExit(AccessToken token)
            {
                EnsureAccess(token);
                if(exitCallbacks.Count == 0)
                    return;
                
                exitCallbacks.Lock();
                foreach(IGameExitCallback callback in exitCallbacks.Current) {
                    try {
                        callback.OnGameExit();
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                exitCallbacks.Unlock();
            }
        }
    }

    public interface IInitialized : ILockableOrderedListElement
    {
        UniTask OnGameInitialized();
    }

    public interface ILoadLevelCompleted
    {
        void OnLevelLoaded();
    }

    public interface IPlayerDeathCallback
    {
        void OnPlayerDeath(NetClient client);
    }

    public interface IPlayerSpawnCallback
    {
        void OnPlayerSpawned(NetClient client);
    }

    public interface IPlayerConnectedCallback
    {
        void OnPlayerConnected(NetClient client);
    }

    public interface IPlayerDisconnectedCallback
    {
        void OnPlayerDisconnected(NetClient client);
    }

    public interface IResetGameStateCallback
    {
        void OnResetGameState();
    }

    public interface IGameQuitCallback
    {
        void OnGameQuit();
    }

    public interface IGameExitCallback
    {
        void OnGameExit();
    }
}
