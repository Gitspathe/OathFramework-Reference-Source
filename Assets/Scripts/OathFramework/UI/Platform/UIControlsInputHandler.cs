using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OathFramework.UI.Platform
{

    public class UIControlsInputHandler : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;

        [Space(10)] 
        
        [SerializeField] private InputActionReference[] actionRefs;
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private InputActionReference resumeAction;
        [SerializeField] private InputActionReference navigationAction;
        [SerializeField] private InputActionReference prevTabAction;
        [SerializeField] private InputActionReference nextTabAction;
        [SerializeField] private InputActionReference toggleInfoAction;
        [SerializeField] private InputActionReference backAction;
        [SerializeField] private InputActionReference submitAction;
        [SerializeField] private InputActionReference oskSubmitAction;
        [SerializeField] private InputActionReference oskCancelAction;
        [SerializeField] private InputActionReference oskBackspaceAction;
        [SerializeField] private InputActionReference oskSwapCaseAction;

        private static Dictionary<string, InputActionReference> actionRefDict = new();
        
        public static PlayerInput PlayerInput        => Instance.playerInput;
        public static Vector2 Navigation             => Instance.navigationAction.action.ReadValue<Vector2>();
        public static InputAction PauseAction        => Instance.pauseAction.action;
        public static InputAction ResumeAction       => Instance.resumeAction.action;
        public static InputAction PrevTabAction      => Instance.prevTabAction.action;
        public static InputAction NextTabAction      => Instance.nextTabAction.action;
        public static InputAction ToggleInfoAction   => Instance.toggleInfoAction.action;
        public static InputAction BackAction         => Instance.backAction.action;
        public static InputAction SubmitAction       => Instance.submitAction.action;
        public static InputAction OSKSubmitAction    => Instance.oskSubmitAction.action;
        public static InputAction OSKCancelAction    => Instance.oskCancelAction.action;
        public static InputAction OSKBackspaceAction => Instance.oskBackspaceAction.action;
        public static InputAction OSKSwapCaseAction  => Instance.oskSwapCaseAction.action;

        public static UIControlsInputHandler Instance { get; private set; }

        public UIControlsInputHandler Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(UIControlsInputHandler)} singleton.");
                Destroy(Instance);
                return null;
            }

            foreach(InputActionReference action in actionRefs) {
                string id = $"<{action.action.actionMap.name}>/{action.action.name}".ToLower();
                actionRefDict.Add(id, action);
            }
            Instance = this;
            return this;
        }

        public static InputActionReference GetInputActionReference(string id)
        {
            id = id.ToLower();
            if(actionRefDict.TryGetValue(id, out InputActionReference actionRef))
                return actionRef;
            
            Debug.LogError($"No InputActionRef for id {id} found.");
            return null;
        }
    }

}
