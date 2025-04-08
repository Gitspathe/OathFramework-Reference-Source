using Cysharp.Threading.Tasks;
using OathFramework.Pooling;
using OathFramework.UI.Platform;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

namespace OathFramework.UI 
{ 

    public class ModalUIScript : MonoBehaviour
    {
        [SerializeField] private GameObject modalPopup;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private Transform buttonParent;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private UIControlsInfoPanel controlsInfoPanel;
        [SerializeField] private LocalizedString closeString;

        private static List<ModalConfig> pending   = new();
        private static List<GameObject> curButtons = new();

        public static bool IsOpen => CurrentConf != null || pending.Count > 0;
        public static ModalConfig CurrentConf { get; private set; }
        public static ModalUIScript Instance  { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(ModalUIScript)} singletons.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private static void Sort()
        {
            if(pending.Count == 0)
                return;
            
            pending.Sort((x, y) => y.Priority.CompareTo(x.Priority));
            for(int i = 0; i < pending.Count; i++) {
                ModalConfig conf = pending[i];
                if(!conf.IsFocused && i == 0) {
                    Show(conf);
                } else if(conf.IsFocused && i != 0) {
                    Hide(conf);
                }
            }
        }

        private static void Show(ModalConfig config)
        {
            if(IsOpen) {
                ClosePopup();
            }
            
            ModalButtonScript prev = null;
            CurrentConf            = config;
            config.IsFocused       = true;
            Instance.title.text    = config.Title == null ? "" : config.Title.GetLocalizedString();
            Instance.text.text     = config.Text == null ? "" : config.Text.GetLocalizedString();
            Instance.controlsInfoPanel.SetNodes(config.ControlsInfo);
            for(int i = 0; i < config.Buttons.Count; i++) {
                (LocalizedString btnTxt, Action _) = config.Buttons[i];
                GameObject go                      = Instantiate(Instance.buttonPrefab, Instance.buttonParent);
                ModalButtonScript btn              = go.GetComponent<ModalButtonScript>().Setup(i, btnTxt);
                curButtons.Add(go);
                if(i == config.InitButton) {
                    btn.InitSelect();
                }
                if(prev != null) {
                    prev.SetRightNav(btn.Selectable);
                    btn.SetLeftNav(prev.Selectable);
                }
                prev = btn;
            }
            Instance.modalPopup.SetActive(true);
        }

        private static void Hide(ModalConfig config)
        {
            config.IsFocused = false;
        }

        public static void Open(ModalConfig config)
        {
            if(pending.Contains(config) || CurrentConf == config)
                return;
            
            pending.Add(config);
            Sort();
        }

        public static void Close(ModalConfig config)
        {
            bool wasPending = pending.Remove(config);
            if(CurrentConf != null && (wasPending || CurrentConf == config)) {
                CurrentConf.SelectLastIfValid();
                StaticObjectPool<ModalConfig>.Return(config.Reset());
            }
            if(CurrentConf == config) {
                ClosePopup();
                CurrentConf = null;
            }
            Sort();
        }

        public static void CloseAll()
        {
            pending.Clear();
            ClosePopup();
            CurrentConf = null;
        }

        private void Start()
        {
            ClosePopup();
        }

        public static ModalConfig ShowGeneric(
            LocalizedString text, 
            LocalizedString title      = null, 
            LocalizedString buttonText = null, 
            int priority               = ModalPriority.Low, 
            Action onButtonClicked     = null)
        {
            return ModalConfig.Retrieve()
                .WithPriority(priority)
                .WithTitle(title)
                .WithText(text)
                .WithButtons(new[] { (buttonText ?? Instance.closeString, onButtonClicked) })
                .Show();
        }

        private static void ClosePopup()
        {
            if(CurrentConf != null) {
                CurrentConf.IsFocused = false;
            }
            Instance.modalPopup.SetActive(false);
            foreach(GameObject go in curButtons) {
                Destroy(go);
            }
            curButtons.Clear();
        }

        public static void ModalButtonPressed(int index)
        {
            (LocalizedString _, Action action) = CurrentConf.Buttons[index];
            try {
                action?.Invoke();
            } catch(Exception e) {
                Debug.LogError(e);
            }
            Close(CurrentConf);
        }
    }

    public static class ModalPriority
    {
        public const int Low      = 1;
        public const int Medium   = 10;
        public const int High     = 100;
        public const int Critical = 1000;
    }

    public class ModalConfig
    {
        public int Priority                                    { get; private set; } = 1;
        public LocalizedString Title                           { get; private set; }
        public LocalizedString Text                            { get; private set; }
        public List<UIControlsInfoPanel.InfoNode> ControlsInfo { get; private set; } = new();
        public List<(LocalizedString, Action)> Buttons         { get; private set; } = new();
        public int InitButton                                  { get; private set; }
        public bool SelectLast                                 { get; private set; }
        
        public bool IsFocused                                  { get; set; }
        public GameObject LastSelected                         { get; private set; }
        
        public static ModalConfig Retrieve() => StaticObjectPool<ModalConfig>.Retrieve();

        public ModalConfig WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public ModalConfig WithTitle(LocalizedString title)
        {
            Title = title;
            return this;
        }

        public ModalConfig WithText(LocalizedString text)
        {
            Text = text;
            return this;
        }

        public ModalConfig WithControlsInfo(IEnumerable<UIControlsInfoPanel.InfoNode> controlsInfo)
        {
            ControlsInfo.AddRange(controlsInfo);
            return this;
        }

        public ModalConfig WithButtons(IEnumerable<(LocalizedString, Action)> buttons)
        {
            Buttons.Clear();
            Buttons.AddRange(buttons);
            return this;
        }

        public ModalConfig WithInitButton(int index)
        {
            InitButton = index;
            return this;
        }

        public ModalConfig WithSelectLast(bool selectLast)
        {
            SelectLast = selectLast;
            return this;
        }

        public ModalConfig Show()
        {
            if(SelectLast) {
                LastSelected = EventSystem.current.currentSelectedGameObject;
            }
            ModalUIScript.Open(this);
            return this;
        }

        public void Close()
        {
            ModalUIScript.Close(this);
        }

        public void SelectLastIfValid()
        {
            if(!SelectLast)
                return;
            
            _ = SelectLastDelayed(LastSelected);
        }

        private async UniTask SelectLastDelayed(GameObject go)
        {
            await UniTask.Yield();
            if(go == null || !go.activeInHierarchy)
                return;
            
            EventSystem.current.SetSelectedGameObject(go);
        }
        
        public ModalConfig Reset()
        {
            Priority     = 1;
            Title        = null;
            Text         = null;
            IsFocused    = false;
            SelectLast   = false;
            LastSelected = null;
            InitButton   = 0;
            ControlsInfo.Clear();
            Buttons.Clear();
            return this;
        }
    }

}
