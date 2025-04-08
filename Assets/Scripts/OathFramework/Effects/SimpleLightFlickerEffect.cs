using OathFramework.Core;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace OathFramework.Effects
{
    public class SimpleLightFlickerEffect : LoopComponent, ILoopFixedUpdate
    {
        [FormerlySerializedAs("light")]
        [Tooltip("External light to flicker; you can leave this null if you attach script to a light")]
        [SerializeField] private Light[] mLights;
        
        [Tooltip("Minimum random light intensity")]
        [SerializeField] private float minIntensity;
        
        [Tooltip("Maximum random light intensity")]
        [SerializeField] private float maxIntensity = 1f;
        
        [Tooltip("How much to smooth out the randomness; lower values = sparks, higher = lantern")]
        [Range(1, 50)]
        [SerializeField] private int smoothing = 5;

        // Continuous average calculation via FIFO queue
        // Saves us iterating every time we update, we just change by the delta
        private Queue<float> smoothQueue;
        private float[] baseIntensities;
        private float lastSum;
        
        /// <summary>
        /// Reset the randomness and start again. You usually don't need to call
        /// this, deactivating/reactivating is usually fine but if you want a strict
        /// restart you can do.
        /// </summary>
        public void Reset() 
        {
            smoothQueue.Clear();
            lastSum = 0;
        }

        private void Start() 
        {
            smoothQueue     = new Queue<float>(smoothing);
            baseIntensities = new float[mLights.Length];
            for(int i = 0; i < mLights.Length; i++) {
                baseIntensities[i] = mLights[i].intensity;
            }
        }

        void ILoopFixedUpdate.LoopFixedUpdate() 
        {
            if(mLights == null || mLights.Length == 0)
                return;

            // pop off an item if too big
            while(smoothQueue.Count >= smoothing) {
                lastSum -= smoothQueue.Dequeue();
            }

            // Generate random new item, calculate new average
            float newVal = Random.Range(minIntensity, maxIntensity);
            smoothQueue.Enqueue(newVal);
            lastSum += newVal;

            // Calculate new smoothed average
            for(int i = 0; i < mLights.Length; i++) {
                mLights[i].intensity = baseIntensities[i] * (lastSum / (float)smoothQueue.Count);
            }
        }
    }
}
