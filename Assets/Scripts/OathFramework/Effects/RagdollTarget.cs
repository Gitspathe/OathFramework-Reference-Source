using OathFramework.Core;
using OathFramework.Pooling;
using OathFramework.Settings;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Effects
{
    public class RagdollTarget : RagdollBase, IPoolableComponent, ILoopUpdate
    {
        [Header("Ragdoll Properties")]
        [SerializeField] private Rigidbody body;
        [SerializeField] private byte matBinderStdSet;
        [SerializeField] private byte matBinderFadeSet  = 1;
        [SerializeField] private float sleepTime        = 3.0f;
        [SerializeField] private float maxTime          = 10.0f;
        [SerializeField] private float fadeTime         = 2.0f;
        [SerializeField] private string matFadePropName = "_Fade";

        private MaterialBinderEx binder;
        private int matFadeID;
        private float curTime;
        private float timeMult = 1.0f;
        private bool isFading;
        private bool isSleeping;
        private List<Rigidbody> bodies              = new();
        private List<Collider> colliders            = new();
        private List<SkinnedMeshRenderer> renderers = new();
        private List<Material> matCache             = new(4);
        private MaterialPropertyBlock propertyBlock;
        
        public PoolableGameObject PoolableGO { get; set; }

        public float MaxTime => maxTime * timeMult;
        
        private static readonly int MatBaseColorID = Shader.PropertyToID("_BaseColor");
        
        protected override List<TransformMapping.TransformData> GetData() => MappingAsset.TargetTransforms;

        protected override void OnInitialize()
        {
            SettingsManager.RegisterRagdoll(this);
            propertyBlock = new MaterialPropertyBlock();
            matFadeID     = Shader.PropertyToID("_Fade");
            binder        = GetComponent<MaterialBinderEx>();
            bodies.AddRange(GetComponentsInChildren<Rigidbody>());
            colliders.AddRange(GetComponentsInChildren<Collider>());
            foreach(Rigidbody rb in bodies) {
                rb.sleepThreshold = RagdollManager.Instance.SleepThreshold;
            }
            foreach(SkinnedMeshRenderer render in GetComponentsInChildren<SkinnedMeshRenderer>()) {
                renderers.Add(render);
            }
        }

        private void OnDestroy()
        {
            SettingsManager.UnregisterRagdoll(this);
        }

        public void AssignTransforms(RagdollSource source)
        {
            for(int i = 0; i < source.Tree.Transforms.Length; i++) {
                if(MappingAsset.SourceTransforms[i].Excluded)
                    continue;
                
                Transform t = source.Tree.Transforms[i];
                Tree.Transforms[i].SetPositionAndRotation(t.position, t.rotation);
            }
        }

        public void ApplyForce(float force, float radius, float upwardsModifier, Vector3 position)
        {
            body.AddExplosionForce(force, position, radius, upwardsModifier, ForceMode.Force);
        }

        public void ApplySettings(SettingsManager.GraphicsSettings settings)
        {
            ApplySettingsTime(settings);
            ApplySettingsRigidBody(settings);
            ApplySettingsJoints(settings);
        }
        
        private void ApplySettingsTime(SettingsManager.GraphicsSettings settings)
        {
            switch(settings.ragdolls) {
                case 0: {
                    timeMult = 0.5f;
                } break;
                case 1: {
                    timeMult = 0.75f;
                } break;
                
                case 2:
                case 3: {
                    timeMult = 1.0f;
                } break;
            }
        }

        private void ApplySettingsRigidBody(SettingsManager.GraphicsSettings settings)
        {
            foreach(Rigidbody rb in bodies) {
                switch(settings.ragdolls) {
                    case 0: {
                        rb.solverIterations         = 3;
                        rb.solverVelocityIterations = 1;
                    } break;
                    case 1: {
                        rb.solverIterations         = 6;
                        rb.solverVelocityIterations = 1;
                    } break;
                    case 2: {
                        rb.solverIterations         = 6;
                        rb.solverVelocityIterations = 2;
                    } break;
                    case 3: {
                        rb.solverIterations         = 10;
                        rb.solverVelocityIterations = 2;
                    } break;
                }
            }
        }
        
        private void ApplySettingsJoints(SettingsManager.GraphicsSettings settings)
        {
            foreach(CharacterJoint joint in GetComponentsInChildren<CharacterJoint>(true)) {
                switch(settings.ragdolls) {
                    case 0: {
                        joint.enableProjection = false;
                    } break;
                    case 1: {
                        joint.enableProjection = false;
                    } break;
                    case 2: {
                        joint.enableProjection = true;
                    } break;
                    case 3: {
                        joint.enableProjection = true;
                    } break;
                }
            }
        }

        void ILoopUpdate.LoopUpdate()
        {
            curTime += Time.deltaTime;
            HandleSleeping();
            HandleFading();
        }

        private void HandleFading()
        {
            if(!isFading) {
                if(curTime < MaxTime)
                    return;

                isFading = true;
                foreach(Rigidbody rb in bodies) {
                    rb.isKinematic = true;
                }
                foreach(Collider col in colliders) {
                    col.isTrigger = true;
                }
                return;
            } 
            if(curTime - MaxTime == 0.0f)
                return;

            if(!ReferenceEquals(binder, null)) {
                binder.CurrentSet = matBinderFadeSet;
            }
            foreach(SkinnedMeshRenderer render in renderers) {
                SetFade(render, (curTime - MaxTime) / fadeTime);
            }
            if(curTime > MaxTime + fadeTime) {
                PoolableGO.Return();
            }
        }

        private void HandleSleeping()
        {
            if(isSleeping || curTime < sleepTime || body.velocity.sqrMagnitude > 0.05f)
                return;
            
            foreach(Rigidbody rb in bodies) {
                rb.Sleep();
            }
            isSleeping = true;
        }

        private void SetFade(SkinnedMeshRenderer render, float fade)
        {
            matCache.Clear();
            render.GetMaterials(matCache);
            render.GetPropertyBlock(propertyBlock);
            foreach(Material mat in matCache) {
                // Step 1 - Try fade param.
                if(mat.HasProperty(matFadeID)) {
                    propertyBlock.SetFloat(matFadeID, fade);
                    continue;
                }
                
                // Step 2 - No fade param. Try color alpha.
                if(mat.HasColor(MatBaseColorID)) {
                    Color col = mat.GetColor(MatBaseColorID);
                    col.a     = 1.0f - fade;
                    propertyBlock.SetColor(MatBaseColorID, col);
                }
            }
            render.SetPropertyBlock(propertyBlock);
        }

        void IPoolableComponent.OnRetrieve()
        {
            foreach(SkinnedMeshRenderer render in renderers) {
                SetFade(render, 0.0f);
            }
            if(!ReferenceEquals(binder, null)) {
                binder.CurrentSet = matBinderStdSet;
            }
        }

        void IPoolableComponent.OnReturn(bool initialization)
        {
            curTime    = 0.0f;
            isFading   = false;
            isSleeping = false;
            foreach(Rigidbody rb in bodies) {
                rb.isKinematic     = false;
                rb.velocity        = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            foreach(Collider col in colliders) {
                col.isTrigger = false;
            }
        }
    }
}
