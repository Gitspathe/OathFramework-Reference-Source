using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{

    public class Animation : Action
    {
        private int nameHash;
        private int indexHash;
        private UniTask task;
        
        [field: Header("Animation Params")]

        [field: SerializeField] public int AnimIndex         { get; private set; }
        [field: SerializeField] public float WaitTime        { get; private set; }
        [field: SerializeField] public string AnimatorAction { get; private set; } = "Action";
        [field: SerializeField] public string AnimatorIndex  { get; private set; } = "ActionIndex";

        protected Animator Animator => Entity.EntityModel.Animator;

        protected override void OnInitialize()
        {
            nameHash  = Animator.StringToHash(AnimatorAction);
            indexHash = Animator.StringToHash(AnimatorIndex);
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
            Animator.SetTrigger(nameHash);
            Animator.SetInteger(indexHash, AnimIndex);
            PlayAnimationClientRpc(AnimIndex);
            if(await ShouldExit(UniTask.WaitForSeconds(WaitTime, cancellationToken: ct)))
                return;

            if(Interruption == InterruptionSource.None) {
                Completed();
            }
        }

        [ClientRpc]
        private void PlayAnimationClientRpc(int animIndex)
        {
            if(IsServer)
                return;

            Animator.SetTrigger(nameHash);
            Animator.SetInteger(indexHash, animIndex);
        }
    }

}
