using OathFramework.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.UI
{
    public class UITweenCallbackController : MonoBehaviour
    {
        private LockableOrderedList<ITweenShowCallback> showCallbacks = new();
        private LockableOrderedList<ITweenHideCallback> hideCallbacks = new();

        private void Awake()
        {
            foreach(ITweenShowCallback callback in GetComponents<ITweenShowCallback>()) {
                Register(callback);
            }
            foreach(ITweenHideCallback callback in GetComponents<ITweenHideCallback>()) {
                Register(callback);
            }
        }

        public void Register(ITweenShowCallback callback)   => showCallbacks.Add(callback);
        public void Register(ITweenHideCallback callback)   => hideCallbacks.Add(callback);
        public void Unregister(ITweenShowCallback callback) => showCallbacks.Remove(callback);
        public void Unregister(ITweenHideCallback callback) => hideCallbacks.Remove(callback);

        public void Show()
        {
            gameObject.SetActive(true);
            showCallbacks.Lock();
            foreach(ITweenShowCallback callback in showCallbacks.Current) {
                try {
                    callback.OnShow();
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
            showCallbacks.Unlock();
        }

        public void Hide()
        {
            hideCallbacks.Lock();
            foreach(ITweenHideCallback callback in hideCallbacks.Current) {
                try {
                    callback.OnHide();
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
            hideCallbacks.Unlock();
        }
    }

    public interface ITweenShowCallback : ILockableOrderedListElement
    {
        void OnShow();
    }
    
    public interface ITweenHideCallback : ILockableOrderedListElement
    {
        void OnHide();
    }
}
