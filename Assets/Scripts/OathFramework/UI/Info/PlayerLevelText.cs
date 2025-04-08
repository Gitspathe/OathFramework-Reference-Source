using OathFramework.Extensions;
using OathFramework.Progression;
using OathFramework.Utility;
using TMPro;
using UnityEngine;

namespace OathFramework.UI.Info
{
    public class PlayerLevelText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        private bool isMouseOver;
        private PlayerBuildData data;
        
        public void SetData(in PlayerBuildData buildData)
        {
            data      = buildData;
            text.text = StringBuilderCache.Retrieve.Clear().Append("Lv. ").Concat(buildData.Level).ToString();
        }

        private void OnDisable()
        {
            if(isMouseOver) {
                OnPointerExit();
            }
        }

        public void OnPointerEnter()
        {
            InfoPopup.Instance.Show(data);
            isMouseOver = true;
        }

        public void OnPointerExit()
        {
            InfoPopup.Instance.Hide();
            isMouseOver = false;
        }
    }
}
