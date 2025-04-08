using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Progression;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI.Builds
{

    public class ExpPopupUIScript : LoopComponent, ILoopLateUpdate
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        [SerializeField] private float holdTime = 1.5f;
        [SerializeField] private float fadeTime = 0.667f;
        [SerializeField] private AnimationCurve accSpeed;
        [Space(10)]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider expSlider;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI remainingText;

        private bool active;
        private byte curLevel;
        private float curExp;
        private float expSinceLast;
        private float curAccSpeed;
        private float expToNext;

        public void Setup(byte startLevel, uint startExp)
        {
            expSinceLast = 0;
            curLevel     = startLevel;
            curExp       = startExp;
            expToNext    = ProgressionManager.GetExpNeeded(curLevel);
            Hide(true);
            Tick();
        }

        public void Tick()
        {
            expSlider.value    = curExp / expToNext;
            levelText.text     = $"LEVEL   {curLevel}";
            remainingText.text = $"REMAINING:   {(int)(expToNext - curExp)}";
        }

        public void RecordExp(uint amount)
        {
            expSinceLast += amount;
        }

        public void Show()
        {
            active            = true;
            canvasGroup.alpha = 0.0f;
            mainPanel.SetActive(true);
            _ = FadeInTask();
        }

        public void Hide(bool instant = false)
        {
            active = false;
            if(instant) {
                canvasGroup.alpha = 0.0f;
                mainPanel.SetActive(false);
                return;
            }
            
            active = false;
            _ = FadeOutTask();
        }

        private async UniTask FadeInTask()
        {
            while(canvasGroup.alpha < 1.0f) {
                canvasGroup.alpha += fadeTime * Time.unscaledDeltaTime;
                await UniTask.Yield();
            }
        }

        private async UniTask FadeOutTask()
        {
            await UniTask.WaitForSeconds(holdTime, true);
            while(canvasGroup.alpha > 0.0f) {
                canvasGroup.alpha -= fadeTime * Time.unscaledDeltaTime;
                await UniTask.Yield();
            }
            mainPanel.SetActive(false);
            active = false;
        }

        private async UniTask LevelUpAnim()
        {
            // TODO: level anim.
        }

        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(!active)
                return;

            curAccSpeed = accSpeed.Evaluate(expSinceLast);
            float add   = Mathf.Clamp(curAccSpeed * Time.unscaledDeltaTime, 0.0f, expSinceLast);
            expSinceLast -= add;
            curExp += add;
            if(curExp >= expToNext) {
                curExp -= expToNext;
                curLevel += 1;
                expToNext = ProgressionManager.GetExpNeeded(curLevel);
                _ = LevelUpAnim();
            }
            if(expSinceLast <= 0.0f && canvasGroup.alpha >= 1.0f) {
                Hide();
            }
            Tick();
        }
    }

}
