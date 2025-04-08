using OathFramework.Core;
using OathFramework.Pooling;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.Effects
{
    [RequireComponent(typeof(PoolableGameObject))]
    public class Prop : EffectBase, 
        ILoopLateUpdate, IPoolableComponent, IModelPlug
    {
        [field: SerializeField] public PropParams Params { get; private set; }

        [SerializeField] private Transform model;
        [SerializeField] private float dissipateDuration = 0.5f;
        
        private QList<EffectNode> nodes;
        private Vector3 defaultPositionOffset;
        private Quaternion defaultRotationOffset;
        
        public ModelPlugType PlugType        => ModelPlugType.Prop;
        public bool IsDissipating            { get; private set; }
        public float Dissipation             { get; private set; }
        public ModelSocketHandler Sockets    { get; private set; }
        public byte CurrentSpot              { get; private set; }
        public ushort ID                     { get; set; }
        public PoolableGameObject PoolableGO { get; set; }

        private void Awake()
        {
            EffectNode[] arr      = GetComponentsInChildren<EffectNode>(true);
            nodes                 = new QList<EffectNode>(arr.Length);
            defaultPositionOffset = model?.localPosition ?? Vector3.zero;
            nodes.AddRange(arr);
        }

        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(IsDissipating) {
                HandleDissipation();
            }
        }

        public void PassTime(float time)
        {
            Dissipation += time;
        }
        
        public void AssignCopyData(in CopyableModelPlug.CopyData copyData)
        {
            IsDissipating = copyData.CurDissipation > 0.01f;
            Dissipation   = copyData.CurDissipation;
            GetComponent<ColorableEffect>()?.SetColor(copyData.Color);
        }
        
        private void HandleDissipation()
        {
            Dissipation += Time.deltaTime;
            if(Dissipation >= dissipateDuration) {
                Sockets     = null;
                CurrentSpot = 0;
                PoolableGO.SetParams(false, false);
                for(int i = 0; i < nodes.Count; i++) {
                    nodes.Array[i].Hide();
                }
                PropManager.Return(this, true);
            }
        }
        
        void IPoolableComponent.OnRetrieve()
        {
            Sockets = null;
            for(int i = 0; i < nodes.Count; i++) {
                nodes.Array[i].Show();
            }
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            IsDissipating = false;
            Dissipation   = 0.0f;
        }

        void IModelPlug.OnAdd(byte spot, ModelSocketHandler sockets)
        {
            Sockets     = sockets;
            CurrentSpot = spot;
            PoolableGO.SetParams(true, true);
            if(!ReferenceEquals(model, null) && Params.TryGetOffset(spot, out Vector3 offset, out Quaternion rotOffset)) {
                model.transform.localPosition = offset;
                model.transform.localRotation = rotOffset;
            } else if(!ReferenceEquals(model, null)) {
                model.transform.localPosition = defaultPositionOffset;
                model.transform.localRotation = defaultRotationOffset;
            }
        }
        
        void IModelPlug.OnRemove(ModelSocketHandler sockets, ModelPlugRemoveBehavior removeBehavior)
        {
            switch(removeBehavior) {
                case ModelPlugRemoveBehavior.Dissipate: {
                    IsDissipating = true;
                    for(int i = 0; i < nodes.Count; i++) {
                        nodes.Array[i].Dissipate(dissipateDuration);
                    }
                } break;
                case ModelPlugRemoveBehavior.Instant: {
                    Sockets     = null;
                    CurrentSpot = 0;
                    PoolableGO.SetParams(false, false);
                    for(int i = 0; i < nodes.Count; i++) {
                        nodes.Array[i].Hide();
                    }
                    PropManager.Return(this, true);
                } break;

                case ModelPlugRemoveBehavior.None:
                    break;
            }
        }
    }
}
