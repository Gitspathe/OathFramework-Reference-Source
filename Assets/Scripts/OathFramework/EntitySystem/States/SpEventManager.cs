using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Data.SpEvents;
using OathFramework.Utility;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem.States
{
    public class SpEventManager : Subsystem
    {
        public override string Name    => "Entity SpEffects Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EntitySpEffectsManager;
        
        private static Database database = new();
        
        public static SpEventManager Instance { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(SpEventManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;

            Register<QuickHeal>();
            Register<ModelEffect>();
            Register<ApplyState>();
            return UniTask.CompletedTask;
        }
        
        // ReSharper disable once ObjectCreationAsStatement
        public static void Register<T>() where T : SpEvent, new() => new T().PostCtor();

        public static bool Register(SpEvent ev, out ushort id)
        {
            id = 0;
            if(ev.DefaultID.HasValue) {
                if(database.RegisterWithID(ev.LookupKey, ev, ev.DefaultID.Value)) {
                    id = ev.DefaultID.Value;
                    ev.Initialize();
                    return true;
                }
            }
            if(database.Register(ev.LookupKey, ev, out ushort retID)) {
                id = retID;
                ev.Initialize();
                return true;
            }
            Debug.LogError($"Failed to register {nameof(SpEvent)} '{ev.LookupKey}'");
            return false;
        }
        
        public static bool TryGet(string key, out SpEvent ev)
        {
            if(database.TryGet(key, out ev, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(SpEvent)} for key '{key}' found.");
            }
            return false;
        }
        
        public static bool TryGet(ushort id, out SpEvent ev)
        {
            if(database.TryGet(id, out ev, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(SpEvent)} for ID '{id}' found.");
            }
            return false;
        }
        
        public static SpEvent Get(string key)
        {
            if(database.TryGet(key, out SpEvent ev, out _))
                return ev;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(SpEvent)} for key '{key}' found.");
            }
            return null;
        }
        
        public static SpEvent Get(ushort id)
        {
            if(database.TryGet(id, out SpEvent ev, out _))
                return ev;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(SpEvent)} for ID '{id}' found.");
            }
            return null;
        }
        
        public static bool TryGetKey(ushort id, out string key)   => database.TryGet(id, out _, out key);
        public static bool TryGetNetID(string key, out ushort id) => database.TryGet(key, out _, out id);
        
        private sealed class Database : Database<string, ushort, SpEvent>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
