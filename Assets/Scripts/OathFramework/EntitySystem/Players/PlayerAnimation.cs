using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem.Players
{

    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimation : EntityAnimation
    {
        private NetworkVariable<Vector3> movementVecNetVar = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );
        
        private PlayerController PlayerController => (PlayerController)Controller;

        public PlayerModel Model => PlayerController.PlayerModel;

        public override void LoopUpdate()
        {
            base.LoopUpdate();
            if(!PlayerController.Entity.NetInitComplete)
                return;
            
            if(!IsOwner) { 
                Model.SetMovementAnim(movementVecNetVar.Value);
            }
        }

        public void UpdateMovement(Vector3 movementVec)
        {
            if(!IsOwner)
                return;

            movementVecNetVar.Value = movementVec;
            Model.SetMovementAnim(movementVec);
        }

        public void ReloadAmmo()
        {
            if(!IsOwner)
                return;
            
            PlayerController.Equipment.Reload();
        }

        public void EndReload()
        {
            if(!IsOwner)
                return;
            
            PlayerController.Equipment.EndReload();
        }

        public void SetAiming(bool val)
        {
            if(!IsOwner)
                return;
            
            Model.SetAimAnim(val);
        }

        public void PlayThrow()
        {
            if(!IsOwner)
                return;
            
            Model.PlayThrow();
        }
        
        public override void OnRetrieve() { }
        public override void OnReturn(bool initialization) { }
    }

}
