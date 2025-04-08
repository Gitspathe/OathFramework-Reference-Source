using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.Utility;
using System.Diagnostics;
using Unity.Netcode;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem.Players
{ 

    public sealed class PlayerManager : Subsystem, IResetGameStateCallback
    {
        private Dictionary<byte, NetClient> players             = new();
        private Dictionary<byte, string> playerIndexToUIDLookup = new();
        private Dictionary<string, byte> playerUIDToIndexLookup = new();

        [field: SerializeField] public byte DefaultMaxPlayers { get; private set; } = 6;
        
        [field: Space(10)]
        
        [field: SerializeField] public DummyPlayer DummyPlayer    { get; private set; }
        [field: SerializeField] public DummyPlayer AltDummyPlayer { get; private set; }

        public static Dictionary<byte, NetClient>.ValueCollection Players => Instance.players.Values;
        public static byte PlayerCount => (byte)Instance.players.Count;
        public static byte MaxPlayers { get; private set; }

        public static PlayerManager Instance { get; private set; }
        
        public override string Name    => "Player Manager";
        public override uint LoadOrder => SubsystemLoadOrders.PlayerManager;

        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple '{nameof(PlayerManager)}' singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            SetMaxPlayers(DefaultMaxPlayers);
            GameCallbacks.Register((IResetGameStateCallback)this);
            return UniTask.CompletedTask;
        }

        public static void AssignPlayerProxyInfo(byte index, string uid)
        {
            Instance.playerIndexToUIDLookup.Add(index, uid);
            Instance.playerUIDToIndexLookup.Add(uid, index);
        }

        public static void ClientConnected(NetClient client)
        {
            Instance.players.Add(client.Index, client);
            if(Instance.playerUIDToIndexLookup.ContainsKey(client.UniqueID)) {
                // Player from snapshot has joined.
            } else {
                Instance.playerUIDToIndexLookup.Add(client.UniqueID, client.Index);
                Instance.playerIndexToUIDLookup.Add(client.Index, client.UniqueID);
            }
            GameCallbacks.Access.OnPlayerConnected(Game.AccessToken, client);
        }

        public static void ClientDisconnected(NetClient client)
        {
            Instance.players.Remove(client.Index);
            Instance.playerUIDToIndexLookup.Remove(client.UniqueID);
            Instance.playerIndexToUIDLookup.Remove(client.Index);
            GameCallbacks.Access.OnPlayerDisconnected(Game.AccessToken, client);
        }
        
        public static void Clear()
        {
            Instance.players.Clear();
            Instance.playerIndexToUIDLookup.Clear();
            Instance.playerUIDToIndexLookup.Clear();
        }

        public static void GetAlivePlayers(QList<NetClient> alive)
        {
            foreach(NetClient client in Players) {
                if(!client.Alive)
                    continue;

                alive.Add(client);
            }
        }

        public static void GetDeadPlayers(QList<NetClient> dead)
        {
            foreach(NetClient client in Players) {
                if(client.Alive)
                    continue;

                dead.Add(client);
            }
        }

        public static bool TryGetPlayerUID(byte index, out string uid)
        {
            return Instance.playerIndexToUIDLookup.TryGetValue(index, out uid);
        }

        public static bool TryGetPlayerIndex(string uid, out byte index)
        {
            return Instance.playerUIDToIndexLookup.TryGetValue(uid, out index);
        }
        
        public static bool TryGetPlayerFromIndex(byte index, out NetClient client)
        {
            return Instance.players.TryGetValue(index, out client);
        }

        public static bool TryGetPlayerFromUID(string uid, out NetClient client)
        {
            client = null;
            return Instance.playerUIDToIndexLookup.TryGetValue(uid, out byte index) && Instance.players.TryGetValue(index, out client);
        }

        public static bool TryGetPlayerFromNetID(ulong netID, out NetClient client)
        {
            client = null;
            foreach (NetClient p in Instance.players.Values) {
                if(p.OwnerClientId != netID)
                    continue;

                client = p;
                return true;
            }
            return false;
        }
        
        public static bool SetMaxPlayers(byte val)
        {
            val = (byte)Mathf.Clamp(val, 1, byte.MaxValue);
            if(val < Instance.players.Count)
                return false;

            Dictionary<byte, NetClient> newClients = new();
            foreach(KeyValuePair<byte, NetClient> pair in Instance.players) {
                newClients.Add(pair.Key, pair.Value);
            }

            Instance.players = newClients;
            MaxPlayers       = val;
            return true;
        }

        public static bool GetFreeClientIndex(out byte index)
        {
            index = 0;
            for (byte i = 1; i < MaxPlayers + 1; i++) {
                if(Instance.players.ContainsKey(i))
                    continue;

                index = i;
                return true;
            }
            return false;
        }

        void IResetGameStateCallback.OnResetGameState()
        {
            Clear();
        }
    }
}
