using OathFramework.EntitySystem.Players;
using OathFramework.Networking;
using OathFramework.UI;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.Core.Service
{

    public class SpectateService : LoopComponent, 
        ILoopUpdate, IResetGameStateCallback, IPlayerDeathCallback
    {
        public static bool IsActive     { get; private set; }
        public static NetClient Current { get; private set; }
        
        private static QList<NetClient> alive = new();
        public static SpectateService Instance { get; private set; }
        
        public SpectateService Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(SpectateService)} singletons.");
                return null;
            }

            Instance = this;
            GameCallbacks.Register((IResetGameStateCallback)this);
            GameCallbacks.Register((IPlayerDeathCallback)this);
            return Instance;
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            if(!IsActive)
                return;
            
            TickAlive();
            if(Current == null || !Current.Alive && alive.Count == 0) {
                // TODO: Empty spectate.
            } else if(!Current.Alive) {
                SetPlayer(alive.Array[0]);
            }
        }

        private static void TickAlive()
        {
            alive.Clear();
            PlayerManager.GetAlivePlayers(alive);
        }

        public static void SetPlayer(NetClient player)
        {
            if(Current != null) {
                Current.PlayerController.ChangeMode(PlayerControllerMode.None);
            }
            Current = player;
            if(Current != null) {
                Current.PlayerController.ChangeMode(PlayerControllerMode.Spectating);
                SpectateUIScript.Instance.SetPlayer(player);
            }
        }

        public static void EnterSpectate()
        {
            SpectateUIScript.Instance.Show();
            TickAlive();
            if(alive.Count == 0) {
                ExitSpectate();
                return;
            }
            
            IsActive = true;
            SetPlayer(alive.Array[0]);
        }

        public static void ExitSpectate()
        {
            SetPlayer(null);
            SpectateUIScript.Instance.Hide();
            IsActive = false;
        }

        public static void SwapNext()
        {
            TickAlive();
            if(Current == null || alive.Count == 0) {
                SetPlayer(null);
                return;
            }

            int curIndex = Current.Index;
            int count    = alive.Count;
            for(int i = 0; i < count; i++) {
                NetClient player = alive.Array[i];
                if(player.Index != curIndex + 1)
                    continue;

                SetPlayer(player);
                return;
            }
            SetPlayer(alive.Array[0]);
        }

        public static void SwapPrevious()
        {
            TickAlive();
            if(Current == null || alive.Count == 0) {
                SetPlayer(null);
                return;
            }
            
            int curIndex = Current.Index;
            int count    = alive.Count;
            for(int i = 0; i < count; i++) {
                NetClient player = alive.Array[i];
                if(player.Index != curIndex - 1)
                    continue;

                SetPlayer(player);
                return;
            }
            SetPlayer(alive.Array[count - 1]);
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            ExitSpectate();
        }

        void IPlayerDeathCallback.OnPlayerDeath(NetClient client)
        {
            if(client == Current) {
                SwapNext();
                return;
            }
            if(client == NetClient.Self) {
                EnterSpectate();
            }
        }
    }

}
