using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.Pooling;
using OathFramework.Utility;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace OathFramework.EquipmentSystem
{
    public sealed class EquippableManager : Subsystem
    {
        [field: Header("Throwing")]
        [field: SerializeField] public PoolParams ArcPrefab       { get; private set; }
        [field: SerializeField] public int ArcPointCount          { get; private set; } = 25;
        [field: SerializeField] public float ArcTimeBetweenPoints { get; private set; } = 0.1f;
        
        [field: Header("Accuracy")]
        [field: SerializeField] public float BaseYVariance      { get; private set; } = 0.25f;
        [field: SerializeField] public float RecoilAngle        { get; private set; } = 2.0f;
        [field: SerializeField] public float RecoilHeight       { get; private set; } = 0.175f;

        [field: Space(5)]

        [field: SerializeField] public float InaccuracyAngle    { get; private set; } = 30.0f;
        [field: SerializeField] public float InaccuracyHeight   { get; private set; } = 0.334f;
        
        [field: Space(5)]
        
        [field: SerializeField] public float PelletSpreadAngle  { get; private set; } = 10.0f;
        [field: SerializeField] public float PelletSpreadHeight { get; private set; } = 0.5f;

        private Database database = new();
        
        public static EquippableManager Instance { get; private set; }
        
        public override string Name    => "Equippable Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EquippableManager;

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(EquippableManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            foreach(Equippable equippable in GetComponentsInChildren<Equippable>(true)) {
                if(!Register(equippable, out ushort netID))
                    continue;
                
                equippable.ID = netID;
                equippable.gameObject.SetActive(false);
                equippable.RegisterInfo();
            }
            foreach(IEquippableManagerInit init in GetComponentsInChildren<IEquippableManagerInit>(true)) {
                init.OnEquippableManagerInit();
            }
            PoolManager.RegisterPool(new PoolManager.GameObjectPool(ArcPrefab), true);

            Instance = this;
            return UniTask.CompletedTask;
        }

        public static PreviewTrajectory RetrieveTrajectoryArc(Transform parent)
        {
            if(parent == null)
                return null;
            
            return PoolManager.Retrieve(Instance.ArcPrefab.Prefab, parent).GetComponent<PreviewTrajectory>();
        }

        public static void ReturnTrajectoryArc(PreviewTrajectory trajectoryArc)
        {
            if(trajectoryArc == null)
                return;
            
            PoolManager.Return(trajectoryArc.PoolableGO);
        }

        private bool Register(Equippable equippable, out ushort netID)
        {
            netID = default;
            if(database.RegisterWithID(equippable.EquippableKey, equippable, equippable.DefaultID)) {
                netID = equippable.DefaultID;
                return true;
            }
            if(database.Register(equippable.EquippableKey, equippable, out _)) {
                netID = (ushort)(database.CurrentID - 1);
                return true;
            }

            if(Game.ExtendedDebug) {
                Debug.LogError($"Attempted to register duplicate {nameof(Equippable)} '{equippable.EquippableKey}'.");
            }
            return false;
        }

        public static T CloneModel<T>(IEquipmentUserController entity, string key, ModelPerspective perspective) where T : EquippableModel
        {
            if(!Instance.database.TryGet(key, out Equippable template, out _)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No template for {nameof(Equippable)} '{key}' found.");
                }
                return null;
            }
            return template.CloneModel<T>(entity, perspective);
        }

        public static Equippable GetTemplate(string key)
        {
            if(!Instance.database.TryGet(key, out Equippable template, out _)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No template for {nameof(Equippable)} '{key}' found.");
                }
                return null;
            }
            return template;
        }
        
        public static Equippable GetTemplate(ushort netID)
        {
            if(!Instance.database.TryGet(netID, out Equippable template, out _)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No template for {nameof(Equippable)} netID '{netID}' found.");
                }
                return null;
            }
            return template;
        }

        public static uint GetNetID(string equippable)
        {
            if(!Instance.database.TryGet(equippable, out Equippable _, out ushort netID)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No NetID for {nameof(Equippable)} '{equippable}' found.");
                }
                return 0;
            }
            return netID;
        }

        public static bool TryGetKey(ushort netID, out string key)   => Instance.database.TryGet(netID, out _, out key);
        public static bool TryGetNetID(string key, out ushort netID) => Instance.database.TryGet(key, out _, out netID);

        public static float RollBaseYVariance()
        {
            return Random.Range(-Instance.BaseYVariance, Instance.BaseYVariance);
        }

        public static float RollRecoilAngle(float recoil)
        {
            float val = recoil * Instance.RecoilAngle;
            return Random.Range(-val, val);
        }

        public static float RollRecoilHeight(float recoil)
        {
            float val = recoil * Instance.RecoilHeight;
            return Random.Range(-val, val);
        }

        public static float RollInaccuracyAngle(float accuracy)
        {
            float inaccuracy = Mathf.Clamp(1.0f - accuracy, 0.0f, 1.0f) * Instance.InaccuracyAngle;
            return Random.Range(-inaccuracy, inaccuracy);
        }

        public static float RollInaccuracyHeight(float accuracy)
        {
            float inaccuracy = Mathf.Clamp(1.0f - accuracy, 0.0f, 1.0f) * Instance.InaccuracyHeight;
            return Random.Range(-inaccuracy, inaccuracy);
        }

        public static Vector2 RollPelletSpread(float pelletSpread)
        {
            if(Mathf.Abs(pelletSpread) < 0.001f)
                return Vector2.zero;

            float inaccuracy = Mathf.Clamp(pelletSpread, 0.0f, 9999.0f) * Instance.PelletSpreadAngle;
            float height     = Mathf.Clamp(pelletSpread, 0.0f, 9999.0f) * Instance.PelletSpreadHeight;
            return new Vector2(
                Random.Range(-inaccuracy, inaccuracy),
                Random.Range(-height, height)
            );
        }
        
        private sealed class Database : Database<string, ushort, Equippable>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;
            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }

    public interface IEquippableManagerInit
    {
        void OnEquippableManagerInit();
    }
}
