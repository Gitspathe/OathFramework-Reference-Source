using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Utility;

namespace OathFramework.Audio
{

    [RequireComponent(typeof(Entity))]
    public class EntityAudio : NetworkBehaviour, IEntityDieCallback
    {
        [SerializeField] private Transform audioTransform;
        [SerializeField] private EntityAudioClip[] clips;
        [SerializeField] private EntityLoopAudioClip[] loops;
        [SerializeField] private bool stopLoopsOnDeath = true;

        private Dictionary<string, byte> clipDataIndexDict         = new();
        private Dictionary<string, byte> loopDataIndexDict         = new();
        private Dictionary<byte, EntityAudioClip> clipDataDict     = new();
        private Dictionary<byte, EntityAudioLoop> loopDataDict     = new();
        private Dictionary<byte, EntityAudioLoop> loopsPlayingDict = new();
        private AudioOverrides closeOverrides;

        public IAudioSpatialCondition SpatialCondition { get; set; }
        
        uint ILockableOrderedListElement.Order => 10_000;
        
        private void Awake()
        {
            closeOverrides = AudioOverrides.NoSpatialBlend;

            byte i = 0;
            foreach(EntityAudioClip data in clips) {
                clipDataIndexDict.Add(data.ID, i);
                clipDataDict.Add(i, data);
                i++;
            }

            i = 0;
            foreach(EntityLoopAudioClip data in loops) {
                loopDataIndexDict.Add(data.ID, i);
                loopDataDict.Add(i, data.Loop);
                i++;
            }
        }
        
        public void OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityDieCallback)this);
        }

        public void ApplyClipData(List<EntityAudioClip> data)
        {
            foreach(EntityAudioClip clip in data) {
                SetAudioClip(clip.ID, clip.Params, clip.FollowEntity);
            }
        }
        
        public void SetAudioClip(string id, AudioParams @params, bool followEntity = true)
        {
            if(!clipDataIndexDict.TryGetValue(id, out byte b) || !clipDataDict.TryGetValue(b, out EntityAudioClip data)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No AudioClip with ID '{id}' found.");
                }
                return;
            }
            
            if(ReferenceEquals(@params, null)) {
                data.Params = null;
                return;
            }
            data.Params       = @params;
            data.FollowEntity = followEntity;
        }

        private void PlayAudioClip(byte id)
        {
            if(!clipDataDict.TryGetValue(id, out EntityAudioClip clip)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No clip with index '{id}' found in entity {gameObject.name}.");
                }
                return;
            }
            if(ReferenceEquals(clip.Params, null))
                return;

            bool spatial = true;
            if(SpatialCondition != null) {
                spatial = SpatialCondition.GetAudioSpatial();
            }
            AudioPool.Retrieve(
                clip.FollowEntity ? audioTransform : null, 
                clip.FollowEntity ? Vector3.zero : transform.position, 
                clip.Params, 
                overrides: spatial ? null : closeOverrides
            );
        }

        private void PlayAudioLoop(byte id, float fadeTime = 1.0f)
        {
            if(loopsPlayingDict.ContainsKey(id))
                return;

            if(!loopDataDict.TryGetValue(id, out EntityAudioLoop loop)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No loop with index '{id}' found in entity {gameObject.name}.");
                }
                return;
            }
            
            bool spatial = true;
            if(SpatialCondition != null) {
                spatial = SpatialCondition.GetAudioSpatial();
            }
            loop.FadeIn(fadeTime, spatial);
            loopsPlayingDict.Add(id, loop);
        }

        private void StopAudioLoop(byte id, float fadeTime = 1.0f)
        {
            if(!loopsPlayingDict.TryGetValue(id, out EntityAudioLoop loop)) {
                return;
            }

            loop.FadeOut(fadeTime);
            loopsPlayingDict.Remove(id);
        }

        public void PlayAudioClip(string id, bool sync = false)
        {
            if(!clipDataIndexDict.TryGetValue(id, out byte index)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No clip '{id}' found in entity {gameObject.name}");
                }
                return;
            }

            PlayAudioClip(index);
            if(sync && IsOwner) {
                PlayAudioClipServerRpc(index);
            }
        }

        public void PlayAudioLoop(string id, float fadeTime = 1.0f, bool sync = false)
        {
            if(!loopDataIndexDict.TryGetValue(id, out byte index)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No loop '{id}' found in entity {gameObject.name}");
                }
                return;
            }

            PlayAudioLoop(index, fadeTime);
            if(sync && IsOwner) {
                PlayAudioLoopServerRpc(index, fadeTime);
            }
        }

        public void StopAudioLoop(string id, float fadeTime = 1.0f, bool sync = false)
        {
            if(!loopDataIndexDict.TryGetValue(id, out byte index)) {
                if(Game.ExtendedDebug) {
                    Debug.LogError($"No loop '{id}' found in entity {gameObject.name}");
                }
                return;
            }

            StopAudioLoop(index, fadeTime);
            if(sync && IsOwner) {
                StopAudioLoopServerRpc(index, fadeTime);
            }
        }

        public void StopAudioLoops(bool sync = false)
        {
            foreach(EntityAudioLoop source in loopsPlayingDict.Values) {
                source.FadeOut(-1.0f);
            }
            loopsPlayingDict.Clear();

            if(sync && IsOwner) {
                StopAudioLoopsServerRpc();
            }
        }

        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            if(stopLoopsOnDeath) {
                StopAudioLoops();
            }
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void PlayAudioClipServerRpc(byte index)
        {
            PlayAudioClipClientRpc(index);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void PlayAudioClipClientRpc(byte index)
        {
            if(IsOwner)
                return;

            PlayAudioClip(index);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
        private void PlayAudioLoopServerRpc(byte index, float fadeTime)
        {
            PlayAudioLoopClientRpc(index, fadeTime);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        private void PlayAudioLoopClientRpc(byte index, float fadeTime)
        {
            if(IsOwner)
                return;

            PlayAudioLoop(index, fadeTime);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
        private void StopAudioLoopServerRpc(byte index, float fadeTime)
        {
            StopAudioLoopClientRpc(index, fadeTime);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        private void StopAudioLoopClientRpc(byte index, float fadeTime)
        {
            if(IsOwner)
                return;

            StopAudioLoop(index, fadeTime);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
        private void StopAudioLoopsServerRpc()
        {
            StopAudioLoopsClientRpc();
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable)]
        private void StopAudioLoopsClientRpc()
        {
            StopAudioLoops();
        }
    }

    public interface IAudioSpatialCondition
    {
        bool GetAudioSpatial();
    }

}
