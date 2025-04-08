using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace OathFramework.UI.Platform
{
    public class ControlInfoNodeText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        public GameObject Setup(string text)
        {
            label.autoSizeTextContainer = true;
            label.text = text;
            return gameObject;
        }
    }
}
