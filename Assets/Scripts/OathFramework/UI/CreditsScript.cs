using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.UI.Platform;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI
{

    public class CreditsScript : LoopComponent, ILoopUpdate
    {
        [SerializeField] private Transform contentTransform;
        [SerializeField] private float maxY;
        [SerializeField] private float speed;
        private float startContentRectY;
        private bool isRunning;
        
        public static CreditsScript Instance { get; private set; }

        public CreditsScript Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(CreditsScript)} singleton.");
                return null;
            }

            Instance          = this;
            startContentRectY = contentTransform.transform.localPosition.y;
            return this;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            MenuUI.Instance.MainPanel.SetActive(false);
            contentTransform.localPosition = new Vector3(contentTransform.localPosition.x, startContentRectY, 0.0f);
            isRunning = true;
            _ = Run();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            MenuUI.Instance.MainPanel.SetActive(true);
            isRunning = false;
        }

        private async UniTask Run()
        {
            while(isRunning) {
                await RunTask();
            }
        }

        private async UniTask RunTask()
        {
            while(isRunning && contentTransform.localPosition.y < maxY) {
                Vector3 pos = contentTransform.localPosition;
                pos = new Vector3(pos.x, pos.y + (speed * Time.unscaledDeltaTime), 0.0f);
                contentTransform.localPosition = pos;
                await UniTask.Yield();
            }
            Hide();
        }

        void ILoopUpdate.LoopUpdate()
        {
            if(UIControlsInputHandler.BackAction.WasPressedThisFrame()) {
                Hide();
            }
        }
    }

}
