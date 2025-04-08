using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace OathFramework.Core
{
    public static class SubsystemManager
    {
        private static List<Subsystem> subsystems = new();
        private static int loaded;

        public static string CurrentSubTask { get; private set; } = "";
        public static float Progress => (float)loaded / (float)subsystems.Count;
        
        private static void Sort() => subsystems.Sort((x, y) => x.LoadOrder.CompareTo(y.LoadOrder));
        
        public static void Register(Subsystem system)
        {
            subsystems.Add(system);
        }

        public static async UniTask PreInitialize()
        {
            CurrentSubTask = "Pre-initializing";
            Sort();
            Stopwatch timer = new();
            timer.Start();
            foreach(Subsystem subsystem in subsystems) {
                await subsystem.PreInitialize(timer);
                if(timer.Elapsed.Milliseconds > AsyncFrameBudgets.High) {
                    await UniTask.Yield();
                    timer.Restart();
                }
            }
            timer.Stop();
        }
        
        public static async UniTask Initialize()
        {
            Sort();
            Stopwatch timer = new();
            timer.Start();
            foreach(Subsystem subsystem in subsystems) {
                CurrentSubTask = subsystem.Name;
                await subsystem.Initialize(timer);
                loaded++;
                if(timer.Elapsed.Milliseconds > AsyncFrameBudgets.High) {
                    CurrentSubTask = subsystem.Name;
                    await UniTask.Yield();
                    timer.Restart();
                }
            }
            timer.Stop();
        }
        
        public static async UniTask PostInitialize()
        {
            CurrentSubTask = "Post-initializing";
            Sort();
            Stopwatch timer = new();
            timer.Start();
            foreach(Subsystem subsystem in subsystems) {
                await subsystem.PostInitialize(timer);
                if(timer.Elapsed.Milliseconds > AsyncFrameBudgets.High) {
                    await UniTask.Yield();
                    timer.Restart();
                }
            }
            timer.Stop();
        }
    }
    
    public abstract class Subsystem : LoopComponent
    {
        public abstract string Name    { get; }
        public abstract uint LoadOrder { get; }
        
        protected virtual void Awake()
        {
            SubsystemManager.Register(this);
        }

        public virtual UniTask PreInitialize(Stopwatch timer) { return UniTask.CompletedTask; }
        public abstract UniTask Initialize(Stopwatch timer);
        public virtual UniTask PostInitialize(Stopwatch timer) { return UniTask.CompletedTask; }
    }

    public static class SubsystemLoadOrders
    {
        public static uint PoolManager                => 10;
        public static uint AudioPool                  => 11;
        public static uint UIAudio                    => 12;
        public static uint AchievementManager         => 13;
        public static uint SettingsManager            => 100;
        public static uint GameService                => 200;
        public static uint UIInfoManager              => 1_000;
        public static uint EquippableManager          => 1_100;
        public static uint EffectPropManager          => 1_200;
        public static uint EffectsManager             => 1_210;
        public static uint EntityFlagsManager         => 1_300;
        public static uint EntityStatParamsManager    => 1_310;
        public static uint EntityStatesManager        => 1_320;
        public static uint EntitySpEffectsManager     => 1_330;
        public static uint EntityManager              => 1_340;
        public static uint ConsumableUseParamsManager => 1_350;
        public static uint AttributeManager           => 1_370;
        public static uint RagdollManager             => 1_410;
        public static uint MapDecalsManager           => 1_440;
        public static uint HitEffectManager           => 1_450;
        public static uint PlayerManager              => 1_500;
        public static uint ProgressionManager         => 1_510;
        public static uint ProjectileManager          => 1_700;
        public static uint AbilityManager             => 1_800;
        public static uint PerkManager                => 1_810;
        public static uint FootstepManager            => 1_900;
        public static uint EventManager               => 2_000;
        public static uint NetGame                    => 2_100;
        public static uint GameUI                     => 2_200;
        public static uint Controls                   => 2_300;
        public static uint PersistenceManager         => 10_000;
        public static uint ProcGenManager             => 11_000;
        public static uint ShaderPreloader            => 100_000;
        public static uint JitOptimizer               => 100_100;
    }
    
    public static class AsyncFrameBudgets
    {
        public static int Background { get; set; } = 3;
        public static int Low        { get; set; } = (int)((1.0f / 60.0f) * 1000.0f);
        public static int Medium     { get; set; } = (int)((1.0f / 30.0f) * 1000.0f);
        public static int High       { get; set; } = (int)((1.0f / 10.0f) * 1000.0f);
    }
}
