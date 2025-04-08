using System.Collections.Generic;
using UnityEngine;
using OathFramework.Networking;
using OathFramework.Progression;
using UnityEngine.UI;
using TMPro;
namespace OathFramework.UI.Builds
{ 

    public class CharacterMenuScript : MonoBehaviour
    {
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private GameObject applyButton;
        [SerializeField] private GameObject revertButton;
        [SerializeField] private GameObject avaliablePanel;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI avaliableText;
        [SerializeField] private Slider expBar;
        [SerializeField] private List<AttributeNode> attributeUIScripts;

        public bool IsVisible => characterPanel.activeSelf;
        public byte AvailablePoints { get; private set; }

        public CharacterMenuScript Initialize()
        {
            foreach(AttributeNode attribute in attributeUIScripts) {
                attribute.Initialize(this);
            }
            applyButton.SetActive(false);
            revertButton.SetActive(false);
            return this;
        }

        public void Tick()
        {
            levelText.text     = BuildMenuScript.Profile.level.ToString();
            AvailablePoints    = (byte)(BuildMenuScript.Profile.level - BuildMenuScript.CurBuildData.Level);
            avaliableText.text = AvailablePoints.ToString();
            expBar.value       = BuildMenuScript.Profile.ExpProgress;
            avaliablePanel.SetActive(AvailablePoints > 0);
            expBar.gameObject.SetActive(BuildMenuScript.Profile.level != PlayerBuildData.MaxLevel);
            foreach(AttributeNode attribute in attributeUIScripts) {
                attribute.Tick();
            }

            PlayerBuildData original = BuildMenuScript.Profile.CurrentLoadout;
            PlayerBuildData newData  = BuildMenuScript.CurBuildData;
            bool changed = original.AttributesChanged(newData);
            applyButton.SetActive(changed);
            revertButton.SetActive(changed);
        }

        public void AddPressed(AttributeNode attribute)
        {
            BuildMenuScript.CurBuildData.IncrementAttribute(attribute.AttributeType);
            BuildMenuScript.TickStats();
            Tick();
        }

        public void ReducePressed(AttributeNode attribute)
        {
            BuildMenuScript.CurBuildData.DecrementAttribute(attribute.AttributeType);
            BuildMenuScript.TickStats();
            Tick();
        }

        public void Show()
        {
            characterPanel.SetActive(true);
        }

        public void Hide()
        {
            characterPanel.SetActive(false);
        }
    }

}
