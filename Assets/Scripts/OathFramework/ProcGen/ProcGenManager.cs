using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.ProcGen
{
    public sealed class ProcGenManager : Subsystem
    {
        public override string Name    => "ProcGen Manager";
        public override uint LoadOrder => SubsystemLoadOrders.ProcGenManager;
        
        private static Database database = new();

        [SerializeField] private List<MapConfig> configs = new();
        
        public static ProcGenManager Instance { get; private set; }

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(ProcGenManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            foreach(MapConfig conf in configs) {
                Register(conf);
            }
            return UniTask.CompletedTask;
        }
        
        public static bool Register(MapConfig conf)
        {
            ushort id;
            if(database.RegisterWithID(conf.Key, conf, conf.DefaultID)) {
                id      = conf.DefaultID;
                conf.ID = id;
                return true;
            }
            if(database.Register(conf.Key, conf, out ushort retID)) {
                id      = retID;
                conf.ID = id;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(MapConfig)} '{conf.Key}'");
            return false;
        }
        
        public static bool TryGet(string key, out MapConfig config)
        {
            if(database.TryGet(key, out config, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(MapConfig)} for lookup '{key}' found.");
            }
            return false;
        }
        
        public static bool TryGet(ushort id, out MapConfig config)
        {
            if(database.TryGet(id, out config, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(MapConfig)} for ID '{id}' found.");
            }
            return false;
        }
        
        public static MapConfig Get(string key)
        {
            if(database.TryGet(key, out MapConfig config, out _))
                return config;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(MapConfig)} for lookup '{key}' found.");
            }
            return null;
        }
        
        public static MapConfig Get(ushort id)
        {
            if(database.TryGet(id, out MapConfig config, out _))
                return config;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(MapConfig)} for ID '{id}' found.");
            }
            return null;
        }
        
        public static bool TryGetKey(ushort id, out string key)   => database.TryGet(id, out _, out key);
        public static bool TryGetNetID(string key, out ushort id) => database.TryGet(key, out _, out id);
        
        private sealed class Database : Database<string, ushort, MapConfig>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
