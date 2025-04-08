using OathFramework.EntitySystem.States;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace OathFramework.EntitySystem
{

    [Serializable]
    public sealed class Stats
    {
        [HideInEditorMode] public uint health    = 100u;
        public uint maxHealth                    = 100u;
        [HideInEditorMode] public ushort stamina = 100;
        public ushort maxStamina                 = 100;
        public StaggerStrength staggerRes        = StaggerStrength.None;
        public uint poise                        = 100u;
        public float poiseReset                  = 1.0f;

        [Space(10)]

        public float speed     = 8.0f;
        public float turnSpeed = 180.0f;

        [Space(10)] 
        
        [TableList, SerializeField] private SerializedParam[] serializedParams;
        [NonSerialized] private Dictionary<ushort, float> @params = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FreeSerializedParams() => serializedParams = null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetParam(EntityStatParam eParam) => @params.TryGetValue(eParam.ID, out float v) ? v : StatParamDefaults.Get(eParam.ID);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetParam(EntityStatParam eParam)
        {
            StatParam sParam = eParam.Param;
            if(Math.Abs(eParam.Value - sParam.DefaultValue) < 0.00001f) {
                @params.Remove(eParam.ID);
            } else if(eParam.Value > sParam.MaxValue) {
                eParam = new EntityStatParam(sParam, sParam.MaxValue);
            } else if(eParam.Value < sParam.MinValue) {
                eParam = new EntityStatParam(sParam, sParam.MinValue);
            }
            @params[eParam.ID] = eParam.Value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetParam(StatParam sParam, float val)
        {
            SetParam(new EntityStatParam(sParam, val));
        }

        public void InitializeParams()
        {
            if(serializedParams == null)
                return;

            @params.Clear();
            foreach(SerializedParam param in serializedParams) {
                try {
                    SetParam(new EntityStatParam(param.paramID, param.value));
                } catch(Exception e) {
                    Debug.LogError(e);
                }
            }
        }
        
        public void CopyTo(Stats other, bool resetCurrent = true)
        {
            other.maxHealth = maxHealth;
            if(resetCurrent) {
                other.health = maxHealth;
            }
            if(other.health > other.maxHealth) {
                other.health = other.maxHealth;
            }
            other.maxStamina = maxStamina;
            other.stamina    = stamina;
            other.staggerRes = staggerRes;
            other.poise      = poise;
            other.poiseReset = poiseReset;
            other.speed      = speed;
            other.turnSpeed  = turnSpeed;
            other.@params.Clear();
            foreach(KeyValuePair<ushort, float> pair in @params) {
                other.@params.Add(pair.Key, pair.Value);
            }
        }
    }
    
    [Serializable]
    public class SerializedParam
    {
        [ValueDropdown("GetAllParams", DoubleClickToConfirm = true, OnlyChangeValueOnConfirm = true)]
        public string paramID;
        public float value;

#if UNITY_EDITOR
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static IEnumerable GetAllParams() => StringDropdownDB.GetValues<StatParam>();
#endif
    }
    
    public readonly struct EntityStatParam : IEquatable<EntityStatParam>
    {
        public StatParam Param { get; }
        public float Value     { get; }
        public string Key      => Param.LookupKey;
        public ushort ID       => Param.ID;
        
        public EntityStatParam(StatParam param, float value = 0.0f)
        {
            Param = param;
            Value = value;
        }

        public EntityStatParam(string paramKey, float value = 0.0f)
        {
            if(!StatParamManager.TryGet(paramKey, out StatParam param)) {
                Debug.LogError(new NullReferenceException(nameof(paramKey)));
                Param = null;
                Value = 0.0f;
                return;
            }
            Param = param;
            Value = value;
        }

        public EntityStatParam(ushort paramID, float value = 0.0f)
        {
            if(!StatParamManager.TryGet(paramID, out StatParam param)) {
                Debug.LogError(new NullReferenceException(nameof(paramID)));
                Param = null;
                Value = 0.0f;
                return;
            }
            Param = param;
            Value = value;
        }

        public static implicit operator StatParam(EntityStatParam param) => param.Param;
        
        public bool Equals(EntityStatParam other) => ID == other.ID && Math.Abs(Value - other.Value) < 0.0001f;
    }
    
}
