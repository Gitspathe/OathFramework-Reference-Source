using Cysharp.Threading.Tasks;
using System;
using Sirenix.OdinInspector;
using OathFramework.AbilitySystem;
using OathFramework.Core;
using OathFramework.Data;
using OathFramework.Data.EntityStates;
using OathFramework.Data.StatParams;
using OathFramework.Effects;
using OathFramework.EntitySystem.Actions;
using OathFramework.EntitySystem.Death;
using OathFramework.EntitySystem.Players;
using OathFramework.EntitySystem.States;
using OathFramework.Networking;
using OathFramework.PerkSystem;
using OathFramework.Persistence;
using OathFramework.Pooling;
using OathFramework.Utility;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    [RequireComponent(typeof(StateHandler), typeof(EntityActionHandler), typeof(EntityTargeting)), 
     RequireComponent(typeof(StaggerHandler), typeof(EntityDeathHandler), typeof(FlagHandler)), 
     RequireComponent(typeof(SpEventHandler))]
    public partial class Entity : NetLoopComponent, 
        ILoopUpdate, IPoolableComponent, IPersistableComponent,
        IEntity
    {
        [field: SerializeField] public EntityTeams Team { get; private set; }

        [SerializeField] private bool resetStatsOnPoolReturn = true;
        [SerializeField] private EntityParams paramsID;

        private bool isInit;
        private bool syncHealth = true;
        private float timeSinceSpawn;
        private float timeSinceStaminaUse;
        private float staminaAccumulator;
        private AccessToken callbackAccessToken;
        
        private NetworkVariable<uint> netHealth = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner,
            value: 1u
        );
        private NetworkVariable<ushort> netStamina = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner,
            value: 1
        );

        // STATUS
        public EntityParams Params { get; private set; }
        public Stats BaseStats     => Params.BaseStats;
        
        [field: SerializeField, HideInEditorMode] 
        public Stats CurStats      { get; private set; }
        
        // MODULES
        public IEntityControllerBase Controller { get; private set; }
        public StateHandler States              { get; private set; }
        public PerkHandler Perks                { get; private set; }
        public FlagHandler Flags                { get; private set; }
        public SpEventHandler SpEvents          { get; private set; }
        public AbilityHandler Abilities         { get; private set; }
        public EntityActionHandler Actions      { get; private set; }
        public EntityDeathHandler Death         { get; private set; }
        public StaggerHandler Stagger           { get; private set; }
        public EntityTargeting Targeting        { get; private set; }
        public EntityAnimation Animation        { get; private set; }
        public DamageRecorder DamageRecorder    { get; private set; }
        public EntityModel EntityModel          { get; set; }
        
        // MISC
        public ModelSocketHandler Sockets    => EntityModel.Sockets;
        public EntityCallbacks Callbacks     { get; } = new();
        public PoolableGameObject PoolableGO { get; set; }
        public NetworkTransform NetTransform { get; private set; }
        
        
        // FLAGS
        public bool IsPlayer                 { get; private set; }
        public bool IsDead                   { get; private set; }
        public bool IsDummy                  { get; private set; }
        public bool PlayedHitEffectThisFrame { get; set; }
        public bool NetInitComplete          { get; private set; }
        public bool ModelSpawned             { get; private set; }
        
        // CACHE
        public Transform CTransform          { get; private set; }
        [NonSerialized] public int Index;

        private void Awake()
        {
            Callbacks.RegisterInitCallbacks(this);
            callbackAccessToken = Callbacks.Access.GenerateAccessToken();
            CTransform          = transform;
            NetTransform        = GetComponent<NetworkTransform>();
            States              = GetComponent<StateHandler>();
            Perks               = GetComponent<PerkHandler>();
            Flags               = GetComponent<FlagHandler>();
            SpEvents            = GetComponent<SpEventHandler>();
            Abilities           = GetComponent<AbilityHandler>();
            Actions             = GetComponent<EntityActionHandler>();
            Stagger             = GetComponent<StaggerHandler>();
            Death               = GetComponent<EntityDeathHandler>();
            Controller          = GetComponent<IEntityControllerBase>();
            Targeting           = GetComponent<EntityTargeting>();
            Animation           = GetComponent<EntityAnimation>();
            DamageRecorder      = GetComponent<DamageRecorder>();
            IsDummy             = GetComponent<DummyPlayer>() != null;
            IsPlayer            = Controller is PlayerController;
            Death.SetCallbackToken(callbackAccessToken);
            Stagger.SetAccessToken(callbackAccessToken);
            if(IsDummy)
                return;

            // If the game is NOT initialized, this must be a pooled entity.
            // So defer InitParams to when it is retrieved from the pool.
            if(Game.Initialized) {
                InitParams();
            }
        }

        // ReSharper disable once PossibleNullReferenceException
        public void InitParams()
        {
            EntityParams @params = null;
            if(paramsID == null || string.IsNullOrEmpty(paramsID.LookupKey) || !EntityManager.TryGetParams(paramsID.LookupKey, out @params)) {
                Debug.LogError($"Entity Params on {gameObject.name} is invalid. Disabling.");
                gameObject.SetActive(false);
            }
#if !UNITY_EDITOR
            paramsID = null;
#endif
            Params   = @params;
            Params.Initialize();
            BaseStats.CopyTo(CurStats);
            CurStats.FreeSerializedParams();
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(IsDummy)
                return;

            IsDead = false;
            if(!isInit) {
                Callbacks.Access.OnInitialize(callbackAccessToken, this);
            }
            isInit = true;
            if(IsOwner) {
                netHealth.Value  = CurStats.maxHealth;
                netStamina.Value = CurStats.maxStamina;
                if(!IsPlayer) {
                    OnNetInitializationComplete();
                }
                return;
            }
            CurStats.health  = netHealth.Value;
            CurStats.stamina = netStamina.Value;
            
            netHealth.OnValueChanged += OnNetHealthChanged;
            if(CurStats.health == 0u) {
                _ = DelayedSyncDeath();
            }
            if(!IsPlayer) {
                OnNetInitializationComplete();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            netHealth.OnValueChanged -= OnNetHealthChanged;
        }

        void ILoopUpdate.LoopUpdate()
        {
            if(IsDummy || IsDead)
                return;

            PlayedHitEffectThisFrame = false;
            timeSinceSpawn += Time.deltaTime;
            if(timeSinceSpawn > 3.0f) {
                syncHealth = false;
            }
            
            HandleStamina();
            if(!IsOwner && IsPlayer) {
                // Non-owner needs better health sync, if entity is a player.
                CurStats.health  = netHealth.Value;
                CurStats.stamina = netStamina.Value;
            }
        }

        private void HandleStamina()
        {
            // Stamina regen.
            timeSinceStaminaUse += Time.deltaTime;
            if(CurStats.stamina == CurStats.maxStamina || timeSinceStaminaUse < CurStats.GetParam(StaminaRegenDelay.Instance))
                return;

            staminaAccumulator += CurStats.GetParam(StaminaRegen.Instance) * Time.deltaTime;
            ushort toAdd = (ushort)staminaAccumulator;
            if(toAdd == 0)
                return;

            CurStats.stamina    = (ushort)Mathf.Clamp((float)CurStats.stamina + toAdd, 0.0f, CurStats.maxStamina);
            staminaAccumulator -= toAdd;
        }

        private async UniTask DelayedSyncDeath()
        {
            if(await UniTask.Yield(cancellationToken: Game.ResetCancellation.Token).SuppressCancellationThrow())
                return;
            
            DieInternal(DamageValue.SyncDeath);
        }
        
        private void OnNetHealthChanged(uint previous, uint current)
        {
            if(!syncHealth)
                return;
            
            CurStats.health = current;
            syncHealth      = false;
        }

        public void SyncNetVars()
        {
            if(!IsOwner)
                return;
            
            netHealth.Value  = CurStats.health;
            netStamina.Value = CurStats.stamina;
            SyncNotOwnerRpc(CurStats.health, CurStats.stamina);
        }

        public void InitStatsForLateClient()
        {
            if(IsOwner)
                return;

            CurStats.health  = netHealth.Value;
            CurStats.stamina = netStamina.Value;
        }

        public void OnNetInitializationComplete()
        {
            NetInitComplete = true;
        }

        public void OnModelSpawned()
        {
            ModelSpawned = true;
        }
        
        public async UniTask WaitForNetInitialization()
        {
            while(!NetInitComplete)
                await UniTask.Yield();
        }

        public async UniTask<bool> WaitForModel()
        {
            while(!ModelSpawned) {
                if(IsDead || destroyCancellationToken.IsCancellationRequested)
                    return false;
                
                await UniTask.Yield();
            }
            return true;
        }

        private async UniTask PersistenceSyncTask(uint health, ushort stamina)
        {
            await WaitForNetInitialization();
            CurStats.health  = health;
            CurStats.stamina = stamina;
            netHealth.Value  = health;
            netStamina.Value = stamina;
            SyncNetVars();
        }

        public void ParallelUpdate()
        {
            Callbacks.Access.OnThreadedUpdate(callbackAccessToken);
        }

        public void DecrementStamina(ushort amt)
        {
            if(IsDummy)
                return;

            DecrementStaminaInternal(false, amt);
            if(IsServer)
                return;
            
            DecrementStaminaServerRpc(amt);
        }
        
        public void IncrementStamina(ushort amt, bool removeDelay = false)
        {
            if(IsDummy)
                return;

            IncrementStaminaInternal(false, amt, removeDelay);
            if(IsServer)
                return;
            
            IncrementStaminaServerRpc(amt, removeDelay);
        }

        public void Heal(HealValue val, HitEffectValue? effect = null)
        {
            if(IsDummy)
                return;

            HealInternal(false, val, effect);
            if(IsServer) {
                if(effect != null) {
                    HealWithEffectClientRpc(val, effect.Value);
                } else {
                    HealClientRpc(val);
                }
                return;
            }
            if(!IsClient)
                return;
            
            if(effect != null) {
                HealWithEffectServerRpc(val, effect.Value);
            } else {
                HealServerRpc(val);
            }
        }

        public void Damage(DamageValue val, HitEffectValue? effect = null)
        {
            if(IsDummy)
                return;
            
            bool hasInstigator = val.GetInstigator(out Entity instigator);
            if(hasInstigator && !val.BypassInstigatorCallbacks) {
                instigator.OnPreDealDamage(this, false, ref val);
            }
            
            /*
             TODO: Currently, client prediction of instant damage (i.e raycasts) is not possible due to how RPCs are set up.
               
             It would cause an issue where the instigator would be notified multiple times, unlike with EffectBoxes where the client
             Handles collision and sending RPCs from their game instance. There is no band-aid fix, since EffectBoxes need to still work as-is.
            
             I probably need to plot out exactly how these work, to support all possible use cases. 
            */
            
            DamageInternal(false, val, effect);
            if(IsServer) {
                if(effect != null) {
                    DamageWithEffectClientRpc(val, effect.Value);
                } else {
                    DamageClientRpc(val);
                }
                return;
            }
            if(!IsClient)
                return;

            if(effect != null) {
                DamageWithEffectServerRpc(val, effect.Value);
            } else {
                DamageServerRpc(val);
            }
        }

        public void TestDamage(ref DamageValue val)
        {
            bool hasInstigator = val.GetInstigator(out Entity instigator);
            if(hasInstigator && !val.BypassInstigatorCallbacks) {
                instigator.OnPreDealDamage(this, false, ref val);
            }
            TestDamageInternal(ref val);
        }

        public void Die(DamageValue lastDamageVal)
        {
            if(IsDummy)
                return;

            if(!DieInternal(lastDamageVal))
                return; // Survived via callback. Don't send RPC.
            
            if(IsServer) {
                DieClientRpc(lastDamageVal);
                return;
            }
            if(!IsClient)
                return;

            DieServerRpc(lastDamageVal);
        }

        public void DieCommand()
        {
            if(IsDummy)
                return;

            if(!IsServer) {
                DieCommandServerRpc();
                return;
            }
            DieInternal(DamageValue.DieCommand);
            DieCommandClientRpc();
        }

        public void OnScoredKill(IEntity other, in DamageValue lastDamageVal, float ratio)
        {
            if(IsDead || IsDummy)
                return;
            
            Callbacks.Access.OnScoredKill(callbackAccessToken, other, in lastDamageVal, ratio);
        }

        private void OnPreDealDamage(Entity target, bool isTest, ref DamageValue damageValue)
        {
            if(IsDead || IsDummy)
                return;
            
            Callbacks.Access.OnPreDealDamage(callbackAccessToken, target, isTest, ref damageValue);
        }

        private void OnDealtDamage(Entity target, bool fromRpc, in DamageValue damageValue)
        {
            if(IsDead || IsDummy)
                return;
            
            Callbacks.Access.OnDealtDamage(callbackAccessToken, target, fromRpc, in damageValue);
        }

        private void DecrementStaminaInternal(bool isRpc, ushort amt)
        {
            if(amt == 0u || IsDead || IsDummy)
                return;
            
            timeSinceStaminaUse = 0.0f;
            CurStats.stamina    = (ushort)Mathf.Clamp((float)CurStats.stamina - amt, 0.0f, CurStats.maxStamina);
            if(IsOwner) {
                netStamina.Value = CurStats.stamina;
            }
            if(!IsServer)
                return;
            
            DecrementStaminaClientRpc(amt);
        }

        private void IncrementStaminaInternal(bool isRpc, ushort amt, bool removeDelay)
        {
            if((amt == 0u && !removeDelay) || IsDead || IsDummy)
                return;

            if(removeDelay) {
                timeSinceStaminaUse = 9999.0f;
            }
            CurStats.stamina = (ushort)Mathf.Clamp((float)CurStats.stamina + amt, 0.0f, CurStats.maxStamina);
            if(IsOwner) {
                netStamina.Value = CurStats.stamina;
            }
            if(!IsServer)
                return;
            
            IncrementStaminaClientRpc(amt, removeDelay);
        }

        private void TriggerSpEffects(QList<EntitySpEvent> spEvents, bool damage, bool auxOnly)
        {
            if(spEvents == null)
                return;
            
            for(int i = 0; i < spEvents.Count; i++) {
                if(!damage && !spEvents.Array[i].Event.ApplyOnNoDamage)
                    continue;
                
                SpEvents.ApplyEvent(spEvents.Array[i], auxOnly);
            }
        }

        private void DamageInternal(bool isRpc, DamageValue val, HitEffectValue? effect = null)
        {
            if((!isRpc && IsDead) || IsDummy)
                return;
            if(Game.ConsoleEnabled && States.HasState(GodModeState.Instance))
                return;

            bool hasInstigator = val.GetInstigator(out Entity instigator);
            if(effect != null) {
                HitEffectValue effectVal = effect.Value;
                Params.TryGetEffectColorOverride(HitSurfaceMaterial.Flesh, out Color? col);
                HitEffectManager.CreateEffect(CTransform, !PlayedHitEffectThisFrame, in val, in effectVal, col);
            }
            if(val.Amount == 0u) {
                TriggerSpEffects(val.SpEvents, false, !IsOwner);
                return;
            }
            
            Callbacks.Access.OnPreTakeDamage(callbackAccessToken, isRpc, false, ref val);
            if(val.Amount == 0u) {
                TriggerSpEffects(val.SpEvents, false, !IsOwner);
                return;
            }

            CurStats.health = (uint)Mathf.Clamp((float)CurStats.health - val.Amount, 0.0f, CurStats.maxHealth);
            if(IsOwner) {
                netHealth.Value = CurStats.health;
            }
            TriggerSpEffects(val.SpEvents, true, !IsOwner);
            Callbacks.Access.OnTakeDamage(callbackAccessToken, isRpc, in val);
            if(hasInstigator && !val.BypassInstigatorCallbacks) {
                instigator.OnDealtDamage(this, isRpc, in val);
            }
            if(!IsOwner || CurStats.health > 0u || IsDead)
                return;

            Die(val);
        }

        private void TestDamageInternal(ref DamageValue val)
        {
            if(Game.ConsoleEnabled && States.HasState(GodModeState.Instance))
                return;
            if(val.Amount == 0u)
                return;
            
            Callbacks.Access.OnPreTakeDamage(callbackAccessToken, false, false, ref val);
        }

        private void HealInternal(bool isRpc, HealValue val, HitEffectValue? effect = null)
        {
            if(val.Amount == 0u || IsDead || IsDummy)
                return;

            if(effect != null) {
                HitEffectValue effectVal = effect.Value;
                Transform t              = EntityModel.Sockets.GetModelSpot(ModelSpotLookup.Human.Torso).Transform;
                HitEffectManager.CreateEffect(t, !PlayedHitEffectThisFrame, in val, in effectVal);
            }
            if(val.Amount == 0u) {
                TriggerSpEffects(val.SpEvents, false, !IsOwner);
                return;
            }
            
            Callbacks.Access.OnPreHeal(callbackAccessToken, isRpc, ref val);
            if(val.Amount <= 0u) {
                TriggerSpEffects(val.SpEvents, false, !IsOwner);
                return;
            }
            
            CurStats.health = (uint)Mathf.Clamp((float)CurStats.health + val.Amount, 0.0f, CurStats.maxHealth);
            if(IsOwner) {
                netHealth.Value = CurStats.health;
            }
            TriggerSpEffects(val.SpEvents, true, !IsOwner);
            Callbacks.Access.OnHeal(callbackAccessToken, isRpc, in val);
        }

        private bool DieInternal(in DamageValue lastDamageVal)
        {
            if(IsDead || IsDummy)
                return false;

            if(!lastDamageVal.IsUnavoidableDeath && IsOwner && Callbacks.Access.OnPreDie(callbackAccessToken, lastDamageVal)) {
                CurStats.health = 1u;
                return false;
            }
            Death.TriggerDeath(in lastDamageVal);
            Targeting.enabled    = false;
            Animation.enabled    = false;
            Stagger.enabled      = false;
            NetTransform.enabled = false;
            IsDead               = true;
            if(IsOwner) {
                netHealth.Value = 0u;
            }
            return true;
        }
        
        public bool TryGetEffectColorOverride(HitSurfaceMaterial material, out Color? color)
        {
            color = null;
            return Params?.TryGetEffectColorOverride(material, out color) ?? false;
        }
        
        void IPoolableComponent.OnRetrieve()
        {
            // Params are initialized when retrieved for the first time.
            if(ReferenceEquals(Params, null)) {
                InitParams();
                if(resetStatsOnPoolReturn) {
                    BaseStats.CopyTo(CurStats);
                }
            }
            
            Targeting.enabled    = true;
            Animation.enabled    = true;
            Stagger.enabled      = true;
            IsDead               = false;
            NetTransform.enabled = NetGame.GameType == GameType.Multiplayer;
            CurStats.health      = CurStats.maxHealth;
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            IsDead          = false;
            CurStats.health = CurStats.maxHealth;
            if((Game.Initialized || !initialization) && resetStatsOnPoolReturn) {
                BaseStats.CopyTo(CurStats);
            }
            Stagger.ResetPoise();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        private void DieCommandServerRpc(RpcParams rpcParams = default)
        {
            // Prevent every player from being able to send kill messages. Only the owner can kill themselves.
            if(OwnerClientId != rpcParams.Receive.SenderClientId) {
                if(!Game.ExtendedDebug)
                    return;
                
                string msg = $"Got kill request from a player ({rpcParams.Receive.SenderClientId}) who doesn't own this GameObject: {gameObject.name}";
                Debug.LogError(msg);
                return;
            }
            DieInternal(DamageValue.DieCommand);
            DieCommandClientRpc();
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        private void DieCommandClientRpc()
        {
            DieInternal(DamageValue.DieCommand);
        }
        
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        private void DecrementStaminaServerRpc(ushort amt, RpcParams rpcParams = default)
        {
            DecrementStaminaInternal(true, amt);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void DecrementStaminaClientRpc(ushort amt)
        {
            if(IsOwner)
                return;

            DecrementStaminaInternal(true, amt);
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        private void IncrementStaminaServerRpc(ushort amt, bool removeDelay, RpcParams rpcParams = default)
        {
            IncrementStaminaInternal(true, amt, removeDelay);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void IncrementStaminaClientRpc(ushort amt, bool removeDelay)
        {
            if(IsOwner)
                return;

            IncrementStaminaInternal(true, amt, removeDelay);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        private void DamageServerRpc(DamageValue val, RpcParams rpcParams = default)
        {
            try {
                DamageInternal(true, val);
                if(rpcParams.Receive.SenderClientId == OwnerClientId) {
                    DamageNotOwnerRpc(val);
                } else {
                    DamageClientRpc(val);
                }
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void DamageClientRpc(DamageValue val)
        {
            try {
                if(IsOwner)
                    return;
                
                DamageInternal(true, val);
            } finally {
                val.ReturnResources();
            }
        }
        
        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        private void DamageNotOwnerRpc(DamageValue val)
        {
            try {
                if(IsServer)
                    return;
                if(val.GetInstigator(out Entity instigator) && instigator.IsOwner)
                    return;

                DamageInternal(true, val);
            } finally {
                val.ReturnResources();
            }
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        private void DamageWithEffectServerRpc(DamageValue val, HitEffectValue effect, RpcParams rpcParams = default)
        {
            try {
                DamageInternal(true, val, effect);
                if(rpcParams.Receive.SenderClientId == OwnerClientId) {
                    DamageWithEffectNotOwnerRpc(val, effect);
                } else {
                    DamageWithEffectClientRpc(val, effect);
                }
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void DamageWithEffectClientRpc(DamageValue val, HitEffectValue effect)
        {
            try {
                if(IsOwner)
                    return;
                
                DamageInternal(true, val, effect);
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        private void DamageWithEffectNotOwnerRpc(DamageValue val, HitEffectValue effect)
        {
            try {
                if(IsServer || val.GetInstigator(out Entity instigator) && instigator.IsOwner)
                    return;

                DamageInternal(true, val, effect);
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        private void HealServerRpc(HealValue val, RpcParams rpcParams = default)
        {
            try {
                HealInternal(true, val);
                if(rpcParams.Receive.SenderClientId == OwnerClientId) {
                    HealNotOwnerRpc(val);
                } else {
                    HealClientRpc(val);
                }
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void HealClientRpc(HealValue val)
        {
            try {
                if(val.GetInstigator(out Entity instigator) && instigator.IsOwner)
                    return;

                HealInternal(true, val);
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        private void HealNotOwnerRpc(HealValue val)
        {
            try {
                if(IsServer || val.GetInstigator(out Entity instigator) && instigator.IsOwner)
                    return;

                HealInternal(true, val);
            } finally {
                val.ReturnResources();
            }
        }
        
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        private void HealWithEffectServerRpc(HealValue val, HitEffectValue effect, RpcParams rpcParams = default)
        {
            try {
                HealInternal(true, val, effect);
                if(rpcParams.Receive.SenderClientId == OwnerClientId) {
                    HealWithEffectNotOwnerRpc(val, effect);
                } else {
                    HealWithEffectClientRpc(val, effect);
                }
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void HealWithEffectClientRpc(HealValue val, HitEffectValue effect)
        {
            try {
                if(val.GetInstigator(out Entity instigator) && instigator.IsOwner)
                    return;

                HealInternal(true, val, effect);
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        private void HealWithEffectNotOwnerRpc(HealValue val, HitEffectValue effect)
        {
            try {
                if(IsServer || val.GetInstigator(out Entity instigator) && instigator.IsOwner)
                    return;

                HealInternal(true, val, effect);
            } finally {
                val.ReturnResources();
            }
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
        private void DieServerRpc(DamageValue lastDamageVal)
        {
            DieInternal(in lastDamageVal);
            DieNotOwnerRpc(lastDamageVal);
        }
        
        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        private void DieClientRpc(DamageValue lastDamageVal)
        {
            DieInternal(in lastDamageVal);
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Reliable)]
        private void DieNotOwnerRpc(DamageValue lastDamageVal)
        {
            if(IsServer)
                return;
            
            DieInternal(in lastDamageVal);
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Reliable)]
        private void SyncNotOwnerRpc(uint health, ushort stamina)
        {
            CurStats.health  = health;
            CurStats.stamina = stamina;
        }

        [Rpc(SendTo.Owner, Delivery = RpcDelivery.Reliable)]
        private void SyncPersistenceOwnerRpc(uint health, ushort stamina)
        {
            _ = PersistenceSyncTask(health, stamina);
        }
    }

}
