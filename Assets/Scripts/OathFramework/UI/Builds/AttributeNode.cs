using OathFramework.Core;
using OathFramework.EntitySystem.Attributes;
using OathFramework.Progression;
using OathFramework.UI.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OathFramework.UI.Builds
{ 

    public class AttributeNode : LoopComponent, 
        ILoopUpdate, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private GameObject addButton;
        [SerializeField] private GameObject reduceButton;
        [SerializeField] private UINavPin navPin;
        private CharacterMenuScript buildCharacter;
        private Vector2 navLastFrame;
        private float time;
        
        public bool IsSelected { get; private set; }

        [field: SerializeField] public AttributeTypes AttributeType { get; private set; }

        public void Initialize(CharacterMenuScript buildCharacterScript)
        {
            buildCharacter = buildCharacterScript;
            Tick();
        }

        void ISelectHandler.OnSelect(BaseEventData eventData)     => IsSelected = true;
        void IDeselectHandler.OnDeselect(BaseEventData eventData) => IsSelected = false;

        protected override void OnDisable()
        {
            base.OnDisable();
            IsSelected   = false;
            navLastFrame = Vector2.zero;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(GameControls.UsingController && GetComponent<UIInitialSelect>() != null) {
                IsSelected = true;
            }
        }

        void ILoopUpdate.LoopUpdate()
        {
            if(!IsSelected)
                return;
            
            HandleController();
        }

        private void HandleController()
        {
            if(!GameControls.UsingController || !navPin.IsPinned)
                return;
            
            Vector2 nav = UIControlsInputHandler.Navigation;
            if(navLastFrame.x > 0.0f && nav.x < 0.0f) {
                ControllerNavChanged(nav);
            } else if(navLastFrame.x < 0.0f && nav.x > 0.0f) {
                ControllerNavChanged(nav);
            } else if(navLastFrame.x == 0.0f && nav.x != 0.0f) {
                ControllerNavChanged(nav);
            }
            navLastFrame = nav;
        }

        private void ControllerNavChanged(Vector2 nav)
        {
            if(nav.x > 0.125f && addButton.activeSelf) {
                AddButtonPressed();
            } else if(nav.x < -0.125f && reduceButton.activeSelf) {
                ReduceButtonPressed();
            }
        }

        public void AddButtonPressed()
        {
            buildCharacter.AddPressed(this);
            Tick();
        }

        public void ReduceButtonPressed()
        {
            buildCharacter.ReducePressed(this);
            Tick();
        }

        public void Tick()
        {
            PlayerBuildData data = BuildMenuScript.CurBuildData;
            numberText.text      = data.GetAttributeValue(AttributeType).ToString();
            addButton.SetActive(IsAddVisible(ref data));
            reduceButton.SetActive(IsReduceVisible(ref data));
        }

        private bool IsAddVisible(ref PlayerBuildData data)
        {
            return data.GetAttributeValue(AttributeType) < PlayerBuildData.MaxAttributeLevel
                && BuildMenuScript.Instance.Character.AvailablePoints > 0;
        }

        private bool IsReduceVisible(ref PlayerBuildData data)
        {
            // TODO: Level check.
            return data.GetAttributeValue(AttributeType) > PlayerBuildData.MinAttributeLevel;
        }
    }

}
