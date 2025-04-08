using Cysharp.Threading.Tasks;
using OathFramework.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI.Platform
{

    public class UIInitialSelect : MonoBehaviour, IControlSchemeChangedCallback
    {
        [SerializeField] private uint priority;
        
        [field: SerializeField] public Selectable Selectable { get; private set; }

        private static PointerEventData eventDataCache;
        private static List<UIInitialSelect> Stack = new();

        private static void InitCache()
        {
            if(eventDataCache != null)
                return;

            eventDataCache = new PointerEventData(EventSystem.current) {
                button = PointerEventData.InputButton.Left
            };
        }
        
        public static void Sort()
        {
            InitCache();
            if(Stack.Count == 0 || !GameControls.UsingController)
                return;
            
            if(EventSystem.current.alreadySelecting) {
                _ = DelayedSort();
                return;
            }
            Stack.Sort((x, y) => y.priority.CompareTo(x.priority));
            Selectable select = Stack[0].Selectable;
            EventSystem.current.SetSelectedGameObject(select.gameObject);
            if(select is InputField iField) {
                iField.OnSelect(eventDataCache);
            } else if(select is TMP_InputField tmpIField) {
                tmpIField.OnSelect(eventDataCache);
            }
        }

        private static async UniTask DelayedSort()
        {
            await UniTask.Yield();
            Sort();
        }

        private void ApplyToStack()
        {
            if(Stack.Contains(this)) {
                Stack.Remove(this);
                Sort();
            }
            Stack.Add(this);
            Sort();
        }

        private void OnEnable()
        {
            GameControlsCallbacks.Register((IControlSchemeChangedCallback)this);
            if(Selectable == null) {
                Selectable = GetComponent<Selectable>();
            }
            ApplyToStack();
        }

        private void OnDisable()
        {
            GameControlsCallbacks.Unregister((IControlSchemeChangedCallback)this);
            Stack.Remove(this);
            Sort();
        }

        void IControlSchemeChangedCallback.OnControlSchemeChanged(ControlSchemes controlScheme)
        {
            if(!isActiveAndEnabled || controlScheme != ControlSchemes.Gamepad)
                return;
            
            ApplyToStack();
        }
    }

}
