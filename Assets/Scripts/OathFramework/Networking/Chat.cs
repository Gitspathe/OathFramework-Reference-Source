using OathFramework.Core;
using OathFramework.EntitySystem.Players;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Networking
{

    public class Chat : NetLoopComponent, ILoopLateUpdate
    {
        private static List<(NetClient, float)> cooldowns    = new();
        private static List<(NetClient, float)> newCooldowns = new();
        
        private const float Delay = 0.5f;
        
        public static Chat Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;
        }

        public override void OnDestroy()
        {
            cooldowns.Clear();
            newCooldowns.Clear();
            Instance = null;
        }
        
        void ILoopLateUpdate.LoopLateUpdate()
        {
            newCooldowns.Clear();
            foreach((NetClient client, float cooldown) in cooldowns) {
                float newCooldown = cooldown - Time.unscaledDeltaTime;
                if(newCooldown > 0.0f) {
                    newCooldowns.Add((client, newCooldown));
                }
            }
            cooldowns.Clear();
            cooldowns.AddRange(newCooldowns);
        }

        public static bool TryGetCooldown(NetClient client, out float cooldown)
        {
            cooldown = -1.0f;
            foreach((NetClient other, float otherCooldown) in cooldowns) {
                if(client == other) {
                    cooldown = otherCooldown;
                    return true;
                }
            }
            return false;
        }
        
        public static bool SendChatMessage(string message)
        {
            if(TryGetCooldown(NetClient.Self, out _))
                return false;
            
            message = StripControlCharacters(message);
            if(string.IsNullOrWhiteSpace(message))
                return true;
            
            ReceiveMessage(NetClient.Self, message);
            if(Instance.IsServer) {
                Instance.SendChatMessageClientRPC(new FixedString128Bytes(message), NetClient.Self.OwnerClientId);
                return true;
            }
            Instance.SendChatMessageServerRpc(new FixedString128Bytes(message));
            return true;
        }
        
        private static void ReceiveMessage(NetClient player, string message)
        {
            cooldowns.Add((player, Delay));
            message = StripControlCharacters(message);
            if(string.IsNullOrWhiteSpace(message))
                return;
            
            ChatCallbacks.CallOnReceivedChatMessage(player, message);
        }
        
        private static string StripControlCharacters(string input)
        {
            if(string.IsNullOrWhiteSpace(input))
                return "";

            if(input.Length > FixedString128Bytes.UTF8MaxLengthInBytes) {
                input = input.Substring(0, FixedString128Bytes.UTF8MaxLengthInBytes);
            }
            string noTags    = Regex.Replace(input, "<.*?>", "");
            StringBuilder sb = StringBuilderCache.Retrieve;
            foreach(char c in noTags) {
                if(!char.IsControl(c)) {
                    sb.Append(c);
                }
            }
            return sb.ToString().Trim();
        }
        
        [Rpc(SendTo.Server)]
        private void SendChatMessageServerRpc(FixedString128Bytes msg, RpcParams @params = default)
        {
            if(!PlayerManager.TryGetPlayerFromNetID(@params.Receive.SenderClientId, out NetClient sender)) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"Received chat msg from {@params.Receive.SenderClientId}, but no {nameof(NetClient)} was found.");
                }
                return;
            }
            if(TryGetCooldown(sender, out _))
                return;
            
            ReceiveMessage(sender, msg.ToString());
            SendChatMessageClientRPC(msg, sender.OwnerClientId);
        }
        
        [Rpc(SendTo.NotServer)]
        private void SendChatMessageClientRPC(FixedString128Bytes msg, ulong playerNetID, RpcParams @params = default)
        {
            if(!PlayerManager.TryGetPlayerFromNetID(playerNetID, out NetClient sender)) {
                if(Game.ExtendedDebug) {
                    Debug.LogWarning($"Received chat msg from {playerNetID}, but no {nameof(NetClient)} was found.");
                }
                return;
            }
            if(sender == NetClient.Self || TryGetCooldown(sender, out _))
                return;
            
            ReceiveMessage(sender, msg.ToString());
        }
    }

    public static class ChatCallbacks
    {
        private static HashSet<IChatReceivedMessage> receivedMsgCallbacks = new();

        public static void Register(IChatReceivedMessage callback) => receivedMsgCallbacks.Add(callback);
        
        public static void Unregister(IChatReceivedMessage callback) => receivedMsgCallbacks.Remove(callback);

        public static void CallOnReceivedChatMessage(NetClient player, string message)
        {
            foreach(IChatReceivedMessage callback in receivedMsgCallbacks) {
                try {
                    callback.OnReceivedChatMessage(player, message);
                } catch(Exception e) {
                    Debug.LogException(e);
                }
            }
        }
    }

    public interface IChatReceivedMessage
    {
        void OnReceivedChatMessage(NetClient player, string message);
    }

}
