using OathFramework.UI.Platform;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace OathFramework.UI
{
    public class ModalButtonScript : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private UIInitialSelect initSelect;

        public Selectable Selectable { get; private set; }
        
        public int Index { get; private set; }

        public void OnClicked()
        {
            ModalUIScript.ModalButtonPressed(Index);
        }

        public void InitSelect()
        {
            initSelect.enabled = true;
        }

        public void SetLeftNav(Selectable select)
        {
            Navigation nav        = Selectable.navigation;
            nav.selectOnLeft      = select;
            Selectable.navigation = nav;
        }

        public void SetRightNav(Selectable select)
        {
            Navigation nav        = Selectable.navigation;
            nav.selectOnRight     = select;
            Selectable.navigation = nav;
        }

        public ModalButtonScript Setup(int index, LocalizedString text)
        {
            Selectable            = GetComponent<Selectable>();
            Navigation nav        = Selectable.navigation;
            nav.mode              = Navigation.Mode.Explicit;
            Selectable.navigation = nav;
            Index                 = index;
            this.text.text        = text.GetLocalizedString();
            return this;
        }
    }
}
