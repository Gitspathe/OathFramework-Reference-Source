using OathFramework.Core;
using OathFramework.EquipmentSystem;
using OathFramework.Progression;
using OathFramework.UI.Platform;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace OathFramework.UI.Builds
{ 

    public class EquipmentMenuScript : MonoBehaviour
    {
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private GameObject smallItemPrefab;
        [SerializeField] private GameObject largeItemPrefab;
        [SerializeField] private Transform itemParent;
        [SerializeField] private GridLayoutGroup itemLayout;
        [SerializeField] private ScrollRect itemRect;
        [SerializeField] private TextMeshProUGUI itemPanelTitle;
        [SerializeField] private Selectable startBtn;
        [SerializeField] private UINavigationGroup[] toggleNavGroupsOnOpen;

        [Header("Localization")]
        [SerializeField] private LocalizedString magitechSubtitleMsg;
        [SerializeField] private LocalizedString perkSubtitleMsg;
        [SerializeField] private LocalizedString primaryWeaponSubtitleMsg;
        [SerializeField] private LocalizedString secondaryWeaponSubtitleMsg;

        private EquipSlotType curEquipSlot;
        private byte curSlotIndex;
        
        private List<UIEquipmentSlot> allSlots                = new();
        private Dictionary<int, UIEquipmentSlot> weaponSlots  = new();
        private Dictionary<int, UIEquipmentSlot> abilitySlots = new();
        private Dictionary<int, UIEquipmentSlot> perkSlots    = new();
        private List<UIItemSlot> itemSlots                    = new();
        private List<string> keysCache                        = new();
        private bool firstRun                                 = true;

        public bool IsVisible => equipmentPanel.activeSelf;
        public UIEquipmentSlot CurSelected { get; private set; }

        public EquipmentMenuScript Initialize()
        {
            LocalizationSettings.SelectedLocaleChanged += LocalizationChanged;
            foreach(UIEquipmentSlot slot in GetComponentsInChildren<UIEquipmentSlot>(true)) {
                RegisterSlot(slot.Initialize(this));
            }
            return this;
        }

        private void LocalizationChanged(Locale newLocale)
        {
            SetupSubPanel(curEquipSlot, curSlotIndex);
        }

        public void OnEquipmentSlotClicked(UIItemSlotBase slot)
        {
            if(slot is UIEquipmentSlot equip) {
                CurSelected = equip;
                foreach(UIEquipmentSlot otherSlot in allSlots) {
                    otherSlot.SetSelected(otherSlot == CurSelected);
                }
                SetupSubPanel(equip.Type, equip.Index);
                string curEquipKey = BuildMenuScript.CurBuildData.GetEquipment(equip.Type, equip.Index);
                BuildMenuScript.SetRightPanelDetails(GetItemSlotForKey(equip.Type, curEquipKey));
                return;
            }
            BuildMenuScript.SetRightPanelDetails(slot);
        }

        public void OnEquipmentSlotSelected(UIEquipmentSlot slot)
        {
            BuildMenuScript.SetRightPanelDetails(slot);
        }

        public void CloseSubPanel()
        {
            if(CurSelected != null) {
                CurSelected.UIEventSystemSelect();
                CurSelected.SetSelected(false);
                CurSelected = null;
            }
            SetupSubPanel(EquipSlotType.None, 0);
        }

        public UIItemSlot GetItemSlotForKey(EquipSlotType type, string equippableKey)
        {
            foreach(UIItemSlot slot in itemSlots) {
                if(slot.SlotType == type && slot.ValueKey == equippableKey) {
                    return slot;
                }
            }
            return null;
        }

        public void UpdateSlots()
        {
            foreach(UIEquipmentSlot slot in allSlots) {
                slot.SetData(BuildMenuScript.CurBuildData.GetEquipment(slot.Type, slot.Index));
            }
            if(CurSelected != null) {
                SetupSubPanel(CurSelected.Type, CurSelected.Index);
            }
        }

        public void Show()
        {
            UpdateSlots();
            if(firstRun) {
                firstRun             = false;
                UIEquipmentSlot slot = weaponSlots[1];
                if(!string.IsNullOrEmpty(slot.ValueKey)) {
                    BuildMenuScript.SetRightPanelDetails(slot);
                }
            }
            equipmentPanel.SetActive(true);
            itemRect.verticalScrollbar.value = 0.0f;
        }

        public void Hide()
        {
            CloseSubPanel();
            CurSelected = null;
            equipmentPanel.SetActive(false);
        }
        
        private void RegisterSlot(UIEquipmentSlot slot)
        {
            switch(slot.Type) {
                case EquipSlotType.Equippable: {
                    if(!weaponSlots.TryAdd(slot.Index, slot)) {
                        Debug.LogError($"Attempted to register duplicate equippable slot with index '{slot.Index}'");
                        return;
                    }
                } break;
                case EquipSlotType.Ability: {
                    if(!abilitySlots.TryAdd(slot.Index, slot)) {
                        Debug.LogError($"Attempted to register duplicate ability slot with index '{slot.Index}'");
                        return;
                    }
                } break;
                case EquipSlotType.Perk: {
                    if(!perkSlots.TryAdd(slot.Index, slot)) {
                        Debug.LogError($"Attempted to register duplicate perk slot with index '{slot.Index}'");
                        return;
                    }
                } break;
                    
                default:
                case EquipSlotType.None:
                    return;
            }
            allSlots.Add(slot);
        }

        private void SetupSubPanel(EquipSlotType slotType, byte index)
        {
            bool resetScroll                 = slotType != curEquipSlot;
            float scrollPos                  = itemRect.verticalNormalizedPosition;
            curEquipSlot                     = slotType;
            curSlotIndex                     = index;
            itemRect.verticalScrollbar.value = 0.0f;
            keysCache.Clear();
            ClearItemSlots();
            if(GameControls.UsingController) {
                foreach(UIEquipmentSlot slot in allSlots) {
                    slot.SetNavigation(slotType == EquipSlotType.None);
                }
                foreach(UINavigationGroup navGroup in toggleNavGroupsOnOpen) {
                    navGroup.SetNavigation(slotType == EquipSlotType.None);
                }
                startBtn.interactable = slotType == EquipSlotType.None;
            }
            switch(slotType) {
                case EquipSlotType.Equippable: {
                    EquipmentSlot slot  = (EquipmentSlot)index;
                    itemLayout.cellSize = new Vector2(200, 100);
                    BuildMenuScript.Profile.GetUnlockedEquippables((EquipmentSlot)index, keysCache);
                    SpawnItemSlots(keysCache, EquipSlotType.Equippable, (byte)slot);
                    SetPanelTitleWeapon(slot);
                } break;
                case EquipSlotType.Ability: {
                    itemLayout.cellSize = new Vector2(100, 100);
                    itemPanelTitle.text = magitechSubtitleMsg.GetLocalizedString();
                    BuildMenuScript.Profile.GetUnlockedAbilities(keysCache);
                    SpawnItemSlots(keysCache, EquipSlotType.Ability, index);
                } break;
                case EquipSlotType.Perk: {
                    itemLayout.cellSize = new Vector2(100, 100);
                    itemPanelTitle.text = perkSubtitleMsg.GetLocalizedString();
                    BuildMenuScript.Profile.GetUnlockedPerks(keysCache);
                    SpawnItemSlots(keysCache, EquipSlotType.Perk, index);
                } break;
                
                case EquipSlotType.None:
                    itemPanelTitle.text = "";
                    return;
                default:
                    Debug.LogError("Invalid slotType index.");
                    return;
            }
            if(!resetScroll) {
                itemRect.verticalNormalizedPosition = scrollPos;
            }
        }
        
        private void SetPanelTitleWeapon(EquipmentSlot slot)
        {
            switch(slot) {
                case EquipmentSlot.Primary:
                    itemPanelTitle.text = primaryWeaponSubtitleMsg.GetLocalizedString();
                    break;
                case EquipmentSlot.Secondary:
                    itemPanelTitle.text = secondaryWeaponSubtitleMsg.GetLocalizedString();
                    break;
                        
                case EquipmentSlot.None:
                default:
                    itemPanelTitle.text = "I AM ERROR";
                    break;
            }
        }
        
        private void ClearItemSlots()
        {
            foreach(UIItemSlot slot in itemSlots) {
                Destroy(slot.gameObject);
            }
            itemSlots.Clear();
        }

        private void SpawnItemSlots(List<string> keys, EquipSlotType type, byte index)
        {
            for(int i = 0; i < keys.Count; i++) {
                string key      = keys[i];
                GameObject go   = Instantiate(largeItemPrefab, itemParent);
                UIItemSlot slot = go.GetComponent<UIItemSlot>().Initialize(key, type, index, i == 0);
                itemSlots.Add(slot);
            }
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= LocalizationChanged;
        }
    }

}
