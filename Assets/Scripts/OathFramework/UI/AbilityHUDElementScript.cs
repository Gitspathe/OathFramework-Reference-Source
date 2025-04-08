using OathFramework.AbilitySystem;
using OathFramework.Extensions;
using OathFramework.EntitySystem;
using OathFramework.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI
{
    public class AbilityHUDElementScript : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image background;
        [SerializeField] private Image backgroundCutout;
        [SerializeField] private float backgroundProgressMult = 1.0f;
        [SerializeField] private Image border;
        [SerializeField] private Image borderCutout;
        [SerializeField] private float borderProgressMult = 1.0f;
        [SerializeField] private TextMeshProUGUI chargesTxt;

        private void Awake()
        {
            icon.preserveAspect = true;
        }

        public void SetData(EntityAbility? ability, Entity entity)
        {
            if(!ability.HasValue) {
                gameObject.SetActive(false);
                return;
            }
            
            gameObject.SetActive(true);
            EntityAbility ea = ability.Value;
            Ability a        = ea.Ability;
            Sprite img       = a.Info != null ? a.Info.Icon : null;
            byte maxCharges  = a.GetMaxCharges(entity);
            bool isMax       = ea.Charges == maxCharges;
            bool showCharges = a.HasCharges && maxCharges > 1 && ea.Charges > 0;
            icon.sprite      = img;
            chargesTxt.gameObject.SetActive(showCharges);
            if(showCharges) {
                chargesTxt.SetText(StringBuilderCache.Retrieve.Concat(ea.Charges));
            }
            if(a.HasCharges) {
                float progress              = isMax ? 1.0f : ea.ChargeProgress == 0.0f ? 0.0f : ea.ChargeProgress / a.GetMaxChargeProgress(entity);
                backgroundCutout.fillAmount = progress * backgroundProgressMult;
                borderCutout.fillAmount     = progress * borderProgressMult;
            } else {
                backgroundCutout.fillAmount = 0.0f;
                borderCutout.fillAmount     = 0.0f;
            }
        }
    }
}
