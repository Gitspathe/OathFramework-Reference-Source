using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Utility;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem.States
{
    public class FlagManager : Subsystem
    {
        public override string Name    => "Entity Flags Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EntityFlagsManager;
        
        private static Database database = new();
        
        public static FlagManager Instance { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(FlagManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            return UniTask.CompletedTask;
        }
        
        // ReSharper disable once ObjectCreationAsStatement
        public static void Register<T>() where T : Flag, new() => new T();

        public static bool Register(Flag flag, out ushort id)
        {
            id = 0;
            if(flag.DefaultID.HasValue && database.RegisterWithID(flag.LookupKey, flag, flag.DefaultID.Value)) {
                id = flag.DefaultID.Value;
                return true;
            }
            if(database.Register(flag.LookupKey, flag, out ushort retID)) {
                id = retID;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(Flag)} lookup '{flag.LookupKey}'.");
            return false;
        }
        
        public static bool TryGet(string key, out Flag flag)
        {
            if(database.TryGet(key, out flag, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(Flag)} for lookup '{key}' found.");
            }
            return false;
        }
        
        public static bool TryGet(ushort id, out Flag flag)
        {
            if(database.TryGet(id, out flag, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(Flag)} for ID '{id}' found.");
            }
            return false;
        }
        
        public static Flag Get(string key)
        {
            if(database.TryGet(key, out Flag flag, out _))
                return flag;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(Flag)} for lookup '{key}' found.");
            }
            return null;
        }
        
        public static Flag Get(ushort id)
        {
            if(database.TryGet(id, out Flag flag, out _))
                return flag;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(Flag)} for ID '{id}' found.");
            }
            return null;
        }
        
        public static bool TryGetKey(ushort id, out string key)   => database.TryGet(id, out _, out key);
        public static bool TryGetNetID(string key, out ushort id) => database.TryGet(key, out _, out id);
        
        private sealed class Database : Database<string, ushort, Flag>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
