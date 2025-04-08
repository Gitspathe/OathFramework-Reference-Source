using UnityEngine;
using UnityEngine.InputSystem;

namespace OathFramework.Debugging
{
    public class SimpleCameraController : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference modifierAction;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;

        [Header("Input Settings")]
        [SerializeField] private float mouseSensitivity = 0.2f;
        
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool isModifierActive;
        
        private void OnEnable()
        {
            if(moveAction != null) {
                moveAction.action.Enable();
            }
            if(lookAction != null) {
                lookAction.action.Enable();
            }
            if(modifierAction != null) {
                modifierAction.action.Enable();
                modifierAction.action.started  += OnModifierStarted;
                modifierAction.action.canceled += OnModifierCanceled;
            }
        }

        private void OnDisable()
        {
            if(moveAction != null) {
                moveAction.action.Disable();
            }
            if(lookAction != null) {
                lookAction.action.Disable();
            }
            if(modifierAction != null) { 
                modifierAction.action.Disable();
                modifierAction.action.started  -= OnModifierStarted;
                modifierAction.action.canceled -= OnModifierCanceled;
            }
        }

        private void Update()
        {
            if(moveAction != null) {
                moveInput = moveAction.action.ReadValue<Vector2>();
            }
            if(lookAction != null) {
                lookInput = lookAction.action.ReadValue<Vector2>();
            }

            Vector3 moveDirection = new(moveInput.x, 0, moveInput.y);
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);
            if(!isModifierActive)
                return;
            
            float yaw   = lookInput.x * mouseSensitivity;
            float pitch = -lookInput.y * mouseSensitivity;
            transform.Rotate(Vector3.up, yaw, Space.World);
            transform.Rotate(Vector3.right, pitch, Space.Self);
        }
        
        private void OnModifierStarted(InputAction.CallbackContext context)
        {
            isModifierActive = true;
        }

        private void OnModifierCanceled(InputAction.CallbackContext context)
        {
            isModifierActive = false;
        }
    }
}
