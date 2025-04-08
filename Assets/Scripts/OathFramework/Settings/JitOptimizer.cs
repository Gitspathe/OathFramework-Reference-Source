using Cysharp.Threading.Tasks;
using OathFramework.Core;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace OathFramework.Settings
{

    public class JitOptimizer : Subsystem
    {
        [SerializeField] private bool runJitOptimizer = true;
        
        public override string Name    => "Jit Optimizer";
        public override uint LoadOrder => SubsystemLoadOrders.JitOptimizer;

        public static JitOptimizer Instance { get; private set; }
        
        public override async UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(JitOptimizer)} singleton.");
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            try {
                await RunJitOptimizer();
            } catch(Exception e) {
                Debug.LogError($"JitOptimizer exited early: {e.Message}");
            }
        }

        private async UniTask RunJitOptimizer()
        {
            if(!runJitOptimizer)
                return;

            foreach(GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
                foreach(IJitOptimizerTask jitTask in obj.GetComponentsInChildren<IJitOptimizerTask>(true)) {
                    await jitTask.Run();
                }
            }
        }
    }

    public interface IJitOptimizerTask
    {
        UniTask Run();
    }

}
