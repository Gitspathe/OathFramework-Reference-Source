using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OathFramework.UI
{ 

    public class NotificationMessageUIScript : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image panel;
        [SerializeField] private AnimationCurve textOpacity;
        [SerializeField] private AnimationCurve panelOpacity;

        private float startTime;
        private float time;

        public void Initialize(string message, float time)
        {
            text.text = message;
            startTime = time;
            this.time = 0.0f;
            gameObject.SetActive(true);
        }

        public void LateUpdate()
        {
            time += Time.unscaledDeltaTime;
            text.color  = new Color(text.color.r, text.color.g, text.color.b, textOpacity.Evaluate(time / startTime));
            panel.color = new Color(panel.color.r, panel.color.g, panel.color.b, panelOpacity.Evaluate(time / startTime));
            if(time > startTime) {
                gameObject.SetActive(false);
                HUDScript.Instance.ReturnNotificationObject(this);
            }
        }
    }

}
