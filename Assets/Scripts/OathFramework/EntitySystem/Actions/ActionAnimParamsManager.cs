using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Utility;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem.Actions
{
    public class ActionAnimParamsManager : Subsystem
    {
        public override string Name    => "Ability Anim Params Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EntityStatParamsManager;

        [SerializeField] private ActionAnimParams[] presetParams;
        
        private static Database database = new();
        
        public static ActionAnimParamsManager Instance { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(ActionAnimParamsManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            foreach(ActionAnimParams @params in presetParams) {
                if(Register(@params, out ushort id)) {
                    @params.ID = id;
                }
            }
            return UniTask.CompletedTask;
        }
        
        public static bool Register(ActionAnimParams @params, out ushort id)
        {
            id = 0;
            if(database.RegisterWithID(@params.LookupKey, @params, @params.DefaultID)) {
                id = @params.DefaultID;
                return true;
            }
            if(database.Register(@params.LookupKey, @params, out ushort retID)) {
                id = retID;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(ActionAnimParams)} lookup '{@params.LookupKey}'.");
            return false;
        }
        
        public static bool TryGet(string key, out ActionAnimParams @params)
        {
            if(database.TryGet(key, out @params, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(ActionAnimParams)} for lookup '{key}' found.");
            }
            return false;
        }
        
        public static bool TryGet(ushort id, out ActionAnimParams @params)
        {
            if(database.TryGet(id, out @params, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(ActionAnimParams)} for ID '{id}' found.");
            }
            return false;
        }
        
        public static ActionAnimParams Get(string key)
        {
            if(database.TryGet(key, out ActionAnimParams @params, out _))
                return @params;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(ActionAnimParams)} for lookup '{key}' found.");
            }
            return null;
        }
        
        public static ActionAnimParams Get(ushort id)
        {
            if(database.TryGet(id, out ActionAnimParams @params, out _))
                return @params;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(ActionAnimParams)} for ID '{id}' found.");
            }
            return null;
        }
        
        public static bool TryGetKey(ushort id, out string key) => database.TryGet(id, out _, out key);
        public static bool TryGetID(string key, out ushort id)  => database.TryGet(key, out _, out id);
        
        private sealed class Database : Database<string, ushort, ActionAnimParams>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
