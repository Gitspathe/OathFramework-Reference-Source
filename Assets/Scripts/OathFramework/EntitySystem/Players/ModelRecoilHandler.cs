using System;
using UnityEngine;

namespace OathFramework.EntitySystem.Players
{
    public class ModelRecoilHandler : MonoBehaviour
    {
        [field: SerializeField] public RecoilNode[] RecoilNodes    { get; private set; }
        [field: SerializeField] public AnimationCurve RecoilReduce { get; private set; }
        
        private float curRecoil;

        public void AddRecoil(float amt)
        {
            curRecoil += amt;
        }
        
        private void Update()
        {
            ResetRecoil();
        }

        private void LateUpdate()
        {
            UpdateRecoil();
        }

        private void UpdateRecoil()
        {
            curRecoil = Mathf.Clamp(curRecoil - (RecoilReduce.Evaluate(curRecoil) * Time.deltaTime), 0.0f, 1.0f);
            if(curRecoil < 0.001f)
                return;
            
            foreach(RecoilNode recoilNode in RecoilNodes) {
                recoilNode.Apply(curRecoil);
            }
        }

        private void ResetRecoil()
        {
            foreach(RecoilNode recoilNode in RecoilNodes) {
                recoilNode.Reset();
            }
        }
        
        [Serializable]
        public class RecoilNode
        {
            [field: SerializeField] public Transform Transform  { get; private set; }
            [field: SerializeField] public Vector3 Angle        { get; private set; }
            [field: SerializeField] public AnimationCurve Curve { get; private set; }

            private Vector3 oldEuler;
            
            public void Apply(float amount)
            {
                Vector3 goal = Angle * Curve.Evaluate(Mathf.Clamp(amount, 0.0f, 1.0f));
                oldEuler     = Vector3.Lerp(oldEuler, goal, 35.0f * Time.deltaTime);
                Transform.localEulerAngles += oldEuler;
            }

            public void Reset()
            {
                if(oldEuler.magnitude < 0.0001f)
                    return;
                
                //Transform.localEulerAngles -= oldEuler;
            }
        }
    }
}
