using OathFramework.AbilitySystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OathFramework.Attributes;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.EquipmentSystem;
using OathFramework.UI.Builds;
using TMPro;

namespace OathFramework.UI
{ 

    public class HUDScript : LoopComponent, ILoopLateUpdate
    {
        public override int UpdateOrder => GameUpdateOrder.Finalize;

        private bool playerIsNull;
        
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform equipmentHUDPanel;
        [SerializeField] private Transform abilityHUDPanel;
        [ArrayElementTitle, SerializeField] private HUDEquipmentPair[] equipmentPairs;
        [SerializeField] private AbilityHUDElementScript[] abilityHUDElements;

        [Space(10)]

        [SerializeField] private GameObject playerPanel;
        [SerializeField] private RectTransform healthBarTransform;
        [SerializeField] private RectTransform staminaBarTransform;
        [SerializeField] private int healthBarBaseHealth   = 500;
        [SerializeField] private float healthBarBaseWidth  = 300;
        [SerializeField] private int staminaBarBaseStamina = 100;
        [SerializeField] private float staminaBarBaseWidth = 175;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider staminaBar;

        [SerializeField] private int poolCount;
        [SerializeField] private Transform notificationMessagesTransform;
        [SerializeField] private GameObject notificationMessagePrefab;

        [SerializeField] private TextMeshProUGUI healsText;

        private List<NotificationMessageUIScript> notificationMsgPool                   = new();
        private Dictionary<EquipmentSlot, EquipmentHUDElementScript> equipmentPairsDict = new();

        public static ExpPopupUIScript ExpPopup       { get; private set; }
        public static PlayerController AttachedPlayer { get; set; }
        
        public static HUDScript Instance { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(HUDScript)} singletons.");
                Destroy(gameObject);
                return;
            }

            panel.SetActive(true);
            foreach(HUDEquipmentPair pair in equipmentPairs) {
                if(equipmentPairsDict.ContainsKey(pair.slot))
                    continue;

                equipmentPairsDict.Add(pair.slot, pair.element);
                pair.element.Initialize(pair.slot);
            }
            for(int i = 0; i < poolCount; i++) {
                GameObject goPlayerNotification = Instantiate(notificationMessagePrefab, notificationMessagesTransform);
                notificationMsgPool.Add(goPlayerNotification.GetComponent<NotificationMessageUIScript>());
                goPlayerNotification.SetActive(false);
            }
            ExpPopup = GetComponent<ExpPopupUIScript>();
            Instance = this;
        }

        public void LoopLateUpdate()
        {
            if(!panel.gameObject.activeSelf)
                return;

            playerIsNull = AttachedPlayer == null;
            UpdateAbilitiesHUD();
            UpdateQuickHealsHUD();
            UpdateStatusHUD();
            UpdateEquipmentHUD();
        }

        private void UpdateAbilitiesHUD()
        {
            if(playerIsNull) {
                abilityHUDPanel.gameObject.SetActive(false);
                return;
            }

            PlayerAbilityHandler handler = AttachedPlayer.Abilities;
            abilityHUDPanel.gameObject.SetActive(true);
            for(int i = 0; i < abilityHUDElements.Length; i++) {
                bool b = handler.TryGetAbilityAtIndex(i, out EntityAbility eb);
                abilityHUDElements[i].SetData(!b ? null : eb, AttachedPlayer.Entity);
            }
        }

        private void UpdateQuickHealsHUD()
        {
            if(playerIsNull) {
                healsText.gameObject.SetActive(false);
                return;
            }
            
            healsText.gameObject.SetActive(true);
            QuickHealHandler handler = AttachedPlayer.QuickHeal;
            byte curHeals            = handler.CurrentCharges;
            healsText.text           = curHeals == 0 ? "" : curHeals.ToString();
        }

        private void UpdateStatusHUD()
        {
            if(playerIsNull) {
                playerPanel.SetActive(false);
                return;
            }

            playerPanel.SetActive(true);
            Stats playerStats             = AttachedPlayer.Entity.CurStats;
            float hpWidth                 = healthBarBaseWidth * ((float)playerStats.maxHealth / healthBarBaseHealth);
            float staminaWidth            = staminaBarBaseWidth * ((float)playerStats.maxStamina / staminaBarBaseStamina);
            Rect hpRect                   = healthBarTransform.rect;
            Rect staminaRect              = staminaBarTransform.rect;
            healthBarTransform.sizeDelta  = new Vector2(hpWidth, hpRect.height);
            staminaBarTransform.sizeDelta = new Vector2(staminaWidth, staminaRect.height);
            healthBar.value               = (float)playerStats.health / playerStats.maxHealth;
            staminaBar.value              = (float)playerStats.stamina / playerStats.maxStamina;
        }

        private void UpdateEquipmentHUD()
        {
            if(playerIsNull) {
                equipmentHUDPanel.gameObject.SetActive(false);
                return;
            }

            equipmentHUDPanel.gameObject.SetActive(true);
            EntityEquipment equip = AttachedPlayer.Equipment;

            // Inventory is not initialized or invalid - hide equipment slot.
            if(equip.InventorySlotArray == null || equip.InventorySlotArray.Length == 0) {
                foreach(EquipmentHUDElementScript element in equipmentPairsDict.Values) {
                    element.SetValues(null);
                    element.IsSelected = false;
                }
                return;
            }

            // Loop through inventory slots and update equipment as needed.
            foreach(InventorySlot slot in equip.InventorySlotArray) {
                if(!TryGetEquipmentElement(slot.SlotID, out EquipmentHUDElementScript element))
                    continue;

                element.SetValues(slot);
                element.IsSelected = equip.CurrentSlot == slot;
            }
        }

        private bool TryGetEquipmentElement(EquipmentSlot slot, out EquipmentHUDElementScript element)
        {
            return equipmentPairsDict.TryGetValue(slot, out element);
        }

        public void ShowNotification(string message, float time = 4.0f)
        {
            if(Game.State != GameState.InGame)
                return;

            if(notificationMsgPool.Count == 0) {
                GameObject go = Instantiate(notificationMessagePrefab, notificationMessagesTransform);
                go.GetComponent<NotificationMessageUIScript>().Initialize(message, time);
                return;
            }
            NotificationMessageUIScript msgScript = notificationMsgPool[notificationMsgPool.Count - 1];
            notificationMsgPool.RemoveAt(notificationMsgPool.Count - 1);
            msgScript.Initialize(message, time);
        }

        public void ReturnNotificationObject(NotificationMessageUIScript msg)
        {
            notificationMsgPool.Add(msg);
        }
    }

    [Serializable]
    public class HUDEquipmentPair : IArrayElementTitle
    {
        public EquipmentSlot slot;
        public EquipmentHUDElementScript element;

        string IArrayElementTitle.Name => slot.ToString();
    }

}
