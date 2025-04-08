using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI.Info
{
    public class PlayerInfoItem : MonoBehaviour
    {
        [SerializeField] private Image icon;
        private bool isMouseOver;
        
        public UIInfo Info { get; private set; }

        public void SetInfo(UIInfo info)
        {
            if(info == null) {
                Info              = null;
                this.icon.sprite  = null;
                this.icon.enabled = false;
                return;
            }
            Info              = info;
            Sprite icon       = info.Icon;
            this.icon.sprite  = icon;
            this.icon.enabled = icon;
        }
        
        private void OnDisable()
        {
            if(isMouseOver) {
                OnPointerExit();
            }
        }

        public void OnSelect()
        {
            InfoPopup.Instance.Show(Info, GetComponent<RectTransform>());
            isMouseOver = true;
        }

        public void OnDeselect()
        {
            InfoPopup.Instance.Hide();
            isMouseOver = false;
        }

        public void OnPointerEnter()
        {
            InfoPopup.Instance.Show(Info, GetComponent<RectTransform>());
            isMouseOver = true;
        }

        public void OnPointerExit()
        {
            InfoPopup.Instance.Hide();
            isMouseOver = false;
        }
    }
}
