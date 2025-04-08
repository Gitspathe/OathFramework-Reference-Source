using OathFramework.AbilitySystem;
using OathFramework.Core;
using OathFramework.EquipmentSystem;
using OathFramework.UI;
using OathFramework.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OathFramework.EntitySystem.Players
{

    [RequireComponent(typeof(PlayerController))]
    public class PlayerHandler : NetLoopComponent, 
        ILoopLateUpdate, IEntityInitCallback, IEntityDieCallback
    {
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;
        
        [SerializeField] private InputActionReference fireAction;
        [SerializeField] private InputActionReference reloadAction;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference dodgeAction;
        [SerializeField] private InputActionReference quickHealAction;

        [Space(5)]

        [SerializeField] private InputActionReference weaponSwapPrimaryAction;
        [SerializeField] private InputActionReference weaponSwapSecondaryAction;
        [SerializeField] private InputActionReference weaponSwapAction;

        [Space(5)]
        
        [SerializeField] private InputActionReference useAbility1Action;
        [SerializeField] private InputActionReference useAbility2Action;

        public InputAction FireAction                => fireAction.action;
        public InputAction ReloadAction              => reloadAction.action;
        public InputAction MoveAction                => moveAction.action;
        public InputAction LookAction                => lookAction.action;
        public InputAction DodgeAction               => dodgeAction.action;
        public InputAction QuickHealAction           => quickHealAction.action;
        public InputAction WeaponSwapPrimaryAction   => weaponSwapPrimaryAction.action;
        public InputAction WeaponSwapSecondaryAction => weaponSwapSecondaryAction.action;
        public InputAction WeaponSwapAction          => weaponSwapAction.action;
        public InputAction UseAbility1Action         => useAbility1Action.action;
        public InputAction UseAbility2Action         => useAbility2Action.action;
        
        public Command DodgeCommand      { get; private set; }
        public Command ReloadCommand     { get; private set; }
        public Command AttackCommand     { get; private set; }
        public Command SwapCommand       { get; private set; }
        public Command QuickHealCommand  { get; private set; }
        public Command UseAbilityCommand { get; private set; }

        private CommandQueue commandQueue = new();
        private PlayerController controller;
        private PlayerAbilityHandler abilityHandler;
        private Entity entity;

        private byte cmdAbilityIndex;
        private Vector3 cmdDodgeDir;
        private SlotCycleDirection? cmdCycleDir;
        private EquipmentSlot? cmdEquipSlot;

        public EntityEquipment Equipment => controller.Equipment;
        
        uint ILockableOrderedListElement.Order => 1_000;

        private void Awake()
        {
            controller     = GetComponent<PlayerController>();
            entity         = GetComponent<Entity>();
            abilityHandler = GetComponent<PlayerAbilityHandler>();
        }

        private void Start()
        {
            DodgeCommand = new Command(
                CommandIDs.Dodge,
                CommandOrders.Dodge,
                () => controller.TryDoDodge(cmdDodgeDir),
                controller.DodgeBlocked,
                36.0f / 60.0f
            );
            ReloadCommand = new Command(
                CommandIDs.Reload,
                CommandOrders.Reload,
                () => Equipment.BeginReload(),
                Equipment.ReloadBlocked,
                40.0f / 60.0f
            );
            AttackCommand = new Command(
                CommandIDs.UseEquippable,
                CommandOrders.UseEquippable,
                AttackOrReload,
                Equipment.HandlerUseBlocked,
                0.0f
            );
            SwapCommand = new Command(
                CommandIDs.Swap,
                CommandOrders.Swap,
                SwapAction,
                Equipment.SwapBlocked,
                25.0f / 60.0f
            );
            QuickHealCommand = new Command(
                CommandIDs.QuickHeal,
                CommandOrders.QuickHeal,
                QuickHeal,
                controller.ItemUseBlocked, 
                42.0f / 60.0f
            );
            UseAbilityCommand = new Command(
                CommandIDs.UseAbility,
                CommandOrders.UseAbility,
                UseAbility,
                abilityHandler.UseAbilityBlocked,
                45.0f / 60.0f
            );
            
            commandQueue.RegisterActions(
                DodgeCommand, ReloadCommand, AttackCommand, SwapCommand, QuickHealCommand, UseAbilityCommand
            );
            return;

            void SwapAction()
            {
                if(cmdCycleDir != null) {
                    Equipment.SwapEquipmentSlot(cmdCycleDir.Value);
                    return;
                }
                if(cmdEquipSlot.HasValue) {
                    Equipment.SwapEquipmentSlot(cmdEquipSlot.Value, false);
                }
            }

            void UseAbility()
            {
                abilityHandler.UseAbility(cmdAbilityIndex);
            }
        }

        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityDieCallback)this);
        }

        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            controller.ControlsSetMovement(Vector3.zero);
            commandQueue.Clear();
        }

        public void LoopLateUpdate()
        {
            if(!IsOwner || entity.IsDead)
                return;
            
            commandQueue.ProcessQueue(Time.deltaTime);
            HandleInput();
        }

        private void HandleInput()
        {
            if(Game.IsQuitting || GameUI.Instance.PlayerControlBlocked) {
                controller.ControlsSetMovement(Vector3.zero);
                return;
            }
            switch (GameControls.ControlScheme) {
                case ControlSchemes.Keyboard:
                    HandleInputKeyboard(); 
                    break;
                case ControlSchemes.Touch:
                    HandleInputTouch(); 
                    break;
                case ControlSchemes.Gamepad:
                    HandleInputGamepad(); 
                    break;
                
                case ControlSchemes.None:
                default:
                    Debug.LogError("No control scheme!");
                    return;
            }
        }
        
        private void HandleInputKeyboard()
        {
            Vector2 move = MoveAction.ReadValue<Vector2>();
            controller.ControlsSetMovement(move);
            
            if(DodgeAction.WasPressedThisFrame()) {
                cmdDodgeDir = new Vector3(move.y, 0.0f, -move.x);
                commandQueue.Enqueue(CommandIDs.Dodge);
            }
            if(QuickHealAction.WasPressedThisFrame()) {
                commandQueue.Enqueue(CommandIDs.QuickHeal);
            }
            if(ReloadAction.WasPressedThisFrame()) {
                commandQueue.Enqueue(CommandIDs.Reload);
            }
            if(FireAction.IsPressed()) {
                commandQueue.Enqueue(CommandIDs.UseEquippable);
            }
            if(Mouse.current != null) {
                if(Mouse.current.scroll.ReadValue().y < 0) {
                    cmdCycleDir = SlotCycleDirection.Down;
                    commandQueue.Enqueue(CommandIDs.Swap);
                }
                if(Mouse.current.scroll.ReadValue().y > 0) {
                    cmdCycleDir = SlotCycleDirection.Up;
                    commandQueue.Enqueue(CommandIDs.Swap);
                }
            }
            if(WeaponSwapAction.WasPressedThisFrame()) {
                cmdCycleDir = SlotCycleDirection.Up;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(WeaponSwapPrimaryAction.WasPressedThisFrame()) {
                cmdEquipSlot = EquipmentSlot.Primary;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(WeaponSwapSecondaryAction.WasPressedThisFrame()) {
                cmdEquipSlot = EquipmentSlot.Secondary;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(UseAbility1Action.WasPressedThisFrame()) {
                cmdAbilityIndex = 0;
                commandQueue.Enqueue(CommandIDs.UseAbility);
            }
            if(UseAbility2Action.WasPressedThisFrame()) {
                cmdAbilityIndex = 1;
                commandQueue.Enqueue(CommandIDs.UseAbility);
            }
        }
        
        private void HandleInputTouch()
        {
            Vector2 move = GameControls.LeftStickValue;
            controller.ControlsSetMovement(move);

            if(DodgeAction.WasPressedThisFrame()) {
                cmdDodgeDir = new Vector3(move.y, 0.0f, -move.x);
                commandQueue.Enqueue(CommandIDs.Dodge);
            }
            if(QuickHealAction.WasPressedThisFrame()) {
                commandQueue.Enqueue(CommandIDs.QuickHeal);
            }
            if(ReloadAction.WasPressedThisFrame()) {
                commandQueue.Enqueue(CommandIDs.Reload);
            }
            if(GameControls.RightStickPressed) {
                commandQueue.Enqueue(CommandIDs.UseEquippable);
            }
            if(WeaponSwapAction.WasPressedThisFrame()) {
                cmdCycleDir = SlotCycleDirection.Up;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(WeaponSwapPrimaryAction.WasPressedThisFrame()) {
                cmdEquipSlot = EquipmentSlot.Primary;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(WeaponSwapSecondaryAction.WasPressedThisFrame()) {
                cmdEquipSlot = EquipmentSlot.Secondary;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(UseAbility1Action.WasPressedThisFrame()) {
                cmdAbilityIndex = 0;
                commandQueue.Enqueue(CommandIDs.UseAbility);
            }
            if(UseAbility2Action.WasPressedThisFrame()) {
                cmdAbilityIndex = 1;
                commandQueue.Enqueue(CommandIDs.UseAbility);
            }
        }

        private void HandleInputGamepad()
        {
            Vector2 move = GameControls.LeftStickValue;
            controller.ControlsSetMovement(move);

            if(DodgeAction.WasPressedThisFrame()) {
                cmdDodgeDir = new Vector3(move.y, 0.0f, -move.x);
                commandQueue.Enqueue(CommandIDs.Dodge);
            }
            if(QuickHealAction.WasPressedThisFrame()) {
                commandQueue.Enqueue(CommandIDs.QuickHeal);
            }
            if(ReloadAction.WasPressedThisFrame()) {
                commandQueue.Enqueue(CommandIDs.Reload);
            }
            if(FireAction.IsPressed()) {
                commandQueue.Enqueue(CommandIDs.UseEquippable);
            }
            if(WeaponSwapAction.WasPressedThisFrame()) {
                cmdCycleDir = SlotCycleDirection.Up;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(WeaponSwapPrimaryAction.WasPressedThisFrame()) {
                cmdEquipSlot = EquipmentSlot.Primary;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(WeaponSwapSecondaryAction.WasPressedThisFrame()) {
                cmdEquipSlot = EquipmentSlot.Secondary;
                commandQueue.Enqueue(CommandIDs.Swap);
            }
            if(UseAbility1Action.WasPressedThisFrame()) {
                cmdAbilityIndex = 0;
                commandQueue.Enqueue(CommandIDs.UseAbility);
            }
            if(UseAbility2Action.WasPressedThisFrame()) {
                cmdAbilityIndex = 1;
                commandQueue.Enqueue(CommandIDs.UseAbility);
            }
        }
        
        private void AttackOrReload()
        {
            Transform aim        = controller.AimTarget;
            Transform cTransform = entity.CTransform;
            if(ReferenceEquals(aim, null))
                return;

            Vector3 aimPos    = aim.position;
            Vector3 position  = cTransform.position;
            Vector3 targetPos = new(aimPos.x, 0.0f, aimPos.z);
            Vector3 pos       = new(position.x, 0.0f, position.z);
            if(Vector3.Distance(targetPos, pos) < 0.75f)
                return;

            Equipment.Use();
        }

        private void QuickHeal()
        {
            controller.QuickHeal.DoHeal();
        }

        private static class CommandIDs
        {
            public static int Swap          => 10;
            public static int Reload        => 20;
            public static int UseEquippable => 21;
            public static int Dodge         => 30;
            public static int QuickHeal     => 40;
            public static int UseAbility    => 50;
        }

        private static class CommandOrders
        {
            public static uint Swap          => 10u;
            public static uint Reload        => 20u;
            public static uint UseEquippable => 21u;
            public static uint Dodge         => 30u;
            public static uint QuickHeal     => 22u;
            public static uint UseAbility    => 40u;
        }
    }

}
