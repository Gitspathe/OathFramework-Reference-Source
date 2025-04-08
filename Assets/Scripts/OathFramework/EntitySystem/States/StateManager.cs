using Cysharp.Threading.Tasks;
using GameCode.MagitechRequiem.Data.Abilities;
using GameCode.MagitechRequiem.Data.Perks;
using OathFramework.Core;
using OathFramework.Data.EntityStates;
using OathFramework.Utility;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem.States
{
    public sealed class StateManager : Subsystem
    {
        public override string Name    => "Entity States Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EntityStatesManager;
        
        private static Database database = new();
        
        public static LockableHashSet<IUpdateable> ToUpdate { get; } = new();
        public static StateManager Instance                 { get; private set; }

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(StateManager)} singleton.");
                Destroy(gameObject);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;

            Register<GodModeState>();
            Register<ConstitutionAttribute>();
            Register<EnduranceAttribute>();
            Register<AgilityAttribute>();
            Register<StrengthAttribute>();
            Register<ExpertiseAttribute>();
            Register<IntelligenceAttribute>();
            Register<Shield>();
            Register<Stunned>();
            Register<GunBuff1State>();
            Register<Invulnerable>();
            Register<Perk4State>();
            Register<Perk5State>();
            Register<Perk6State>();
            Register<Perk8State>();
            Register<Perk13State>();
            Register<Perk14State>();
            Register<Perk16State>();
            Register<Perk16PassiveState>();
            Register<Perk18State>();
            Register<Perk20State>();
            Register<Perk21State>();
            Register<Perk22State>();
            Register<Perk23State>();
            Register<Perk26State>();
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
        public static void Register<T>() where T : State, new() => new T().PostCtor();

        public static bool Register(State state, out ushort id)
        {
            id = 0;
            if(state.DefaultID.HasValue) {
                if(database.RegisterWithID(state.LookupKey, state, state.DefaultID.Value)) {
                    id = state.DefaultID.Value;
                    state.Initialize();
                    return true;
                }
            }
            if(database.Register(state.LookupKey, state, out ushort retID)) {
                id = retID;
                state.Initialize();
                return true;
            }
            Debug.LogError($"Failed to register {nameof(EntityState)} '{state.LookupKey}'");
            return false;
        }
        
        public static bool TryGet(string key, out State state)
        {
            if(database.TryGet(key, out state, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(EntityState)} for lookup '{key}' found.");
            }
            return false;
        }
        
        public static bool TryGet(ushort id, out State state)
        {
            if(database.TryGet(id, out state, out _))
                return true;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(EntityState)} for ID '{id}' found.");
            }
            return false;
        }
        
        public static State Get(string key)
        {
            if(database.TryGet(key, out State effect, out _))
                return effect;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(EntityState)} for lookup '{key}' found.");
            }
            return null;
        }
        
        public static State Get(ushort id)
        {
            if(database.TryGet(id, out State effect, out _))
                return effect;

            if(Game.ExtendedDebug) {
                Debug.LogError($"No {nameof(EntityState)} for ID '{id}' found.");
            }
            return null;
        }
        
        public static bool TryGetKey(ushort id, out string key)   => database.TryGet(id, out _, out key);
        public static bool TryGetNetID(string key, out ushort id) => database.TryGet(key, out _, out id);

        private sealed class Database : Database<string, ushort, State>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
