using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.UI.Info;
using OathFramework.UI.Platform;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OathFramework.UI
{

    public class ChatUI : LoopComponent, 
        ILoopUpdate, IChatReceivedMessage, IResetGameStateCallback,
        IOnScreenKeyboardCallback
    {
        [SerializeField] private GameObject messagePrefab;
        [SerializeField] private Transform messageParent;
        [SerializeField] private int maxMessages = 20;

        [Space(10)]
        
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private UINavPin uiNavPin;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text inputFieldValue;

        [Header("Optional")]
        [SerializeField] private UISelectableProxy selectableProxy;
        [SerializeField] private PlayerInfoHolder playerInfoHolder;
        [SerializeField] private MoveDirection playerInfoHolderDirection;
        [SerializeField] private bool applyToPlayerInfos = true;
        
        private List<GameObject> curMessages = new();
        private bool isFocused;
        
        private void Awake()
        {
            ChatCallbacks.Register((IChatReceivedMessage)this);
            GameControlsCallbacks.Register((IOnScreenKeyboardCallback)this);
            GameCallbacks.Register((IResetGameStateCallback)this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if(inputField != null) {
                inputField.text = "";
            }
            isFocused = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(playerInfoHolder != null && selectableProxy != null) {
                Selectable selectable = selectableProxy.GetComponent<Selectable>();
                Navigation nav        = selectable.navigation;
                switch(playerInfoHolderDirection) {
                    case MoveDirection.Left:
                        nav.selectOnLeft = playerInfoHolder.GetFirst();
                        if(applyToPlayerInfos) {
                            playerInfoHolder.SetSelectable(selectable, MoveDirection.Right);
                        }
                        break;
                    case MoveDirection.Up:
                        nav.selectOnUp = playerInfoHolder.GetFirst();
                        if(applyToPlayerInfos) {
                            playerInfoHolder.SetSelectable(selectable, MoveDirection.Down);
                        }
                        break;
                    case MoveDirection.Right:
                        nav.selectOnRight = playerInfoHolder.GetFirst();
                        if(applyToPlayerInfos) {
                            playerInfoHolder.SetSelectable(selectable, MoveDirection.Left);
                        }
                        break;
                    case MoveDirection.Down:
                        nav.selectOnDown = playerInfoHolder.GetFirst();
                        if(applyToPlayerInfos) {
                            playerInfoHolder.SetSelectable(selectable, MoveDirection.Up);
                        }
                        break;
                    
                    case MoveDirection.None:
                    default:
                        break;
                }
                selectable.navigation = nav;
            }
        }

        private void OnDestroy()
        {
            ChatCallbacks.Unregister((IChatReceivedMessage)this);
            GameControlsCallbacks.Unregister((IOnScreenKeyboardCallback)this);
            GameCallbacks.Unregister((IResetGameStateCallback)this);
        }
        
        public void Send()
        {
            if(Chat.Instance == null || Chat.TryGetCooldown(NetClient.Self, out _) || !Chat.SendChatMessage(inputField.text))
                return;

            inputField.text = "";
            if(!GameControls.UsingController) {
                inputField.ActivateInputField();
            }
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            // Gamepad controls.
            if(GameControls.UsingController) {
                if(OnScreenKeyboard.Instance.focus == inputField) {
                    if(!isFocused) {
                        isFocused = true;
                    }
                } else if(isFocused) {
                    isFocused = false;
                    uiNavPin?.Unpin();
                }
                return;
            }
            
            // Keyboard & touch controls.
            if(!ReferenceEquals(inputField, null) && EventSystem.current.currentSelectedGameObject == inputField.gameObject) {
                if(!isFocused) {
                    isFocused = true;
                    uiNavPin?.Pin();
                }
                if(UIControlsInputHandler.SubmitAction.WasPressedThisFrame()) {
                    Send();
                }
            } else if(isFocused) {
                isFocused = false;
                uiNavPin?.Unpin();
            }
        }

        void IChatReceivedMessage.OnReceivedChatMessage(NetClient player, string message)
        {
            if(curMessages.Count >= maxMessages) {
                Destroy(curMessages[0]);
                curMessages.RemoveAt(0);
            }
            GameObject go = Instantiate(messagePrefab, messageParent);
            go.GetComponent<ChatMessage>().Setup($"<color=red>{player.Name}:</color=red><space=10>{message}");
            curMessages.Add(go);
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0.0f;
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            foreach(GameObject go in curMessages) {
                Destroy(go);
            }
            curMessages.Clear();
        }

        void IOnScreenKeyboardCallback.OnOSKOpened(Selectable target) {}

        void IOnScreenKeyboardCallback.OnOSKSubmit(Selectable target)
        {
            if(OnScreenKeyboard.Instance.focus == inputField) {
                Send();
            }
        }

        void IOnScreenKeyboardCallback.OnOSKClosed() {}
    }

}
