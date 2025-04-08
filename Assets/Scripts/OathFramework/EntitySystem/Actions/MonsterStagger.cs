using Cysharp.Threading.Tasks;
using OathFramework.EntitySystem.Monsters;
using System.Threading;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    public class MonsterStagger : Stagger
    {
        protected override void OnStart(CancellationToken ct)
        {
            _ = ExecuteTask(ct);
        }

        protected override void OnTick(float deltaTime)
        {
            base.OnTick(deltaTime);
        }

        protected override void HandleKnockBack(Vector3 velocity)
        {
            MonsterController controller = (MonsterController)Entity.Controller;
            controller.NavAgent.Move(velocity * Time.deltaTime);
        }

        protected override void OnEnd()
        {
            base.OnEnd();
        }
        
        private async UniTask ExecuteTask(CancellationToken ct)
        {
            Animator.SetTrigger(NameHash);
            Animator.SetInteger(IndexHash, AnimIndex);
            Entity.Animation.ResetTriggers("stagger");
            if(await ShouldExit(UniTask.WaitForSeconds(AdjWaitTime, cancellationToken: ct)))
                return;

            if(Interruption == InterruptionSource.None) {
                Completed();
            }
        }
    }
}
