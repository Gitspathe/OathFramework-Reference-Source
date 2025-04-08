using OathFramework.EquipmentSystem;
using OathFramework.Progression;
using OathFramework.UI.Info;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI.Builds
{

    public class UIEquipmentSlot : UIItemSlotBase
    {
        private EquipmentMenuScript equipmentMenu;
        private Navigation.Mode navMode;

        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image background;
        [SerializeField] private Image border;
        [SerializeField] private Color unselectedColor;
        [SerializeField] private Color selectedColor;

        [field: Space(10)]
        
        [field: SerializeField] public EquipSlotType Type { get; private set; }
        [field: SerializeField] public byte Index         { get; private set; }
        
        public bool IsSelected           { get; private set; }
        public override string ValueKey  { get; protected set; }

        public override EquipSlotType SlotType {
            get => Type; 
            protected set => Type = value;
        }

        private void UpdateSlot()
        {
            switch(Type) {
                case EquipSlotType.Equippable: {
                    UIEquippableInfo info = UIInfoManager.GetEquippableInfo(ValueKey);
                    if(info == null) {
                        SetNull();
                        return;
                    }
                    icon.sprite = info.Icon;
                    icon.color  = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                } break;
                case EquipSlotType.Ability: {
                    if(string.IsNullOrEmpty(ValueKey)) {
                        SetNull();
                        return;
                    }
                    AbilityInfo info = UIInfoManager.GetAbilityInfo(ValueKey);
                    if(info == null) {
                        SetNull();
                        return;
                    }
                    icon.sprite = info.Icon;
                    icon.color  = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                } break;
                case EquipSlotType.Perk: {
                    if(string.IsNullOrEmpty(ValueKey)) {
                        SetNull();
                        return;
                    }
                    UIPerkInfo info = UIInfoManager.GetPerkInfo(ValueKey);
                    if(info == null) {
                        SetNull();
                        return;
                    }
                    icon.sprite = info.Icon;
                    icon.color  = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                } break;

                default:
                case EquipSlotType.None:
                    Debug.LogError("Invalid equipment slot type.");
                    return;
            }
        }

        private void SetNull()
        {
            icon.sprite = null;
            icon.color  = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }

        public void SetData(string dataKey)
        {
            ValueKey = dataKey;
            UpdateSlot();
        }

        public void OnClicked()
        {
            equipmentMenu.OnEquipmentSlotClicked(this);
        }

        public void OnSelected()
        {
            equipmentMenu.OnEquipmentSlotSelected(this);
        }

        public void OnDeselect()
        {
            //equipmentMenu.OnEquipmentSlotClickAway(this);
        }

        public void SetNavigation(bool val)
        {
            Navigation nav    = button.navigation;
            nav.mode          = val ? navMode : Navigation.Mode.None;
            button.navigation = nav;
        }

        public void UIEventSystemSelect()
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        public void SetSelected(bool val)
        {
            ColorBlock colors  = button.colors;
            IsSelected         = val;
            colors.normalColor = val ? selectedColor : unselectedColor;
            button.colors      = colors;
        }

        public UIEquipmentSlot Initialize(EquipmentMenuScript menu)
        {
            equipmentMenu = menu;
            navMode       = button.navigation.mode;
            return this;
        }
    }

}
