using TMPro;
using UnityEngine;

namespace OathFramework.UI
{
    public class ChatMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        public void Setup(string message)
        {
            text.text = message;
            text.ForceMeshUpdate(true);
            GetComponent<RectTransform>().sizeDelta = new Vector2(0.0f, text.preferredHeight);
        }
    }
}
