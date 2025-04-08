using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.EntitySystem.Players;
using OathFramework.Utility;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Localization;
using Debug = UnityEngine.Debug;

namespace OathFramework.Progression
{
    public sealed class ProgressionManager : Subsystem, IInitialized
    {
        [field: SerializeField] public AnimationCurve ExpNeeded          { get; private set; }
        [field: SerializeField] public AnimationCurve PlayerCountExpMult { get; private set; }
        [field: SerializeField] public float SharedExpMult               { get; private set; } = 0.5f;
        
        [field: Space(10), Header("Strings")]
        
        [field: SerializeField] public LocalizedString PartialLoadFailureTitleStr { get; private set; }
        [field: SerializeField] public LocalizedString PartialLoadFailureMsgStr   { get; private set; }
        [field: SerializeField] public LocalizedString FatalLoadFailureTitleStr   { get; private set; }
        [field: SerializeField] public LocalizedString FatalLoadFailureMsgStr     { get; private set; }
        public static PlayerProfile Profile       { get; private set; }
        public static ProgressionManager Instance { get; private set; }

        public override string Name    => "Progression Manager";
        public override uint LoadOrder => SubsystemLoadOrders.ProgressionManager;

        uint ILockableOrderedListElement.Order => 10;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(ProgressionManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            GameCallbacks.Register((IInitialized)this);
            return UniTask.CompletedTask;
        }
        
        async UniTask IInitialized.OnGameInitialized()
        {
            // TODO: Multiple profiles.
            Profile = PlayerProfile.Default;
            await Profile.Initialize();
        }

        public static float GetExpMult() => Instance.PlayerCountExpMult.Evaluate(PlayerManager.PlayerCount);
        public static uint GetExpNeeded(byte nextLevel) => (uint)Instance.ExpNeeded.Evaluate(nextLevel);

#if UNITY_EDITOR
        [ContextMenu("Give Exp")]
        private void DebugGiveExp()
        {
            if(!Application.isPlaying)
                return;

            Profile.AddExp(10_000);
        }
#endif
        
    }
}
