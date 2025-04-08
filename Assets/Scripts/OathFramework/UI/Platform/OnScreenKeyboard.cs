using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.UI.Platform;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace OathFramework.UI
{

    public class OnScreenKeyboard : LoopComponent, ILoopLateUpdate
    {
        public GameObject mainPanel;
        public Selectable focus;
        public Color32 textColor;
        public Color32 mainColor;
        public Color32 specialColor;
        public Color32 backgroundColor;
        public Color32 highlightColor;
        public Color32 selectedColor;
        public Color32 pressedColor;
        public Sprite mainSprite;
        public Sprite specialSprite;
        public GameObject[] panels;
        public GameObject[] keys;
        public GameObject[] specialKeys;

        [Space(10)] 
        
        public UIControlsInfoPanel controlsPanel;
        public GameObject firstMainKey;
        public GameObject firstSymbolsKey;
        public GameObject firstSmilesKey;
        public GameObject firstNumericKey;

        private UINavigationGroup navGroup;
        private Selectable selectOnSubmit;
        private bool showNumeric;
        private bool cancelSupported;
        private KeyboardType curType;
        private CancellationTokenSource cts;

        [Space(10)]
        
        public LocalizedString okString;
        public LocalizedString cancelString;
        public LocalizedString backspaceString;
        public LocalizedString swapCaseString;

        [HideInInspector] public bool capsEnabled;
        
        public static bool SubmitPressed        { get; private set; }
        public static bool IsOpen               { get; private set; }
        public static OnScreenKeyboard Instance { get; private set; }

        public OnScreenKeyboard Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(OnScreenKeyboard)} singleton.");
                Destroy(Instance);
            }
            Instance = this;
            
            SetTextColor(textColor);
            SetMainColor(mainColor);
            SetSpecialColor(specialColor);
            SetBackgroundColor(backgroundColor);
            SetHighlightColor(highlightColor);
            SetPressedColor(pressedColor);
            SetSelectedColor(selectedColor);
            SetMainSprite(mainSprite);
            SetSpecialSprite(specialSprite);
            navGroup = GetComponent<UINavigationGroup>();
            foreach(GameObject go in keys) {
                navGroup.Register(go.GetComponent<Button>());
            }
            foreach(GameObject go in specialKeys) {
                navGroup.Register(go.GetComponent<Button>());
            }
            
            SetActive(false);
            return this;
        }

        public void ShowNumeric(bool b)
        {
            panels[3].SetActive(b);
            showNumeric = b;
        }

        public void SetHighlightColor(Color32 c)
        {
            foreach(GameObject go in keys) {
                Button btn              = go.GetComponent<Button>();
                ColorBlock colors       = btn.colors;
                colors.highlightedColor = c;
                btn.colors              = colors;
            }
            foreach(GameObject go in specialKeys) {
                Button btn              = go.GetComponent<Button>();
                ColorBlock colors       = btn.colors;
                colors.highlightedColor = c;
                btn.colors              = colors;
            }
        }
        
        public void SetPressedColor(Color32 c)
        {
            foreach(GameObject go in keys) {
                Button btn          = go.GetComponent<Button>();
                ColorBlock colors   = btn.colors;
                colors.pressedColor = c;
                btn.colors          = colors;
            }
            foreach(GameObject go in specialKeys) {
                Button btn          = go.GetComponent<Button>();
                ColorBlock colors   = btn.colors;
                colors.pressedColor = c;
                btn.colors          = colors;
            }
        }
        
        public void SetSelectedColor(Color32 c)
        {
            foreach(GameObject go in keys) {
                Button btn           = go.GetComponent<Button>();
                ColorBlock colors    = btn.colors;
                colors.selectedColor = c;
                btn.colors           = colors;
            }
            foreach(GameObject go in specialKeys) {
                Button btn           = go.GetComponent<Button>();
                ColorBlock colors    = btn.colors;
                colors.selectedColor = c;
                btn.colors           = colors;
            }
        }

        public void SetTextColor(Color32 c)
        {
            foreach(GameObject go in keys) {
                go.transform.Find("Text").GetComponent<Text>().color = c;
            }

            foreach(GameObject go in specialKeys) {
                go.transform.Find("Text").GetComponent<Text>().color = c;
            }
        }

        public void SetMainColor(Color32 c)
        {
            foreach(GameObject go in keys) {
                go.GetComponent<Image>().color = c;
            }
        }

        public void SetSpecialColor(Color32 c)
        {
            foreach(GameObject go in specialKeys) {
                go.GetComponent<Image>().color = c;
            }
        }

        public void SetBackgroundColor(Color32 c)
        {
            mainPanel.GetComponent<Image>().color = c;
        }

        public void SetMainSprite(Sprite s)
        {
            foreach(GameObject go in keys) {
                go.GetComponent<Image>().sprite = s;
            }
        }

        public void SetSpecialSprite(Sprite s)
        {
            foreach(GameObject go in specialKeys) {
                go.GetComponent<Image>().sprite = s;
            }
        }

        public void SetFocus(Selectable i)
        {
            focus = i;
        }

        public void SetActiveFocus(Selectable i, Selectable previous, KeyboardType type = KeyboardType.Main, bool showNumeric = true, bool canExit = false)
        {
            selectOnSubmit  = previous;
            cancelSupported = canExit;
            List<UIControlsInfoPanel.InfoNode> controlNodes = new() {
                new UIControlsInfoPanel.InfoNode("<UI>/OSKSubmit", okString)
            };
            if(canExit) {
                controlNodes.Add(new UIControlsInfoPanel.InfoNode("<UI>/OSKCancel", cancelString));
            }
            controlNodes.Add(new UIControlsInfoPanel.InfoNode("<UI>/OSKBackspace", backspaceString));
            if(type == KeyboardType.Main) {
                controlNodes.Add(new UIControlsInfoPanel.InfoNode("<UI>/OSKSwapCase", swapCaseString));
            }
            controlsPanel.SetNodes(controlNodes);
            
            focus = i;
            SetKeyboardType(type);
            ShowNumeric(showNumeric);
            SetActive(true);
            if(focus is InputField iField) {
                iField.MoveTextEnd(true);
            } else if(focus is TMP_InputField tmpIField) {
                tmpIField.MoveTextEnd(true);
            }
        }

        public void WriteKey(Text t)
        {
            if(!focus)
                return;

            if(focus is InputField iField) {
                if(iField.text.Length >= iField.characterLimit)
                    return;
                
                iField.text += t.text;
            } else if(focus is TMP_InputField tmpIField) {
                if(tmpIField.text.Length >= tmpIField.characterLimit)
                    return;
                
                tmpIField.text += t.text;
            }
        }

        public void WriteSpecialKey(int n)
        {
            switch(n) {
                case 0: {
                    if(!focus)
                        return;
                    
                    if(focus is InputField iField) {
                        if(iField.text.Length > 0) {
                            iField.text = iField.text.Substring(0, iField.text.Length - 1);
                        }
                    } else if(focus is TMP_InputField tmpIField) {
                        if(tmpIField.text.Length > 0) {
                            tmpIField.text = tmpIField.text.Substring(0, tmpIField.text.Length - 1);
                        }
                    }
                } break;
                case 1: {
                    EventSystem system = EventSystem.current;
                    if(focus == null)
                        return;

                    if(focus is InputField iField) {
                        iField.onSubmit.Invoke(iField.text);
                    } else if(focus is TMP_InputField tmpIField) {
                        tmpIField.onSubmit.Invoke(tmpIField.text);
                    }
                    GameControlsCallbacks.OSKSubmit(focus);
                    focus = null;
                    SetActive(false);
                } break;
                case 2:
                    SwitchCaps();
                    break;
                case 3:
                    SetActive(false);
                    break;
                case 4:
                    SetKeyboardType(KeyboardType.Symbols);
                    break;
                case 5:
                    SetKeyboardType(KeyboardType.Smiles);
                    break;
                case 6:
                    FocusPrevious();
                    break;
                case 7:
                    FocusNext();
                    break;
                case 8:
                    SetKeyboardType(KeyboardType.Main, false);
                    break;
            }
        }

        public void SetActive(bool b)
        {
            IsOpen = b;
            _ = b ? EnableTask() : DisableTask();
        }

        private async UniTask EnableTask()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            
            GameControlsCallbacks.OSKOpened(focus);
            navGroup.SetNavigation(true);
            await UniTask.Yield(cts.Token);
            mainPanel.SetActive(true);
        }

        private async UniTask DisableTask()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            
            navGroup.SetNavigation(false);
            GameControlsCallbacks.OSKClosed();
            await UniTask.Yield(cancellationToken: cts.Token);
            mainPanel.SetActive(false);
            if(selectOnSubmit != null) {
                EventSystem.current.SetSelectedGameObject(selectOnSubmit.gameObject);
            }
            selectOnSubmit = null;
        }

        public void SetCaps(bool b)
        {
            if(b) {
                foreach(GameObject go in keys) {
                    Text t = go.transform.Find("Text").GetComponent<Text>();
                    t.text = t.text.ToUpper();
                }
            } else {
                foreach(GameObject go in keys) {
                    Text t = go.transform.Find("Text").GetComponent<Text>();
                    t.text = t.text.ToLower();
                }
            }
            capsEnabled = b;
        }

        public void SwitchCaps()
        {
            SetCaps(!capsEnabled);
        }

        public void FocusPrevious()
        {
            EventSystem system = EventSystem.current;
            if(!focus)
                return;

            Selectable current = focus.GetComponent<Selectable>();
            Selectable next    = current.FindSelectableOnLeft();
            if(!next) {
                next = current.FindSelectableOnUp();
            }
            if(!next)
                return;

            Selectable inputField = next.GetComponent<Selectable>();
            if(inputField != null && inputField is InputField iField) {
                iField.OnPointerClick(new PointerEventData(system));
                focus = iField;
            } else if(inputField != null && inputField is TMP_InputField tmpIField) {
                tmpIField.OnPointerClick(new PointerEventData(system));
                focus = tmpIField;
            }
            system.SetSelectedGameObject(next.gameObject);
        }

        public void FocusNext()
        {
            EventSystem system = EventSystem.current;
            if(!focus)
                return;

            Selectable current = focus.GetComponent<Selectable>();
            Selectable prev    = current.FindSelectableOnRight();
            if(!prev) {
                prev = current.FindSelectableOnDown();
            }
            if(!prev)
                return;
            
            Selectable inputField = prev.GetComponent<Selectable>();
            if(inputField != null && inputField is InputField iField) {
                iField.OnPointerClick(new PointerEventData(system));
                focus = iField;
            } else if(inputField != null && inputField is TMP_InputField tmpIField) {
                tmpIField.OnPointerClick(new PointerEventData(system));
                focus = tmpIField;
            }
            system.SetSelectedGameObject(prev.gameObject);
        }

        public void SetKeyboardType(KeyboardType type, bool? numeric = null)
        {
            curType = type;
            panels[0].SetActive(false);
            panels[1].SetActive(false);
            panels[2].SetActive(false);
            switch(type) {
                case KeyboardType.Main:
                    panels[0].SetActive(true);
                    break;
                case KeyboardType.Symbols:
                    panels[1].SetActive(true);
                    break;
                case KeyboardType.Smiles:
                    panels[2].SetActive(true);
                    break;
            }
            if(numeric.HasValue) {
                panels[3].SetActive(numeric.Value);
            }
        }

        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(!IsOpen)
                return;
            
            // Fallback for weird shit.
            if(IsOpen && EventSystem.current.currentSelectedGameObject == null) {
                SelectionFallback();
            }
            
            if(UIControlsInputHandler.OSKSubmitAction.WasPressedThisFrame()) {
                WriteSpecialKey(1);
            }
            if(UIControlsInputHandler.OSKSwapCaseAction.WasPressedThisFrame()) {
                WriteSpecialKey(2);
            }
            if(UIControlsInputHandler.OSKBackspaceAction.WasPressedThisFrame()) {
                WriteSpecialKey(0);
            }
            if(cancelSupported && UIControlsInputHandler.OSKCancelAction.WasPressedThisFrame()) {
                WriteSpecialKey(3);
            }
        }

        private void SelectionFallback()
        {
            switch(curType) {
                case KeyboardType.None:
                    EventSystem.current.SetSelectedGameObject(firstNumericKey);
                    break;
                case KeyboardType.Main:
                    EventSystem.current.SetSelectedGameObject(firstMainKey);
                    break;
                case KeyboardType.Symbols:
                    EventSystem.current.SetSelectedGameObject(firstSymbolsKey);
                    break;
                case KeyboardType.Smiles:
                    EventSystem.current.SetSelectedGameObject(firstSmilesKey);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum KeyboardType
        {
            None    = 0,
            Main    = 1,
            Symbols = 2,
            Smiles  = 3
        }
    }

}
