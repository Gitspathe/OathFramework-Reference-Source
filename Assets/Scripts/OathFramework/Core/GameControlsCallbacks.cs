using OathFramework.Utility;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.Core
{
    public class GameControlsCallbacks
    {
        private static LockableHashSet<IControlSchemeChangedCallback> controlSchemeCallbacks = new();
        private static LockableHashSet<IOnScreenKeyboardCallback> onScreenKeyboardCallbacks  = new();

        public static void ControlSchemeChanged(ControlSchemes controlScheme)
        {
            controlSchemeCallbacks.Lock();
            foreach(IControlSchemeChangedCallback callback in controlSchemeCallbacks.Current) {
                try {
                    callback.OnControlSchemeChanged(controlScheme);
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
            controlSchemeCallbacks.Unlock();
        }
        
        public static void OSKOpened(Selectable target)
        {
            onScreenKeyboardCallbacks.Lock();
            foreach(IOnScreenKeyboardCallback callback in onScreenKeyboardCallbacks.Current) {
                try {
                    callback.OnOSKOpened(target);
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
            onScreenKeyboardCallbacks.Unlock();
        }

        public static void OSKSubmit(Selectable target)
        {
            onScreenKeyboardCallbacks.Lock();
            foreach(IOnScreenKeyboardCallback callback in onScreenKeyboardCallbacks.Current) {
                try {
                    callback.OnOSKSubmit(target);
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
            onScreenKeyboardCallbacks.Unlock();
        }
        
        public static void OSKClosed()
        {
            onScreenKeyboardCallbacks.Lock();
            foreach(IOnScreenKeyboardCallback callback in onScreenKeyboardCallbacks.Current) {
                try {
                    callback.OnOSKClosed();
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
            onScreenKeyboardCallbacks.Unlock();
        }
        
        public static void Register(IControlSchemeChangedCallback callback) => controlSchemeCallbacks.Add(callback);
        public static void Register(IOnScreenKeyboardCallback callback) => onScreenKeyboardCallbacks.Add(callback);

        public static void Unregister(IControlSchemeChangedCallback callback) => controlSchemeCallbacks.Remove(callback);
        public static void Unregister(IOnScreenKeyboardCallback callback) => onScreenKeyboardCallbacks.Remove(callback);
    }
    
    public interface IControlSchemeChangedCallback
    {
        void OnControlSchemeChanged(ControlSchemes controlScheme);
    }

    public interface IOnScreenKeyboardCallback
    {
        void OnOSKOpened(Selectable target);
        void OnOSKSubmit(Selectable target);
        void OnOSKClosed();
    }
}
