using UnityEngine;
using TMPro;

namespace OathFramework.UI
{ 

    public class DeathUIScript : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;

        public static DeathUIScript Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI timeLeftText;
        private float timeLeft;

        public DeathUIScript Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(DeathUIScript)} singletons.");
                Destroy(gameObject);
                return null;
            }

            Instance = this;
            return Instance;
        }

        private void LateUpdate()
        {
            timeLeft -= Time.deltaTime;
            if(timeLeft < 0.0f) {
                timeLeft = 0.0f;
            }

            //timeLeftText.text = Mathf.Round(timeLeft + 0.49f).ToString("0") + "s";
        }

        public void Show(float timeUntilRespawn)
        {
            timeLeft = timeUntilRespawn;
            mainPanel.SetActive(true);
        }

        public void Hide()
        {
            mainPanel.SetActive(false);
        }
    }

}
