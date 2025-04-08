using UnityEngine;

namespace OathFramework.Effects
{ 

    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float decreaseFactor = 1.0f;
        [SerializeField] private AnimationCurve falloff;

        private float curDuration;
        private float startDuration;
        private float shakeAmount;
        private float startShakeAmount;

        private void Update()
        {
            if(curDuration > 0.0f) {
                shakeAmount             = startShakeAmount * (1.0f - falloff.Evaluate(curDuration / startDuration));
                transform.localPosition = Random.insideUnitSphere * shakeAmount;
                curDuration            -= Time.deltaTime * decreaseFactor;
            } else {
                transform.localPosition = Vector3.zero;
                curDuration             = 0.0f;
                shakeAmount             = 0.0f;
                startShakeAmount        = 0.0f;
            }
        }

        public void Shake(float duration, float magnitude)
        {
            magnitude *= 0.1f;
            if(duration > curDuration) {
                curDuration   = duration;
                startDuration = duration;
            }
            if(magnitude > shakeAmount) {
                shakeAmount      = magnitude;
                startShakeAmount = magnitude;
            }
        }
    }

}
