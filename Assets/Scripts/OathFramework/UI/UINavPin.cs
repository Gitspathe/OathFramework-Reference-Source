using OathFramework.Core;
using OathFramework.UI.Platform;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.UI
{
    public class UINavPin : MonoBehaviour
    {
        [SerializeField] private bool tickOnEnable = true;
        [SerializeField] private uint priority;
        
        private static List<UINavPin> OpenPins = new();
        
        public bool IsPinned { get; private set; }

        private static void Sort()
        {
            if(OpenPins.Count == 0)
                return;
            
            OpenPins.Sort((x, y) => y.priority.CompareTo(x.priority));
            for(int i = 0; i < OpenPins.Count; i++) {
                UINavPin uiNavPin = OpenPins[i];
                uiNavPin.IsPinned = i == 0;
            }
        }
        
        private static void Tick()
        {
            Sort();
#if !UNITY_IOS && !UNITY_ANDROID
            if(Game.State == GameState.Quitting || UIControlsInputHandler.Instance == null)
                return;
            
            UIControlsInputHandler.PlayerInput.SwitchCurrentActionMap(OpenPins.Count == 0 ? "Player" : "UI");
#endif
            if(GameUI.Instance != null) {
                GameUI.Instance.TickAlwaysEnabledActions();
            }
        }

        public void Pin()
        {
            if(OpenPins.Contains(this))
                return;
            
            OpenPins.Add(this);
            Tick();
        }

        public void Unpin()
        {
            OpenPins.Remove(this);
            IsPinned = false;
            Tick();
        }
        
        private void OnEnable()
        {
            if(!tickOnEnable || OpenPins.Contains(this))
                return;
            
            OpenPins.Add(this);
            Tick();
        }

        private void OnDisable()
        {
            OpenPins.Remove(this);
            IsPinned = false;
            Tick();
        }
    }
}
