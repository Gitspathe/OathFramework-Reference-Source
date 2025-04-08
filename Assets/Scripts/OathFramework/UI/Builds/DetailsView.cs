using OathFramework.AbilitySystem;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using OathFramework.Progression;
using OathFramework.UI.Info;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace OathFramework.UI.Builds
{
    public class DetailsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private Transform nodeParent;
        
        private List<GameObject> curNodes = new();
        
        public DetailsView Initialize()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocalizationChanged;
            return this;
        }

        private void OnLocalizationChanged(Locale newLocale)
        {
            SetupDetails(null);
        }

        public void SetupLevelDetails(in PlayerBuildData buildData)
        {
            Clear();
            LevelDetailNode node = new(buildData);
            curNodes.Add(node.Display(nodeParent));
        }

        public void SetupDetails(IDetailsViewObject info)
        {
            if(info == null) {
                SetupDetailsEquipSlot(EquipSlotType.None, null, null);
                return;
            }
            if(info is UIEquippableInfo equippable) {
                SetupDetailsEquipSlot(EquipSlotType.Equippable, equippable.Template.EquippableKey, null);
                return;
            }
            if(info is AbilityInfo ability) {
                SetupDetailsEquipSlot(EquipSlotType.Ability, ability.AbilityKey, null);
                return;
            }
            if(info is UIPerkInfo perk) {
                SetupDetailsEquipSlot(EquipSlotType.Perk, perk.PerkKey, null);
            }
        }

        public void SetupDetailsEquipSlot(EquipSlotType type, string key, string comparisonKey)
        {
            switch(type) {
                case EquipSlotType.Equippable: {
                    SetupEquippableDetails(key, comparisonKey);
                } break;
                case EquipSlotType.Ability: {
                    SetupAbilityDetails(key);
                } break;
                case EquipSlotType.Perk: {
                    SetupPerkDetails(key);
                } break;

                case EquipSlotType.None:
                default: {
                    Clear();
                } break;
            }
        }

        private void SetTitleAndDescription(IDetailsViewObject detailsObj, Entity entity)
        {
            if(detailsObj == null) {
                title.enabled       = false;
                description.enabled = false;
            } else {
                title.enabled       = true;
                description.enabled = true;
                title.text          = detailsObj.Title;
                description.text    = detailsObj.GetDescription(entity);
            }
        }
        
        private void SetupEquippableDetails(string dataKey, string comparisonKey = null)
        {
            Clear();
            Equippable equipData   = EquippableManager.GetTemplate(dataKey);
            Equippable compareData = !string.IsNullOrEmpty(comparisonKey) ? EquippableManager.GetTemplate(comparisonKey) : null;
            if(equipData == null)
                return;
            
            SetTitleAndDescription(equipData.UIInfo, PlayerManager.Instance.DummyPlayer.Entity);
            foreach(InfoNode node in equipData.UIInfo.InfoNodes) {
                if(compareData != null && node is INodeComparable<Equippable> comparable) {
                    curNodes.Add(comparable.Display(compareData, nodeParent));
                    continue;
                }
                curNodes.Add(node.Display(nodeParent));
            }
        }

        private void SetupAbilityDetails(string dataKey)
        {
            Clear();
            if(string.IsNullOrEmpty(dataKey))
                return;
            
            Ability abilityData = AbilityManager.Get(dataKey);
            SetTitleAndDescription(abilityData.Info, PlayerManager.Instance.DummyPlayer.Entity);
        }

        private void SetupPerkDetails(string dataKey)
        {
            Clear();
            if(string.IsNullOrEmpty(dataKey))
                return;
            
            Perk perkData = PerkManager.Get(dataKey);
            SetTitleAndDescription(perkData.UIInfo, PlayerManager.Instance.DummyPlayer.Entity);
        }

        public void Clear()
        {
            title.text       = "";
            description.text = "";
            foreach(GameObject go in curNodes) {
                Destroy(go);
            }
            curNodes.Clear();
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocalizationChanged;
        }
    }

    public interface IDetailsViewObject
    {
        string Title { get; }
        Sprite Icon  { get; }
        string GetDescription(Entity entity);
    }
}
