using OathFramework.Extensions;
using OathFramework.Core;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

namespace OathFramework.Utility
{

    public class FramerateCounter : LoopComponent, ILoopUpdate
    {
        [field: SerializeField] public bool ShowOnStart               { get; private set; }
        [field: SerializeField] public GameObject MainPanel           { get; private set; }
        [field: SerializeField] public TextMeshProUGUI AverageFPSText { get; private set; }
        [field: SerializeField] public int AverageFrames              { get; private set; } = 60;
        [field: SerializeField] public float UpdateInterval           { get; private set; } = 0.25f;

        private List<double> frameTimes = new();
        private float curTimer;
        private bool showFPS;
        
        public static FramerateCounter Instance { get; private set; }

        public override int UpdateOrder => GameUpdateOrder.Default;

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(FramerateCounter)} singleton.");
                Destroy(gameObject);
                return;
            }

            Hide();
            if(ShowOnStart) {
                Show();
            }
#if DEBUG
            Show();
#endif
            
            Instance = this;
        }

        public void Toggle()
        {
            if(showFPS) {
                Hide();
            } else {
                Show();
            }
        }

        public void Show()
        {
            MainPanel.SetActive(true);
            frameTimes.Clear();
            showFPS = true;
        }

        public void Hide()
        {
            MainPanel.SetActive(false);
            frameTimes.Clear();
            showFPS = false;
        }

        public void LoopUpdate()
        {
            if(!showFPS)
                return;
            
            double time = Time.unscaledDeltaTime;
            if(frameTimes.Count >= AverageFrames) {
                frameTimes.RemoveAt(frameTimes.Count - 1);
            }
            frameTimes.Insert(0, time);

            double acc = 0;
            foreach(double t in frameTimes) {
                acc += t;
            }
            curTimer += Time.unscaledDeltaTime;
            if(curTimer < UpdateInterval)
                return;

            double avg = acc / frameTimes.Count;
            AverageFPSText.SetText(StringBuilderCache.Retrieve.Append("AVG FPS: ").Concat((int)(1.0 / avg)));
            curTimer = 0.0f;
        }
    }

}
