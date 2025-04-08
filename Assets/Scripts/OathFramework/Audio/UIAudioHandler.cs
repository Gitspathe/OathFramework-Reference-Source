using OathFramework.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.Audio
{
    public class UIAudioHandler : LoopComponent, 
        ILoopUpdate, IPointerEnterHandler, ISelectHandler,
        IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] private UIAudioHandlerType type;
        [SerializeField] private bool playNavSound = true;
        [SerializeField] private bool playSubmitSound = true;
        [SerializeField] private Overrides[] overrides;

        private UnityAction<float> onSliderChanged;
        private Slider slider;
        private float lastVal;
        private bool firstUpdate = true;
        
        private void Awake()
        {
            if(type == UIAudioHandlerType.Slider) {
                slider = GetComponent<Slider>();
                slider.onValueChanged.AddListener(ValueChangeCheck);
                lastVal = slider.value;
            }
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            firstUpdate = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            firstUpdate = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            firstUpdate = true;
        }

        private void OnDestroy()
        {
            if(type == UIAudioHandlerType.Slider) {
                slider.onValueChanged.RemoveListener(ValueChangeCheck);
            }
        }
        
        public void ValueChangeCheck(float val)
        {
            UIAudio.PlaySound(val >= lastVal ? UISounds.SliderIncrement : UISounds.SliderDecrement, overrides);
            lastVal = val;
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if(!playNavSound || firstUpdate)
                return;
            
            UIAudio.PlaySound(UISounds.Nav, overrides);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if(!playSubmitSound)
                return;
            
            UIAudio.PlaySound(type == UIAudioHandlerType.Back ? UISounds.Back : UISounds.Confirm, overrides);
        }

        void ISubmitHandler.OnSubmit(BaseEventData eventData)
        {
            if(!playSubmitSound)
                return;
            
            UIAudio.PlaySound(type == UIAudioHandlerType.Back ? UISounds.Back : UISounds.Confirm, overrides);
        }

        void ISelectHandler.OnSelect(BaseEventData eventData)
        {
            if(!playNavSound || firstUpdate)
                return;
            
            UIAudio.PlaySound(UISounds.Nav, overrides);
        }

        [System.Serializable]
        public class Overrides
        {
            [field: SerializeField] public UISounds SoundType        { get; private set; }
            [field: SerializeField] public AudioParams OverrideSound { get; private set; }
        }
    }

    public enum UIAudioHandlerType
    {
        Default,
        Slider, 
        Back
    }
}
