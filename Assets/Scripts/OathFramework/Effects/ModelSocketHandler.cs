using OathFramework.Core;
using OathFramework.Pooling;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Scripting;

namespace OathFramework.Effects
{
    public class ModelSocketHandler : MonoBehaviour, IPoolableComponent
    {
        [SerializeField, OnValueChanged("VerifyData")] protected ModelSpotsConfig config;
        [SerializeField] protected ModelSpot[] data;

        private Dictionary<byte, ModelSpot> dict = new();
        private HashSet<IModelPlug> allPlugs     = new();
        private Dictionary<byte, QList<IModelPlug>> plugsDict;
        
        public IModelSockets Model           { get; protected set; }
        public PoolableGameObject PoolableGO { get; set; }
        
        [Button("Refresh")]
        [Preserve, MethodImpl(MethodImplOptions.NoOptimization)]
        private void VerifyData()
        {
#if UNITY_EDITOR
            ModelSpot[] oldData = new ModelSpot[data.Length];
            data.CopyTo(oldData, 0);
            if(config == null) {
                data = Array.Empty<ModelSpot>();
                EditorUtility.SetDirty(this);
                return;
            }

            data = new ModelSpot[config.@params.Length];
            for(int i = 0; i < config.@params.Length; i++) {
                bool            exists = false;
                ModelSpotParams spotParams;
                foreach(ModelSpot spot in oldData) {
                    spotParams = config.@params[i];
                    if(spot == null || spot.ID != spotParams.ID)
                        continue;

                    data[i] = new ModelSpot(spotParams.ID, spotParams.Name, spot.Transform);
                    exists  = true;
                    break;
                }
                if(exists)
                    continue;
                
                spotParams = config.@params[i];
                data[i]    = new ModelSpot(spotParams.ID, spotParams.Name, null);
            }
            EditorUtility.SetDirty(this);
#endif
        }
        
        protected virtual void Awake()
        {
            Model = GetComponent<IModelSockets>();
            foreach(ModelSpot spot in data) {
                dict.Add(spot.ID, spot);
            }
            plugsDict = new Dictionary<byte, QList<IModelPlug>>(dict.Count);
            foreach(ModelSpot spot in dict.Values) {
                if(ReferenceEquals(spot.Transform, null))
                    continue;
                
                plugsDict.Add(spot.ID, new QList<IModelPlug>());
            }
        }
        
        public bool TryGetEffect(string effectKey, out Effect effect)
        {
            effect = null;
            if(!EffectManager.TryGetID(effectKey, out ushort id)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No effect found for key {effectKey}");
                }
                return false;
            }
            return TryGetEffect(id, out effect);
        }
        
        public bool TryGetEffect(ushort effectID, out Effect effect)
        {
            effect = null;
            foreach(IModelPlug plug in allPlugs) {
                if(!(plug is Effect e))
                    continue;

                if(e.ID == effectID) {
                    effect = e;
                    return true;
                }
            }
            return false;
        }

        public bool HasEffect(string effectKey)
        {
            if(!EffectManager.TryGetID(effectKey, out ushort id)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No effect found for key {effectKey}");
                }
                return false;
            }
            return HasEffect(id);
        }
        
        public bool HasEffect(ushort effectID)
        {
            foreach(IModelPlug plug in allPlugs) {
                if(!(plug is Effect e))
                    continue;

                if(e.ID == effectID)
                    return true;
            }
            return false;
        }

        public ModelSpot GetModelSpot(byte id, bool allowFallback = true)
        {
            int count = 0;
            while(true) {
                if(count++ > byte.MaxValue) {
                    Debug.LogError("Failed to find ModelSpot: recursive loop detected.");
                    return null;
                }
                
                if(dict.TryGetValue(id, out ModelSpot spot))
                    return spot;
                if(!allowFallback)
                    return null;
                
                byte? next = config.GetFallback(id);
                if(next == null) {
                    return null;
                }
                id = next.Value;
            }
        }
        
        private void OnDisable()
        {
            if(!ReferenceEquals(PoolableGO, null))
                return;

            ReturnPlugs(Game.State == GameState.Quitting ? ModelPlugRemoveBehavior.Instant : ModelPlugRemoveBehavior.Dissipate);
        }

        private void ReturnPlugs(ModelPlugRemoveBehavior removeBehavior)
        {
            foreach(QList<IModelPlug> plugs in plugsDict.Values) {
                if(removeBehavior != ModelPlugRemoveBehavior.None) {
                    for(int i = 0; i < plugs.Count; i++) {
                        plugs.Array[i].OnRemove(this, removeBehavior);
                    }
                }
                plugs.Clear();
            }
            allPlugs.Clear();
        }
        
        public void AddPlug(byte spot, IModelPlug plug)
        {
            if(!plugsDict.TryGetValue(spot, out QList<IModelPlug> plugs))
                return;
            
            plugs.Add(plug);
            allPlugs.Add(plug);
            plug.OnAdd(spot, this);
        }

        public void RemovePlug(IModelPlug plug, ModelPlugRemoveBehavior removeBehavior = ModelPlugRemoveBehavior.None)
        {
            if(!plugsDict.TryGetValue(plug.CurrentSpot, out QList<IModelPlug> plugs))
                return;
            
            plugs.Remove(plug);
            allPlugs.Remove(plug);
            plug.OnRemove(this, removeBehavior);
        }

        public void RemovePlug(ushort effectID, ModelPlugRemoveBehavior removeBehavior = ModelPlugRemoveBehavior.None)
        {
            QList<IModelPlug> toRemove = StaticObjectPool<QList<IModelPlug>>.Retrieve();
            foreach(IModelPlug plug in allPlugs) {
                if(plug.ID == effectID) {
                    toRemove.Add(plug);
                }
            }
            for(int i = 0; i < toRemove.Count; i++) {
                IModelPlug plug = toRemove.Array[i];
                allPlugs.Remove(plug);
                plugsDict[plug.CurrentSpot].Remove(plug);
                plug.OnRemove(this, removeBehavior);
            }
            toRemove.Clear();
            StaticObjectPool<QList<IModelPlug>>.Return(toRemove);
        }

        public void ApplyCopyablePlugs(in QList<CopyableModelPlug.CopyData> copyData)
        {
            for(int i = 0; i < copyData.Count; i++) {
                CopyableModelPlug.InitializeCopy(this, in copyData.Array[i]);
            }
        }
        
        public void GetCopyablePlugs(QList<CopyableModelPlug.CopyData> copyData)
        {
            foreach(IModelPlug plug in allPlugs) {
                if(!((MonoBehaviour)plug).TryGetComponent(out CopyableModelPlug copy))
                    continue;
                
                copyData.Add(copy.GetData());
            }
        }

        void IPoolableComponent.OnRetrieve()
        {
            
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            ReturnPlugs(Game.State == GameState.Quitting ? ModelPlugRemoveBehavior.Instant : ModelPlugRemoveBehavior.Dissipate);
        }
    }
}
