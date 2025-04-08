using Cysharp.Threading.Tasks;
using GameCode.MagitechRequiem.Data.Perks;
using OathFramework.Core;
using OathFramework.Utility;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.PerkSystem
{
    public class PerkManager : Subsystem
    {
        public override string Name    => "Perk Manager";
        public override uint LoadOrder => SubsystemLoadOrders.PerkManager;
        
        private static Database database = new();
        
        public static LockableHashSet<IUpdateable> ToUpdate { get; } = new();
        public static PerkManager Instance                  { get; private set; }
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(PerkManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            
            Register<Perk1>();
            Register<Perk2>();
            Register<Perk3>();
            Register<Perk4>();
            Register<Perk5>();
            Register<Perk6>();
            Register<Perk7>();
            Register<Perk8>();
            Register<Perk9>();
            Register<Perk10>();
            Register<Perk11>();
            Register<Perk12>();
            Register<Perk13>();
            Register<Perk14>();
            Register<Perk15>();
            Register<Perk16>();
            Register<Perk17>();
            Register<Perk18>();
            Register<Perk19>();
            Register<Perk20>();
            Register<Perk21>();
            Register<Perk22>();
            Register<Perk23>();
            Register<Perk24>();
            Register<Perk25>();
            Register<Perk26>();
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
        public static void Register<T>() where T : Perk, new() => new T().PostCtor();

        public static bool Register(Perk perk, out ushort id)
        {
            id = 0;
            if(perk.DefaultID.HasValue && database.RegisterWithID(perk.LookupKey, perk, perk.DefaultID.Value)) {
                id = perk.DefaultID.Value;
                perk.Initialize();
                return true;
            }
            if(database.Register(perk.LookupKey, perk, out ushort retID)) {
                id = retID;
                perk.Initialize();
                return true;
            }
            Debug.LogError($"Failed to register {nameof(Perk)} lookup '{perk.LookupKey}'.");
            return false;
        }
        
        public static bool TryGet(string key, out Perk perk) => database.TryGet(key, out perk, out _);
        public static bool TryGet(ushort id, out Perk perk) => database.TryGet(id, out perk, out _);

        public static Perk Get(string key)
        {
            if(database.TryGet(key, out Perk perk, out _))
                return perk;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(Perk)} for lookup '{key}' found.");
            }
            return null;
        }
        
        public static Perk Get(ushort id)
        {
            if(database.TryGet(id, out Perk perk, out _))
                return perk;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(Perk)} for ID '{id}' found.");
            }
            return null;
        }
        
        public static bool TryGetKey(ushort id, out string key)   => database.TryGet(id, out _, out key);
        public static bool TryGetNetID(string key, out ushort id) => database.TryGet(key, out _, out id);
        
        private sealed class Database : Database<string, ushort, Perk>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
