using Cysharp.Threading.Tasks;
using GameCode.MagitechRequiem.Data.Abilities;
using OathFramework.Core;
using OathFramework.Utility;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.AbilitySystem
{
    public class AbilityManager : Subsystem
    {
        public override string Name    => "Ability Manager";
        public override uint LoadOrder => SubsystemLoadOrders.AbilityManager;
        
        private static Database database = new();
        
        public static LockableHashSet<IUpdateable> ToUpdate { get; } = new();
        public static AbilityManager Instance               { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(AbilityManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;

            Register<Ability1>();
            Register<Ability2>();
            Register<Ability3>();
            Register<Ability4>();
            Register<Ability5>();
            Register<Ability6>();
            return UniTask.CompletedTask;
        }
        
        private void Update()
        {
            ToUpdate.Lock();
            foreach(IUpdateable updateable in ToUpdate.Current) {
                updateable.Update();
            }
            ToUpdate.Unlock();
        }
        
        // ReSharper disable once ObjectCreationAsStatement
        public static void Register<T>() where T : Ability, new() => new T().PostCtor();

        public static bool Register(Ability ability, out ushort id)
        {
            id = 0;
            if(ability.DefaultID.HasValue && database.RegisterWithID(ability.LookupKey, ability, ability.DefaultID.Value)) {
                id = ability.DefaultID.Value;
                ability.Initialize();
                return true;
            }
            if(database.Register(ability.LookupKey, ability, out ushort retID)) {
                id = retID;
                ability.Initialize();
                return true;
            }
            Debug.LogError($"Failed to register {nameof(Ability)} lookup '{ability.LookupKey}'.");
            return false;
        }
        
        public static bool TryGet(string key, out Ability ability) => database.TryGet(key, out ability, out _);
        public static bool TryGet(ushort id, out Ability ability) => database.TryGet(id, out ability, out _);

        public static Ability Get(string key)
        {
            if(database.TryGet(key, out Ability ability, out _))
                return ability;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(Ability)} for lookup '{key}' found.");
            }
            return null;
        }
        
        public static Ability Get(ushort id)
        {
            if(database.TryGet(id, out Ability ability, out _))
                return ability;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(Ability)} for ID '{id}' found.");
            }
            return null;
        }
        
        public static bool TryGetKey(ushort id, out string key)   => database.TryGet(id, out _, out key);
        public static bool TryGetNetID(string key, out ushort id) => database.TryGet(key, out _, out id);
        
        private sealed class Database : Database<string, ushort, Ability>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
