using OathFramework.EquipmentSystem;
using OathFramework.UI.Info;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ParamDisplayInfo = OathFramework.UI.Info.EquipmentInfoUtil.ParamDisplayInfo;

namespace OathFramework.UI.Builds
{

    public class InfoStatBar : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Slider underBar;
        [SerializeField] private Slider overBar;
        [SerializeField] private Color barNormalColor;
        [SerializeField] private Color barAddColor;
        [SerializeField] private Color barSubtractColor;
        [SerializeField] private Color valueNormalColor;
        [SerializeField] private Color valueAddColor;
        [SerializeField] private Color valueSubtractColor;

        private Image underBarImage;
        private Image overBarImage;

        public void SetupGeneric(int value, int min, int max)
        {
            underBar.gameObject.SetActive(false);
            underBarImage      = underBar.GetComponent<Image>();
            overBarImage       = overBar.GetComponent<Image>();
            valueText.text     = value.ToString();
            overBarImage.color = barNormalColor;
            valueText.color    = valueNormalColor;
            overBar.value      = ((float)min + (float)value) / ((float)max - (float)min);
        }
        
        public void SetupEquippable(UIEquippableParams param, Equippable template, Equippable other = null)
        {
            ParamDisplayInfo displayInfoA = EquipmentInfoUtil.GetDisplayInfo(param, template);
            ParamDisplayInfo displayInfoB = other != null ? EquipmentInfoUtil.GetDisplayInfo(param, other) : default;
            titleText.text                = displayInfoA.Title;
            valueText.text                = displayInfoA.DisplayValue;
            underBarImage                 = underBar.GetComponent<Image>();
            overBarImage                  = overBar.GetComponent<Image>();
            underBar.gameObject.SetActive(other != null);
            overBar.gameObject.SetActive(true);
            if(other != null) {
                SetupEquippableInternal(displayInfoA, displayInfoB);
                return;
            }
            
            SetupEquippableInternal(displayInfoA);
        }

        private void SetupEquippableInternal(ParamDisplayInfo infoA)
        {
            overBarImage.color = barNormalColor;
            overBar.value      = infoA.NormalizedValue;
        }
        
        private void SetupEquippableInternal(ParamDisplayInfo infoA, ParamDisplayInfo infoB)
        {
            if(Mathf.Abs(infoA.NormalizedValue - infoB.NormalizedValue) < 0.0001f) {
                underBar.gameObject.SetActive(false);
                overBarImage.color = barNormalColor;
                valueText.color    = valueNormalColor;
                overBar.value      = infoA.NormalizedValue;
            } else if(infoA.NormalizedValue > infoB.NormalizedValue) {
                overBarImage.color  = barNormalColor;
                underBarImage.color = barAddColor;
                valueText.color     = valueAddColor;
                overBar.value       = infoB.NormalizedValue;
                underBar.value      = infoA.NormalizedValue;
            } else {
                overBarImage.color  = barNormalColor;
                underBarImage.color = barSubtractColor;
                valueText.color     = valueSubtractColor;
                overBar.value       = infoA.NormalizedValue;
                underBar.value      = infoB.NormalizedValue;
            }
        }
    }

}
