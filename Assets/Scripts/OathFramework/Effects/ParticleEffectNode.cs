using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    public class ParticleEffectNode : EffectNode
    {
        [SerializeField] private ParticleSystem particleSys;
        [SerializeField] private bool deferStateControl;
        [SerializeField] private bool hideImmediately;
        [SerializeField] private bool handleChildren = true;

        protected List<ParticleSystem> Children = new();
        protected ParticleSystem PS;
        protected ParticleSystem.MinMaxGradient DefaultColor;
        
        protected virtual void Awake()
        {
            PS           = particleSys != null ? particleSys : GetComponent<ParticleSystem>();
            DefaultColor = PS.main.startColor;
            if(handleChildren) {
                Children.AddRange(GetComponentsInChildren<ParticleSystem>(true));
            }
        }

        protected override void OnSetColor(ParticleSystem.MinMaxGradient? color)
        {
            if(ReferenceEquals(PS, null)) 
                return;
            
            ParticleSystem.MainModule module = PS.main;
            module.startColor = color ?? DefaultColor;
            if(!handleChildren)
                return;

            foreach(ParticleSystem psC in Children) {
                ParticleSystem.MainModule moduleC = psC.main;
                moduleC.startColor = color ?? DefaultColor;
            }
        }

        protected override void OnShow()
        {
            if(deferStateControl)
                return;
            
            PS.Play(handleChildren);
        }

        protected override void OnDissipate(float duration)
        {
            if(deferStateControl)
                return;
            
            PS.Stop(handleChildren, hideImmediately ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }

        protected override void OnHide()
        {
            if(deferStateControl)
                return;
            
            PS.Stop(handleChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
