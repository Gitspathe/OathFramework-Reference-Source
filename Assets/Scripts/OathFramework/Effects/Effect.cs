using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Persistence;
using OathFramework.Pooling;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    [RequireComponent(typeof(PoolableGameObject))]
    public partial class Effect : EffectBase, 
        IModelPlug, ILoopLateUpdate, IPoolableComponent,
        IPersistableComponent
    {
        [field: SerializeField] public EffectParams Params { get; private set; }

        [Space(5)] 
        
        [SerializeField] private bool persistent;
        
        [HideIf(nameof(persistent))]
        [SerializeField] private float duration          = 1.0f;
        [SerializeField] private float dissipateDuration = 0.5f;

        [Space(5)]
        [SerializeField] private bool moveWithParent   = true;
        [SerializeField] private bool rotateWithParent = true;

        private QList<EffectNode> nodes;
        private QList<IEffectSpawned> spawnCallbacks = new();
        private Dictionary<string, ICopyableModelPlugComponent> copyableComponents;

        public ModelPlugType PlugType          => ModelPlugType.Effect;
        public bool LocalRotation              => rotateWithParent;
        public bool HasDelayedTransform        { get; private set; }
        public bool IsDissipating              { get; private set; }
        public DelayedTransform DelayTransform { get; private set; }
        public ModelSocketHandler Sockets      { get; set; }
        public NetEffect NetEffect             { get; private set; }
        public IEntity Source                  { get; set; }
        public byte CurrentSpot                { get; set; }
        public ushort ID                       { get; set; }
        public ushort ExtraData                { get; set; }
        public bool Local                      { get; set; }
        public bool NetworkEnabled             { get; set; }
        public float CurDuration               { get; private set; }
        public float Dissipation               { get; private set; }
        public bool IsNetworked                => NetworkEnabled && !ReferenceEquals(NetEffect, null);
        public PoolableGameObject PoolableGO   { get; set; }
        
        public override int UpdateOrder => GameUpdateOrder.Effects;

        private void Awake()
        {
            NetEffect           = GetComponent<NetEffect>();
            DelayTransform      = GetComponent<DelayedTransform>();
            HasDelayedTransform = DelayTransform != null;
            EffectNode[] arr    = GetComponentsInChildren<EffectNode>(true);
            nodes               = new QList<EffectNode>(arr.Length);
            nodes.AddRange(arr);
            for(int i = 0; i < arr.Length; i++) {
                nodes.Array[i].Initialize(this);
            }
            if(GetComponent<CopyableModelPlug>() != null) {
                copyableComponents = new Dictionary<string, ICopyableModelPlugComponent>();
                foreach(ICopyableModelPlugComponent c in GetComponentsInChildren<ICopyableModelPlugComponent>(true)) {
                    copyableComponents.Add(c.ID, c);
                }
            }
            spawnCallbacks.AddRange(GetComponentsInChildren<IEffectSpawned>(true));
        }

        public void OnSpawned()
        {
            for(int i = 0; i < spawnCallbacks.Count; i++) {
                spawnCallbacks.Array[i].OnEffectSpawned();
            }
        }

        public void Return(bool instant = false)
        {
            if(IsDissipating && !instant)
                return;
            
            int count = nodes.Count;
            if(instant) {
                for(int i = 0; i < count; i++) {
                    nodes.Array[i].Hide();
                }
                EffectManager.ReturnImmediate(this);
                return;
            }
            if(dissipateDuration <= 0.0f) {
                for(int i = 0; i < count; i++) {
                    nodes.Array[i].Hide();
                }
            } else {
                for(int i = 0; i < count; i++) {
                    nodes.Array[i].Dissipate(dissipateDuration);
                }
            }
            IsDissipating = true;
            if(dissipateDuration <= 0.0f) {
                EffectManager.ReturnImmediate(this);
            }
        }

        public void SetParams(Data data)
        {
            CurDuration   = data.Duration;
            IsDissipating = data.IsDissipating;
            Dissipation   = data.Dissipation;
            ExtraData     = data.ExtraData;
        }
        
        public void PassTime(float t)
        {
            if(IsDissipating) {
                Dissipation += t;
                return;
            }
            CurDuration += t;
        }

        public void AssignCopyData(in CopyableModelPlug.CopyData copyData)
        {
            CurDuration   = copyData.CurDuration;
            IsDissipating = copyData.CurDissipation > 0.01f;
            Dissipation   = copyData.CurDissipation;
            foreach(KeyValuePair<string, ICopyableModelPlugComponentData> pair in copyData.ComponentData) {
                if(!copyableComponents.TryGetValue(pair.Key, out ICopyableModelPlugComponent c))
                    continue;
                
                c.ApplyData(pair.Value);
            }
            GetComponent<ColorableEffect>()?.SetColor(copyData.Color);
        }

        private void HandleDissipation()
        {
            Dissipation += Time.deltaTime;
            if(Dissipation >= dissipateDuration) {
                EffectManager.ReturnImmediate(this);
            }
        }
        
        void ILoopLateUpdate.LoopLateUpdate()
        {
            if(IsDissipating) {
                HandleDissipation();
                return;
            }
            if(persistent)
                return;
            
            CurDuration += Time.deltaTime;
            if(CurDuration >= duration && !IsDissipating) {
                Return();
            }
        }
        
        void IPoolableComponent.OnRetrieve()
        {
            PoolableGO.SetParams(moveWithParent, rotateWithParent);
            ID        = Params.ID;
            int count = nodes.Count;
            for(int i = 0; i < count; i++) {
                nodes.Array[i].Show();
            }
        }
        
        void IPoolableComponent.OnReturn(bool initialization)
        {
            Sockets       = null;
            Source        = null;
            CurDuration   = 0.0f;
            IsDissipating = false;
            Dissipation   = 0.0f;
        }
        
        void IModelPlug.OnAdd(byte spot, ModelSocketHandler sockets)
        {
            Sockets     = sockets;
            CurrentSpot = spot;
            for(int i = 0; i < nodes.Count; i++) {
                nodes.Array[i].AddedToSockets(spot, sockets);
            }
        }

        void IModelPlug.OnRemove(ModelSocketHandler sockets, ModelPlugRemoveBehavior removeBehavior)
        {
            Sockets     = null;
            CurrentSpot = 0;
            PoolableGO.SetParams(false, false);
            switch(removeBehavior) {
                case ModelPlugRemoveBehavior.Dissipate: {
                    Return();
                } break;
                case ModelPlugRemoveBehavior.Instant: {
                    EffectManager.ReturnImmediate(this);
                } break;
                
                case ModelPlugRemoveBehavior.None:
                    break;
            }
            for(int i = 0; i < nodes.Count; i++) {
                nodes.Array[i].RemovedFromSockets(sockets, removeBehavior);
            }
        }
    }
    
    public interface IEffectSpawned
    {
        void OnEffectSpawned();
    }
}
