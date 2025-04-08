using OathFramework.Core;
using OathFramework.EntitySystem.Players;
using OathFramework.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OathFramework.Utility
{ 

    public class CameraController : LoopComponent, ILoopLateUpdate
    {
        public override int UpdateOrder => GameUpdateOrder.EntityUpdate;

        public ICameraControllerTarget Target;
        
        public float mobileZoom = 0.2f;
        public float targetYOffset = 2.0f;
	    public Transform targetMouse;
        public Transform targetMouseUnclamped;
        
        [Space(10)]
        
        public LayerMask obstructionMask;
        public float smoothSpeed          = 5.0f;
        public Vector3 obstructionBuffer  = new(-2.0f, 0.0f, 0.0f);
        public Vector3 angledOffset       = new(2, 15, 0);
        public Vector3 topDownOffset      = new(0, 15, 0);
        public Quaternion angledRotation  = Quaternion.Euler(45f, 0f, 0f);
        public Quaternion topDownRotation = Quaternion.Euler(90f, 0f, 0f);

        private bool isObscured;
        private Vector3 targetOffset;
        private Quaternion targetRotation;
        
        private Vector3 aimDirection;
        private Vector2 oldAimVector;
        private Vector3 moveVelocity;
	    private Vector3 desiredPosition;
	    private Plane plane;
        private const float CircleRadius = 5.0f;

        public static CameraController Instance { get; private set; }

        private void Awake()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(CameraController)} singleton.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Start () {
#if UNITY_IOS || UNITY_ANDROID
            //cam.orthographicSize *= (1.0f - mobileZoom);
#endif
	    }

        public void SetTarget(ICameraControllerTarget target)
        {
            Target = target;
        }
        
        public void LoopLateUpdate()
        {
            if(Game.IsQuitting || Target == null || Target.CamFollowTransform == null)
                return;

            Vector3 targetPos  = Target.CamFollowTransform.position;
            UpdateTransform(targetPos);
            UpdateAim(targetPos);
        }

        private void UpdateTransform(Vector3 targetPos)
        {
            CheckObstruction();
            SmoothMoveCamera();
        }
        
        private void CheckObstruction()
        {
            if(Target == null || Target.CamFollowTransform == null)
                return;

            Vector3 checkPosition = Target.CamFollowTransform.position + angledOffset + obstructionBuffer;
            Ray     ray           = new(checkPosition, Target.CamFollowTransform.position - checkPosition);
            isObscured            = Physics.Raycast(ray, Vector3.Distance(checkPosition, Target.CamFollowTransform.position), obstructionMask);
            targetOffset          = isObscured ? topDownOffset : angledOffset;
        }

        private void SmoothMoveCamera()
        {
            if(Target == null || Target.CamFollowTransform == null)
                return;

            Vector3 targetPos  = Target.CamFollowTransform.position + targetOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, isObscured ? topDownRotation : angledRotation, Time.deltaTime * smoothSpeed);
        }

        private void UpdateAim(Vector3 targetPos)
        {
            Camera mainCam = Camera.main;
            if(mainCam == null || PauseMenu.IsPaused)
                return;
            
            plane = new Plane(Vector3.up, new Vector3(0.0f, targetYOffset, 0.0f));
            Ray ray;
            switch(GameControls.ControlScheme) {
                case ControlSchemes.Keyboard: {
                    if(Mouse.current == null)
                        return;
                    
                    ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
                } break;
                
                case ControlSchemes.Touch: {
                    Vector2 vec;
                    if(GameControls.RightStickPressed) {
                        vec = GameControls.RightStickValue;
                        oldAimVector = vec;
                    } else {
                        vec = oldAimVector;
                    }

                    Vector2 center = new(Screen.width / 2.0f, Screen.height / 2.0f);
                    vec            = new Vector3(center.x + (vec.x * Screen.width), center.y + (vec.y * Screen.height), 0.0f);
                    ray            = mainCam.ScreenPointToRay(new Vector3(vec.x, vec.y, 0.0f));
                } break;
                
                case ControlSchemes.Gamepad: {
                    Vector2 vec;
                    if(GameControls.RightStickPressed) {
                        vec = GameControls.RightStickValue;
                        oldAimVector = vec;
                    } else {
                        vec = oldAimVector;
                    }
                    
                    Vector2 center = new(Screen.width / 2.0f, Screen.height / 2.0f);
                    vec            = new Vector3(center.x + (vec.x * Screen.width), center.y + (vec.y * Screen.height), 0.0f);
                    ray            = mainCam.ScreenPointToRay(new Vector3(vec.x, vec.y, 0.0f));
                } break;
                
                case ControlSchemes.None:
                default:
                    Debug.LogError("No control scheme!");
                    return;
            }
            if(!plane.Raycast(ray, out float rayDistance)) 
                return;

            bool isAimDampened = PlayerController.Active != null && PlayerController.Active.IsAimDampened;
            Vector3 pos        = new(targetPos.x, targetYOffset, targetPos.z);
            Vector3 planePoint = ray.GetPoint(Mathf.Clamp(rayDistance, 2.5f, 100.0f));
            planePoint         = new Vector3(planePoint.x, targetYOffset, planePoint.z);
            Vector3 direction  = (planePoint - pos).normalized;
            
            // Add a bit of smoothing if not using keyboard.
            aimDirection = GameControls.UsingKeyboard && !isAimDampened 
                ? direction 
                : Vector3.Slerp(aimDirection, direction, (isAimDampened ? 7.5f : 15.0f) * Time.deltaTime);
            
            Vector3 clamped               = targetPos + (aimDirection * CircleRadius);
            Vector3 aimTarget             = new(clamped.x, targetYOffset, clamped.z);
            targetMouse.position          = aimTarget;
            targetMouseUnclamped.position = new Vector3(planePoint.x, 0.0f, planePoint.z);
        }
    }

    public interface ICameraControllerTarget
    {
        Transform CamFollowTransform      { get; }
        CameraController CameraController { get; set; }
    }

}
