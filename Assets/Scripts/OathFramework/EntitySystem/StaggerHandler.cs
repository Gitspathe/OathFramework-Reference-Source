using OathFramework.Core;
using OathFramework.EntitySystem.Actions;
using OathFramework.Persistence;
using OathFramework.Utility;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public partial class StaggerHandler : NetLoopComponent,
        ILoopUpdate, IPersistableComponent, IEntityInitCallback, 
        IEntityTakeDamageCallback
    {
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;
        
        public Entity Entity                     { get; private set; }
        public Stats CurStats                    { get; private set; }
        public StaggerStrength Immunity          => CurStats.staggerRes;
        public uint Poise                        => CurStats.poise;
        public float PoiseReset                  => CurStats.poiseReset;
        public bool IsStaggered                  => StaggerTime > 0.0f;
        public bool IsUncontrollable             => UncontrollableTime > 0.0f;
        public EntityActionHandler ActionHandler => Entity.Actions;
        
        [field: SerializeField] public Stagger LowStaggerAction      { get; private set; }
        [field: SerializeField] public Stagger ModerateStaggerAction { get; private set; }
        [field: SerializeField] public Stagger HighStaggerAction     { get; private set; }

        public float CurrentPoise       { get; private set; }
        public float CurrentPoiseReset  { get; private set; }
        public float StaggerTime        { get; private set; }
        public float UncontrollableTime { get; private set; }
        
        private AccessToken accessToken;

        uint ILockableOrderedListElement.Order => 10_000;

        private void Awake()
        {
            Entity   = GetComponent<Entity>();
            CurStats = Entity.CurStats;
        }

        public void SetAccessToken(AccessToken token)
        {
            accessToken = token;
        }

        public void ResetPoise()
        {
            CurrentPoise      = Poise;
            CurrentPoiseReset = 0.0f;
        }
        
        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityTakeDamageCallback)this);
            ResetPoise();
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            StaggerTime        -= Time.deltaTime;
            UncontrollableTime -= Time.deltaTime;
            CurrentPoiseReset  += Time.deltaTime;
            if(CurrentPoiseReset >= PoiseReset) {
                // TODO: Stop using magic number (0.2)
                CurrentPoise = Mathf.Clamp(CurrentPoise + (Poise * 0.2f * Time.deltaTime), 0.0f, Poise);
            }
        }

        void IEntityTakeDamageCallback.OnDamage(Entity entity, bool fromRpc, in DamageValue val)
        {
            if(entity.IsDead || val.StaggerStrength == StaggerStrength.None || IsStaggered || (int)Immunity >= (int)val.StaggerStrength)
                return;

            CurrentPoiseReset = 0.0f;
            CurrentPoise      = Mathf.Clamp(CurrentPoise - val.StaggerAmount, 0.0f, Poise);
            if(CurrentPoise > 0.0f || !IsOwner)
                return;

            val.GetInstigator(out Entity instigator);
            DoStagger(false, val.StaggerStrength, instigator);
        }

        private Stagger GetStaggerAction(StaggerStrength strength)
        {
            int curLvl = (int)strength;
            while(true) {
                if(curLvl <= 0)
                    return null;

                switch((StaggerStrength)curLvl) {
                    case StaggerStrength.Low:
                        if(!ReferenceEquals(LowStaggerAction, null))
                            return LowStaggerAction;
                        break;
                    case StaggerStrength.Medium:
                        if(!ReferenceEquals(ModerateStaggerAction, null))
                            return ModerateStaggerAction;
                        break;
                    case StaggerStrength.High:
                        if(!ReferenceEquals(HighStaggerAction, null))
                            return HighStaggerAction;
                        break;
                    
                    case StaggerStrength.None:
                    default:
                        return null;
                }
                curLvl--;
            }
        }

        private void DoStagger(bool fromRpc, StaggerStrength strength, Entity instigator)
        {
            Stagger action = GetStaggerAction(strength);
            if(ReferenceEquals(action, null))
                return;

            StaggerTime        = action.WaitTime;
            UncontrollableTime = action.UncontrollableTime;
            ResetPoise();
            action.SetParams(StaggerTime, instigator);
            ActionHandler.InvokeAction(action);
            Entity.Callbacks.Access.OnStagger(accessToken, strength, instigator);
            if(fromRpc)
                return;
            
            if(!IsServer) {
                DoStaggerServerRpc(strength, instigator);
            } else {
                DoStaggerClientRpc(strength, instigator);
            }
        }

        [Rpc(SendTo.Server)]
        private void DoStaggerServerRpc(StaggerStrength strength, NetworkBehaviourReference instigatorRef)
        {
            instigatorRef.TryGet(out Entity instigator);
            DoStagger(true, strength, instigator);
            DoStaggerClientRpc(strength, instigatorRef);
        }

        [Rpc(SendTo.NotServer)]
        private void DoStaggerClientRpc(StaggerStrength strength, NetworkBehaviourReference instigatorRef)
        {
            if(IsOwner)
                return;
            
            instigatorRef.TryGet(out Entity instigator);
            DoStagger(true, strength, instigator);
        }
    }

}
