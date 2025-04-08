using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OathFramework.Effects
{
    public class PropManager : Subsystem
    {
        [SerializeField] private PropCollection[] props;
        
        public override string Name    => "Effect Prop Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EffectPropManager;
        
        public static PropManager Instance { get; private set; }
        
        private static Dictionary<ushort, Prop> prefabDict = new();
        private static Database database = new();
        
        public override async UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(PropManager)} singleton.");
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            foreach(PropCollection propCollection in props) {
                foreach(PoolParams pair in propCollection.pools) {
                    if(!pair.Prefab.TryGetComponent(out Prop prop)) {
                        Debug.LogError($"{pair.Prefab.name} is not a {nameof(Prop)}.");
                        continue;
                    }
                    if(!Register(prop))
                        continue;
                    
                    await PoolManager.RegisterPoolAsync(timer, new PoolManager.GameObjectPool(pair), true);
                }
            }
        }
        
        public static bool Register(Prop prop)
        {
            PropParams @params = prop.Params;
            if(database.RegisterWithID(@params.Key, prop, @params.DefaultID)) {
                prefabDict.Add(@params.DefaultID, prop);
                @params.ID = @params.DefaultID;
                return true;
            }
            if(database.Register(@params.Key, prop, out ushort retID)) {
                prefabDict.Add(retID, prop);
                @params.ID = retID;
                return true;
            }
            Debug.LogError($"Failed to register {nameof(Prop)} '{@params.Key}'.");
            return false;
        }
        
        public static Prop Retrieve(
            string key,
            Vector3? pos               = null,
            Quaternion? rot            = null,
            Vector3? scale             = null,
            ModelSocketHandler sockets = null,
            byte modelSpot             = 0)
        {
            if(!database.TryGet(key, out Prop _, out ushort id)) {
                Debug.LogError($"No {nameof(Prop)} with key '{key}' found.");
                return null;
            }
            return Retrieve(id, pos, rot, scale, sockets, modelSpot);
        }

        public static Prop Retrieve(
            ushort id,
            Vector3? pos               = null,
            Quaternion? rot            = null,
            Vector3? scale             = null,
            ModelSocketHandler sockets = null,
            byte modelSpot             = 0)
        {
            if(!database.TryGet(id, out Prop prefab, out _)) {
                Debug.LogError($"No {nameof(Prop)} with ID {id} found.");
                return null;
            }
            
            ModelSpot parent = null;
            if(!ReferenceEquals(sockets, null) && modelSpot != 0) {
                parent = sockets.GetModelSpot(modelSpot);
            }
            Prop instance = ReferenceEquals(parent, null) 
                ? PoolManager.Retrieve(prefab.gameObject, pos, rot, scale).GetComponent<Prop>() 
                : PoolManager.Retrieve(prefab.gameObject, pos, rot, scale, parent.Transform).GetComponent<Prop>();
            
            if(!ReferenceEquals(parent, null)) {
                sockets.AddPlug(parent.ID, instance);
            }
            return instance;
        }

        public static void Return(Prop prop, bool instant = false)
        {
            if(!ReferenceEquals(prop.Sockets, null)) {
                prop.Sockets.RemovePlug(prop, instant ? ModelPlugRemoveBehavior.Instant : ModelPlugRemoveBehavior.Dissipate);
            }
            if(instant) {
                prop.PoolableGO.Return();
            }
        }
        
        public static bool TryGetKey(ushort id, out string key)   => database.TryGet(id, out _, out key);
        public static bool TryGetNetID(string key, out ushort id) => database.TryGet(key, out _, out id);
        
        private sealed class Database : Database<string, ushort, Prop>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }
}
