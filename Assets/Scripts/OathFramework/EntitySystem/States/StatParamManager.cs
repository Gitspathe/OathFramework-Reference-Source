using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Data.StatParams;
using OathFramework.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem.States
{
    public class StatParamManager : Subsystem
    {
        public override string Name    => "Entity Stat Params Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EntityStatParamsManager;
        
        private static Database database = new();
        
        public static StatParamManager Instance { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(StatParamManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;

            Register<ExpReward>();
            Register<ExpMult>();
            Register<CriticalMult>();
            Register<CriticalChance>();
            Register<BaseDamage>();
            Register<DamageMult>();
            Register<AttackSpeedMult>();
            Register<ReloadSpeedMult>();
            Register<SwapSpeedMult>();
            Register<AccuracyMult>();
            Register<ProjectileSpeedMult>();
            Register<ExplosiveRangeMult>();
            Register<MaxRangeMult>();
            Register<StaminaRegen>();
            Register<StaminaRegenDelay>();
            Register<DodgeStaminaUse>();
            Register<DodgeDurationMult>();
            Register<DodgeSpeedMult>();
            Register<DodgeIFramesMult>();
            Register<DodgeIFrames>();
            Register<ItemUseSpeedMult>();
            Register<QuickHealCharges>();
            Register<QuickHealAmount>();
            Register<QuickHealSpeedMult>();
            Register<AbilityUseSpeedMult>();
            return UniTask.CompletedTask;
        }
        
        public static void Register<T>() where T : StatParam, new() => new T().Initialize();

        public static bool Register(StatParam statParam, out ushort id)
        {
            id = 0;
            if(statParam.DefaultID.HasValue) {
                if(database.RegisterWithID(statParam.LookupKey, statParam, statParam.DefaultID.Value)) {
                    id = statParam.DefaultID.Value;
                    return true;
                }
            }
            if(database.Register(statParam.LookupKey, statParam, out ushort retID)) {
                id = retID;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(StatParam)} lookup '{statParam.LookupKey}'.");
            return false;
        }
        
        public static bool TryGet(string key, out StatParam statParam)
        {
            if(database.TryGet(key, out statParam, out _))
                return true;
            
            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(StatParam)} for lookup '{key}' found.");
            }
            return false;
        }
        
        public static bool TryGet(ushort id, out StatParam statParam)
        {
            if(database.TryGet(id, out statParam, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(StatParam)} for ID '{id}' found.");
            }
            return false;
        }
        
        public static StatParam Get(string key)
        {
            if(database.TryGet(key, out StatParam param, out _))
                return param;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(StatParam)} for lookup '{key}' found.");
            }
            return null;
        }
        
        public static StatParam Get(ushort id)
        {
            if(database.TryGet(id, out StatParam param, out _))
                return param;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(StatParam)} for ID '{id}' found.");
            }
            return null;
        }
        
        public static bool TryGetKey(ushort id, out string key) => database.TryGet(id, out _, out key);
        public static bool TryGetID(string key, out ushort id)  => database.TryGet(key, out _, out id);
        
        private sealed class Database : Database<string, ushort, StatParam>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
    
    public static class StatParamDefaults
    {
        private static Dictionary<ushort, float> defaultLookup = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Get(ushort id) => defaultLookup.GetValueOrDefault(id, 0.0f);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(ushort id, float value) => defaultLookup[id] = value;
    }
}
