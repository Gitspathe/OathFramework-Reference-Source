using OathFramework.EntitySystem.Players;
using UnityEngine;
using Unity.Netcode;
using OathFramework.Networking;

namespace OathFramework.Core.Service
{

    public class NotificationService : MonoBehaviour, IPlayerConnectedCallback, IPlayerDisconnectedCallback
    {
        public static NotificationService Instance { get; private set; }

        public NotificationService Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(NotificationService)} singletons.");
                return null;
            }

            Instance = this;
            GameCallbacks.Register((IPlayerConnectedCallback)this);
            GameCallbacks.Register((IPlayerDisconnectedCallback)this);
            return Instance;
        }
        
        void IPlayerConnectedCallback.OnPlayerConnected(NetClient client)
        {
            if(client.OwnerClientId != NetGame.Manager.LocalClientId) {
                PlayerConnected(client.Name);
            }
        }

        void IPlayerDisconnectedCallback.OnPlayerDisconnected(NetClient client)
        {
            if(client.OwnerClientId != NetGame.Manager.LocalClientId && NetGame.ConnectionState != GameConnectionState.Disconnected) {
                PlayerDisconnected(client.Name);
            }
        }

        public void PlayerConnected(string playerName)
        {
            //HUDScript.Instance.ShowMessage($"{playerName} connected");
        }

        public void PlayerDisconnected(string playerName)
        {
            //HUDScript.Instance.ShowMessage($"{playerName} disconnected");
        }

        public void PlayerKicked(string playerName)
        {
            //HUDScript.Instance.ShowMessage($"Kicked {playerName}");
        }

        public void PlayerDied(NetClient player)//, DamageValue lastDamageVal)
        {
            //switch(lastDamageVal.Source) {
            //    case DamageSource.SyncDeath: { 
            //        // Don't show message on sync death.
            //    } break;
            //    case DamageSource.DieCommand: {
            //        HUDScript.Instance.ShowNotification($"{player.Name} redeployed");
            //    } break;
            //    case DamageSource.Water: {
            //        HUDScript.Instance.ShowNotification($"{player.Name} froze to death");
            //    } break;
            //    case DamageSource.Entity: {
            //        HUDScript.Instance.ShowNotification($"{player.Name} was killed by {lastDamageVal.Instigator.Name}");
            //    } break;
            //    case DamageSource.Player: { 
            //        HUDScript.Instance.ShowNotification($"{player.Name} was killed by {lastDamageVal.Instigator.Name}");
            //        // TODO: Weapon?
            //    } break;

            //    case DamageSource.Fall:
            //    case DamageSource.Undefined:
            //    default: { 
            //        HUDScript.Instance.ShowNotification($"{player.Name} died");
            //    } break;
            //}
        }
    }

}
