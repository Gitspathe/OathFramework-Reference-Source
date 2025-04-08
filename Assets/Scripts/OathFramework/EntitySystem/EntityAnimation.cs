using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.Pooling;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    [RequireComponent(typeof(IEntityController))]
    public abstract class EntityAnimation : NetLoopComponent, ILoopUpdate, IPoolableComponent
    {
        [SerializeField] private bool cullOffscreen;
        [SerializeField] private TriggerResets[] triggerResets;
        
        private Dictionary<string, int[]> triggerResetsDict = new();
        
        protected IEntityControllerBase Controller;
        protected int AnimMoveSpeedMultParamHash;
        
        [field: SerializeField] public string AnimSpeedMultParam { get; private set; } = "MoveSpeedMult";
        
        public PoolableGameObject PoolableGO { get; set; }
        
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;
        
        protected virtual void Awake()
        {
            Controller                 = GetComponent<IEntityControllerBase>();
            AnimMoveSpeedMultParamHash = Animator.StringToHash(AnimSpeedMultParam);
            foreach(TriggerResets reset in triggerResets) {
                int[] arr = new int[reset.Resets.Length];
                for(int i = 0; i < arr.Length; i++) {
                    arr[i] = Animator.StringToHash(reset.Resets[i]);
                }
                triggerResetsDict.Add(reset.ID, arr);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            UpdateCullParam();
        }

        public void UpdateCullParam()
        {
            if(!cullOffscreen && IsOwner && NetGame.GameType != GameType.SinglePlayer)
                return;
            
            EntityModel model = Controller.Entity.EntityModel;
            if(ReferenceEquals(model, null))
                return;
            
            model.Animator.cullingMode = AnimatorCullingMode.CullCompletely;
            model.SetUpdateOffscreen(false);
        }

        public virtual void LoopUpdate()
        {
            if(!Controller.Entity.NetInitComplete || ReferenceEquals(Controller.Entity.EntityModel, null))
                return;

            EntityModel model = Controller.Entity.EntityModel;
            float speedMult = Controller.Entity.CurStats.speed / Controller.Entity.BaseStats.speed;
            model.Animator.SetFloat(AnimMoveSpeedMultParamHash, speedMult);
        }
        
        public abstract void OnRetrieve();
        public abstract void OnReturn(bool initialization);

        public void ResetTriggers(string id)
        {
            Entity e = Controller.Entity;
            if(!e.NetInitComplete || ReferenceEquals(e.EntityModel, null) || !triggerResetsDict.TryGetValue(id, out int[] arr))
                return;

            foreach(int hash in arr) {
                e.EntityModel.Animator.ResetTrigger(hash);
            }
        }
    }

    [System.Serializable]
    public class TriggerResets
    {
        [field: SerializeField] public string ID       { get; private set; }
        [field: SerializeField] public string[] Resets { get; private set; }
    }
}
