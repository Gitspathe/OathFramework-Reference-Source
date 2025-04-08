using System.Runtime.CompilerServices;
using UnityEngine;

namespace OathFramework.Effects
{
    public abstract class EffectNode : MonoBehaviour
    {
        protected EffectBase Parent;
        protected ParticleSystem.MinMaxGradient? Color;
        protected ParticleSystem.MinMaxGradient? MinMaxColor;
        
        public void Initialize(EffectBase effect)
        {
            Parent = effect;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetColor(ParticleSystem.MinMaxGradient? color = null)
        {
            Color = color;
            OnSetColor(color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Show()
        {
            OnShow();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dissipate(float duration)
        {
            OnDissipate(duration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Hide()
        {
            OnHide();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddedToSockets(byte spot, ModelSocketHandler sockets)
        {
            OnAddedToSockets(spot, sockets);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemovedFromSockets(ModelSocketHandler sockets, ModelPlugRemoveBehavior returnBehavior)
        {
            OnRemovedFromSockets(sockets, returnBehavior);
        }
        
        protected virtual void OnAddedToSockets(byte spot, ModelSocketHandler sockets) { }
        protected virtual void OnRemovedFromSockets(ModelSocketHandler sockets, ModelPlugRemoveBehavior returnBehavior) { }
        
        protected abstract void OnSetColor(ParticleSystem.MinMaxGradient? color);
        protected abstract void OnShow();
        protected abstract void OnDissipate(float duration);
        protected abstract void OnHide();
    }
}
