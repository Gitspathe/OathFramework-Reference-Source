using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using OathFramework.Extensions;
using OathFramework.EquipmentSystem;
using OathFramework.Utility;

namespace OathFramework.UI
{ 

    public class EquipmentHUDElementScript : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image iconCutout;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private Color cutoutColor;
        [SerializeField] private Color cutoutSelectedColor;

        [Space(10)] 
        
        [SerializeField] private TweenSettings iconTween;

        private EquipmentSlot slot;

        public bool IsSelected { 
            get => isSelected;

            set {
                if(value == isSelected)
                    return;

                isSelected = value;
                Tween.Color(iconCutout, isSelected ? cutoutSelectedColor : cutoutColor, iconTween);
            }
        }
        private bool isSelected;

        public void Initialize(EquipmentSlot slot)
        {
            this.slot           = slot;
            icon.preserveAspect = true;
        }

        public void SetValues(InventorySlot slot)
        {
            if(slot.IsEmpty) {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            ammoText.SetText(StringBuilderCache.Retrieve.Concat(slot.Ammo));
            if(slot.HasAmmoCount) {
                EquippableStats stats = slot.GetStatsAs<EquippableStats>();
                iconCutout.fillAmount = slot.Ammo == 0 ? 0.0f : (float)slot.Ammo / stats.AmmoCapacity;
            } else {
                iconCutout.fillAmount = 1.0f;
            }
            if(ReferenceEquals(slot.Equippable.UIInfo, null)) {
                icon.sprite       = null;
                iconCutout.sprite = null;
                return;
            }
            icon.sprite       = slot.Equippable.UIInfo.Icon;
            iconCutout.sprite = slot.Equippable.UIInfo.Icon;
        }

        private void Awake()
        {
            icon.preserveAspect       = true;
            iconCutout.color          = cutoutColor;
            iconCutout.preserveAspect = true;
            iconCutout.fillOrigin     = 0;
            iconCutout.type           = Image.Type.Filled;
            iconCutout.fillMethod     = Image.FillMethod.Horizontal;
        }
    }

}
