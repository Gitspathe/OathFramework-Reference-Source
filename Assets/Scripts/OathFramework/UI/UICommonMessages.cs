using UnityEngine;
using UnityEngine.Localization;

namespace OathFramework.UI
{
    public class UICommonMessages : MonoBehaviour
    {
        [SerializeField] private LocalizedString yes;
        [SerializeField] private LocalizedString no;
        [SerializeField] private LocalizedString ok;
        [SerializeField] private LocalizedString close;
        [SerializeField] private LocalizedString cancel;
        [SerializeField] private LocalizedString confirm;

        public static LocalizedString Yes     => Instance.yes;
        public static LocalizedString No      => Instance.no;
        public static LocalizedString OK      => Instance.ok;
        public static LocalizedString Close   => Instance.close;
        public static LocalizedString Cancel  => Instance.cancel;
        public static LocalizedString Confirm => Instance.confirm;

        public static UICommonMessages Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
        }
    }
}
