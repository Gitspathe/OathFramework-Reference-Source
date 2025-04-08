using OathFramework.Extensions;
using OathFramework.Core;
using OathFramework.Pooling;
using OathFramework.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace OathFramework.UI
{
    
    public class DamagePopup : LoopComponent, IPoolableComponent, ILoopUpdate
    {
        [SerializeField] private TextMeshPro text;
        [SerializeField] private float time;
        [SerializeField] private float speed;
        [SerializeField] private AnimationCurve opacity;

        private float curTime;
        private int framesUntilUpdate;
        
        public PoolableGameObject PoolableGO { get; set; }
        public override int UpdateOrder => GameUpdateOrder.Default;

        private static int curFrameDelay;
        private const int FrameDelay = 5;
        
        public void LoopUpdate()
        {
            curTime += Time.unscaledDeltaTime;
            if(curTime >= time && !ReferenceEquals(PoolableGO, null)) {
                PoolManager.Return(PoolableGO);
                return;
            }

            gameObject.transform.position += transform.up * (speed * Time.unscaledDeltaTime);
            framesUntilUpdate--;
            if(framesUntilUpdate > 0)
                return;

            float nextOpacity = opacity.Evaluate(curTime / time);
            if(Math.Abs(nextOpacity - text.color.a) > 0.001f) {
                text.color = new Color(text.color.r, text.color.g, text.color.b, nextOpacity);
            }
            framesUntilUpdate = FrameDelay;
        }

        public void Setup(int value)
        {
            if(curFrameDelay++ >= FrameDelay) {
                curFrameDelay = 0;
            }
            framesUntilUpdate = curFrameDelay;
            
            text.SetText(StringBuilderCache.Retrieve.Concat(value));
            curTime = 0.0f;
        }

        public void OnRetrieve()
        {
            
        }

        public void OnReturn(bool initialization)
        {
            text.SetText(StringBuilderCache.Retrieve.Append("ERROR"));
        }
    }

}
