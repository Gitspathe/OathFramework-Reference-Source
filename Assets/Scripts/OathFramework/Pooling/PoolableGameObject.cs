using OathFramework.Core;
using OathFramework.Utility;
using System;
using UnityEngine;

namespace OathFramework.Pooling
{ 
    public class PoolableGameObject : LoopComponent, ILoopLateUpdate
    {
        private Vector3? offsetPosition;
        private Quaternion? offsetRotation;

        private Vector3 startingPosition;
        private Quaternion startingRotation;

        private bool moveWithParent;
        private bool rotateWithParent;

        private bool isAttached;
        private QList<IPoolableComponent> components = new();
        private PoolManager.GameObjectPool pool;
        private Func<bool> shouldRegister;

        public override Func<bool> ShouldRegisterDelegate => shouldRegister;

        public bool IsDestroyed     { get; private set; }
        public Transform CTransform { get; private set; }
        public Transform Attached   { get; set; }

        public Vector3? OffsetPosition
        {
            get => offsetPosition;
            set => offsetPosition = value;
        }

        public Quaternion? OffsetRotation
        {
            get => offsetRotation;
            set => offsetRotation = value;
        }

        public bool IsInitialized { get; private set; }
        public bool IsInPool      { get; private set; }

        public void Initialize(PoolManager.GameObjectPool pool)
        {
            if(IsInitialized)
                return;

            DontDestroyOnLoad(gameObject);
            this.pool        = pool;
            CTransform       = transform;
            startingPosition = CTransform.position;
            startingRotation = CTransform.rotation;
            shouldRegister   = () => isAttached;
            foreach(IPoolableComponent component in GetComponentsInChildren<IPoolableComponent>(true)) {
                component.PoolableGO = this;
                components.Add(component);
            }
            IsInitialized = true;
        }

        public void SetParams(bool moveWithParent, bool rotateWithParent)
        {
            this.moveWithParent   = moveWithParent;
            this.rotateWithParent = rotateWithParent;
        }

        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(Attached == null)
                return;
            
            if(moveWithParent) {
                CTransform.position = offsetPosition.HasValue
                    ? Attached.position + offsetPosition.Value
                    : Attached.position + startingPosition;
            }
            if(rotateWithParent) {
                CTransform.localRotation = offsetRotation.HasValue 
                    ? Attached.rotation * startingRotation * offsetRotation.Value 
                    : Attached.rotation * startingRotation;
            }
        }

        public void Return()
        {
            pool.Return(this, false);
        }

        public void OnRetrieve()
        {
            enabled    = true;
            IsInPool   = false;
            isAttached = !ReferenceEquals(Attached, null);
            int count  = components.Count;
            gameObject.SetActive(true);
            for(int i = 0; i < count; i++) {
                components.Array[i].OnRetrieve();
            }
        }

        public void OnReturn(bool initialization)
        {
            IsInPool  = true;
            int count = components.Count;
            for(int i = 0; i < count; i++) {
                components.Array[i].OnReturn(initialization);
            }
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            IsDestroyed = true;
        }
    }

}
