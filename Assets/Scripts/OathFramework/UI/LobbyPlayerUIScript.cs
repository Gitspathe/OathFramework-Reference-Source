using UnityEngine;
using TMPro;

namespace OathFramework.UI
{ 

    public class LobbyPlayerUIScript : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        public void Initialize(string playerName)
        {
            text.text = playerName;
        }
    }

}
