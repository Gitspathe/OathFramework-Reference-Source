using OathFramework.Core;
using UnityEngine;

namespace OathFramework.Utility
{ 

    public class FaceCamera : LoopComponent, ILoopLateUpdate
    {
        public override int UpdateOrder => GameUpdateOrder.Default;
        
        public Transform CTransform { get; private set; }

        private void Awake()
        {
            CTransform = transform;
        }

        public void LoopLateUpdate()
        {
            Camera cam = Camera.main;
            if(cam == null)
                return;

            Transform camTrans = cam.transform;
            Quaternion cRot    = camTrans.rotation;
            CTransform.LookAt(CTransform.position + cRot * Vector3.forward, cRot * Vector3.up);
        }
    }

}
