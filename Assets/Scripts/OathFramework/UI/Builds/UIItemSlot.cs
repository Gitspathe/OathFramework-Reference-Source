using OathFramework.Core;
using OathFramework.EquipmentSystem;
using OathFramework.Progression;
using OathFramework.UI.Info;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI.Builds
{
    [RequireComponent(typeof(Button))]
    public class UIItemSlot : UIItemSlotBase, 
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, 
        IDeselectHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image background;
        [SerializeField] private Image border;
        [SerializeField] private Color unselectedColor;
        [SerializeField] private Color selectedColor;

        private bool isSelected;
        private Button button;
        private LayoutElement layoutElement;
        
        public override string ValueKey        { get; protected set; }
        public override EquipSlotType SlotType { get; protected set; }
        public byte Index                      { get; private set; }

        private ref PlayerBuildData BuildData => ref BuildMenuScript.CurBuildData;

        public void OnClicked()
        {
            if(BuildMenuScript.BuildDataChanged) {
                BuildMenuScript.DiscardChanges();
            }
            switch(SlotType) {
                case EquipSlotType.Equippable: {
                    BuildData.SetEquippable((EquipmentSlot)Index, ValueKey);
                } break;
                case EquipSlotType.Ability: {
                    if(isSelected) {
                        BuildData.IsAbilityEquipped(ValueKey, out byte i);
                        BuildData.SetAbility(i, "");
                    }
                    BuildData.SetAbility(Index, ValueKey);
                } break;
                case EquipSlotType.Perk: {
                    if(isSelected) {
                        BuildData.IsPerkEquipped(ValueKey, out byte i);
                        BuildData.SetPerk(i, "");
                    }
                    BuildData.SetPerk(Index, ValueKey);
                } break;
                
                default:
                case EquipSlotType.None:
                    Debug.LogError("Invalid UI slot type.");
                    break;
            }
            BuildMenuScript.ApplyChanges();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            BuildMenuScript.SetRightPanelDetails(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            BuildMenuScript.SetRightPanelDetails(null);
        }
        
        public void OnSelect(BaseEventData eventData)
        {
            BuildMenuScript.SetRightPanelDetails(this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            BuildMenuScript.SetRightPanelDetails(null);
        }

        private void SetSelected(bool val, bool takeControl = true)
        {
            isSelected = val;
            if(takeControl && GameControls.UsingController) {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
            ColorBlock colors  = button.colors;
            colors.normalColor = val ? selectedColor : unselectedColor;
            button.colors      = colors;
        }

        public UIItemSlot Initialize(string key, EquipSlotType type, byte index, bool isFirst)
        {
            button                        = GetComponent<Button>();
            layoutElement                 = GetComponent<LayoutElement>();
            ValueKey                      = key;
            Index                         = index;
            SlotType                      = type;
            layoutElement.preferredWidth  = 200;
            layoutElement.preferredHeight = 100;
            switch(type) {
                case EquipSlotType.Equippable: {
                    UIEquippableInfo info = UIInfoManager.GetEquippableInfo(key);
                    icon.sprite           = info == null ? null : info.Icon;
                    string curEquip       = BuildData.GetEquipment(type, index);
                    SetSelected(
                        curEquip == key, 
                        curEquip == key || string.IsNullOrEmpty(curEquip) && isFirst
                    );
                } break;
                case EquipSlotType.Ability: {
                    AbilityInfo info  = UIInfoManager.GetAbilityInfo(key);
                    icon.sprite       = info == null ? null : info.Icon;
                    SetSelected(
                        BuildData.IsAbilityEquipped(key), 
                        BuildData.GetAbility(index) == key || (string.IsNullOrEmpty(BuildData.GetAbility(index)) && isFirst)
                    );
                } break;
                case EquipSlotType.Perk: {
                    UIPerkInfo info = UIInfoManager.GetPerkInfo(key);
                    icon.sprite     = info == null ? null : info.Icon;
                    SetSelected(
                        BuildData.IsPerkEquipped(key), 
                        BuildData.GetPerk(index) == key || (string.IsNullOrEmpty(BuildData.GetPerk(index)) && isFirst)
                    );
                } break;
                
                case EquipSlotType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            return this;
        }
    }
}
