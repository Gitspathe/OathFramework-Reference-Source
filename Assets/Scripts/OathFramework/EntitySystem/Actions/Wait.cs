using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OathFramework.EntitySystem.Actions
{ 

    public class Wait : Action
    {
        [field: Header("Wait Params")]

        [field: SerializeField] public float WaitTime       { get; private set; }
        [field: SerializeField] public float WaitTimeRandom { get; private set; }

        private UniTask task;

        protected override void OnInitialize()
        {

        }

        protected override void OnStart(CancellationToken ct)
        {
            task = ExecuteTask(ct);
        }

        protected override void OnTick(float deltaTime)
        {

        }

        protected override void OnEnd()
        {

        }

        private async UniTask ExecuteTask(CancellationToken ct)
        {
            float time = WaitTime;
            if(WaitTimeRandom > 0.001f) {
                time += Random.Range(0.0f, WaitTimeRandom);
            }
            if(await ShouldExit(UniTask.WaitForSeconds(time, cancellationToken: ct)))
                return;

            if(Interruption == InterruptionSource.None) {
                Completed();
            }
        }
    }

}
