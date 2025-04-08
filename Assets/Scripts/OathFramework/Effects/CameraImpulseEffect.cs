using Sirenix.OdinInspector;
using UnityEngine;

namespace OathFramework.Effects
{
    public class CameraImpulseEffect : MonoBehaviour
    {
        [SerializeField] private float radius    = 5.0f;
        [SerializeField] private float duration  = 1.0f;
        [SerializeField] private float magnitude = 0.5f;

        [SerializeField] private bool useFalloff;
        [SerializeField, ShowIf("@useFalloff")] private AnimationCurve falloff;

        private void OnEnable()
        {
            Camera main = Camera.main;
            if(main == null)
                return;
            
            CameraShake shake = main.GetComponent<CameraShake>();
            Vector3 camVec    = new(main.transform.position.x, 0.0f, main.transform.position.z);
            Vector3 thisVec   = new(transform.position.x, 0.0f, transform.position.z);
            float dist        = Mathf.Clamp(Vector3.Distance(camVec, thisVec), 0.001f, ushort.MaxValue);
            if(dist > radius)
                return;

            if(useFalloff) {
                magnitude *= falloff?.Evaluate(dist / radius) ?? 1.0f;
            }
            shake.Shake(duration, magnitude);
        }
    }
}
