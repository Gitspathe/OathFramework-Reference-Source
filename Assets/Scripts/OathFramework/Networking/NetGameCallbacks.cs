using OathFramework.Utility;
using System;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Networking
{
    public sealed class NetGameCallbacks
    {
        private LockableOrderedList<ISceneBeganLoading> beganLoadingCallbacks                              = new();
        private LockableOrderedList<IClientSceneLoadCompleted> clientSceneLoadCompletedCallbacks           = new();
        private LockableOrderedList<ISceneLoadEventCompleted> sceneLoadEventCompletedCallbacks             = new();
        private LockableOrderedList<IClientSceneIntegrateCompleted> clientSceneIntegrateCompletedCallbacks = new();
        private LockableOrderedList<ISceneIntegrateEventCompleted> sceneIntegrateEventCompletedCallbacks   = new();

        public void Register(ISceneBeganLoading callback) => beganLoadingCallbacks.Add(callback);
        public void Register(IClientSceneLoadCompleted callback) => clientSceneLoadCompletedCallbacks.Add(callback);
        public void Register(ISceneLoadEventCompleted callback) => sceneLoadEventCompletedCallbacks.Add(callback);
        public void Register(IClientSceneIntegrateCompleted callback) => clientSceneIntegrateCompletedCallbacks.Add(callback);
        public void Register(ISceneIntegrateEventCompleted callback) => sceneIntegrateEventCompletedCallbacks.Add(callback);

        public void Unregister(ISceneBeganLoading callback) => beganLoadingCallbacks.Remove(callback);
        public void Unregister(IClientSceneLoadCompleted callback) => clientSceneLoadCompletedCallbacks.Remove(callback);
        public void Unregister(ISceneLoadEventCompleted callback) => sceneLoadEventCompletedCallbacks.Remove(callback);
        public void Unregister(IClientSceneIntegrateCompleted callback) => clientSceneIntegrateCompletedCallbacks.Remove(callback);
        public void Unregister(ISceneIntegrateEventCompleted callback) => sceneIntegrateEventCompletedCallbacks.Remove(callback);

        public NetGameCallbacksAccess Access { get; private set; }

        public NetGameCallbacks()
        {
            Access = new NetGameCallbacksAccess(this);
        }
        
        public sealed class NetGameCallbacksAccess : CallbackAccessor
        {
            private NetGameCallbacks callbacks;

            public NetGameCallbacksAccess(NetGameCallbacks callbacks)
            {
                this.callbacks = callbacks;
            }
            
            public void OnBeganLoadingScene(AccessToken token, SceneEvent sceneEvent)
            {
                EnsureAccess(token);
                if(callbacks.beganLoadingCallbacks.Count == 0)
                    return;
                
                callbacks.beganLoadingCallbacks.Lock();
                foreach(ISceneBeganLoading callback in callbacks.beganLoadingCallbacks.Current) {
                    try {
                        callback.OnSceneBeganLoading(sceneEvent);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.beganLoadingCallbacks.Unlock();
            }
            
            public void OnClientLoadedScene(AccessToken token, SceneEvent sceneEvent)
            {
                EnsureAccess(token);
                if(callbacks.clientSceneLoadCompletedCallbacks.Count == 0)
                    return;
                
                callbacks.clientSceneLoadCompletedCallbacks.Lock();
                foreach(IClientSceneLoadCompleted callback in callbacks.clientSceneLoadCompletedCallbacks.Current) {
                    try {
                        callback.OnClientSceneLoadCompleted(sceneEvent);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.clientSceneLoadCompletedCallbacks.Unlock();
            }
            
            public void OnLoadSceneEventCompleted(AccessToken token, SceneEvent sceneEvent)
            {
                EnsureAccess(token);
                if(callbacks.sceneLoadEventCompletedCallbacks.Count == 0)
                    return;
                
                callbacks.sceneLoadEventCompletedCallbacks.Lock();
                foreach(ISceneLoadEventCompleted callback in callbacks.sceneLoadEventCompletedCallbacks.Current) {
                    try {
                        callback.OnSceneLoadEventCompleted(sceneEvent);
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.sceneLoadEventCompletedCallbacks.Unlock();
            }

            public void OnClientSceneIntegrateCompleted(AccessToken token)
            {
                EnsureAccess(token);
                if(callbacks.clientSceneIntegrateCompletedCallbacks.Count == 0)
                    return;
                
                callbacks.clientSceneIntegrateCompletedCallbacks.Lock();
                foreach(IClientSceneIntegrateCompleted callback in callbacks.clientSceneIntegrateCompletedCallbacks.Current) {
                    try {
                        callback.OnClientSceneIntegrateCompleted();
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.clientSceneIntegrateCompletedCallbacks.Unlock();
            }
            
            public void OnSceneIntegrateEventCompleted(AccessToken token)
            {
                EnsureAccess(token);
                if(callbacks.sceneIntegrateEventCompletedCallbacks.Count == 0)
                    return;
                
                callbacks.sceneIntegrateEventCompletedCallbacks.Lock();
                foreach(ISceneIntegrateEventCompleted callback in callbacks.sceneIntegrateEventCompletedCallbacks.Current) {
                    try {
                        callback.OnSceneIntegrateEventCompleted();
                    } catch(Exception e) {
                        Debug.LogError(e);
                    }
                }
                callbacks.sceneIntegrateEventCompletedCallbacks.Unlock();
            }
        }
    }
    
    public interface ISceneBeganLoading : ILockableOrderedListElement
    {
        void OnSceneBeganLoading(SceneEvent sceneEvent);
    }
    
    public interface IClientSceneLoadCompleted : ILockableOrderedListElement
    {
        void OnClientSceneLoadCompleted(SceneEvent sceneEvent);
    }
    
    public interface ISceneLoadEventCompleted : ILockableOrderedListElement
    {
        void OnSceneLoadEventCompleted(SceneEvent sceneEvent);
    }

    public interface IClientSceneIntegrateCompleted : ILockableOrderedListElement
    {
        void OnClientSceneIntegrateCompleted();
    }

    public interface ISceneIntegrateEventCompleted : ILockableOrderedListElement
    {
        void OnSceneIntegrateEventCompleted();
    }
}
