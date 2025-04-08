using OathFramework.Core;
using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.Effects
{

    internal class UVTextureAnimator : LoopComponent, IPoolableComponent, ILoopUpdate
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        public GameObject main;
        public int rows         = 4;
        public int columns      = 4;
        public float fps        = 20;
        public int offsetMat    = 0;
        public float startDelay = 0;
        public bool dynamicMaterial;

        public bool isInterpolateFrames     = true;
        public AnimationCurve frameOverTime = AnimationCurve.Linear(0, 1, 1, 1);

        private string[] textureNames = { "_BaseMap" };
        private int[] textureIDs      = { 0 };
        private MeshRenderer meshRenderer;
        private Renderer currentRenderer;
        private Material instanceMaterial;
        private float animationStartTime;
        private bool canUpdate;
        private int previousIndex;
        private int totalFrames;
        private float currentInterpolatedTime;
        private int currentIndex;
        private Vector2 size;
        private bool isInitialized;
        private bool startDelayIsBroken;
        private bool error;
        
        public PoolableGameObject PoolableGO { get; set; }
        
        private static readonly int TexNextFrame       = Shader.PropertyToID("_Tex_NextFrame");
        private static readonly int InterpolationValue = Shader.PropertyToID("InterpolationValue");
        private IPoolableComponent poolableComponentImplementation;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            int i        = 0;
            foreach(string tex in textureNames) {
                textureIDs[i++] = Shader.PropertyToID(tex);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(isInitialized)
                return;
            
            InitDefaultVariables();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            isInitialized = false;
            previousIndex = 0;
        }

        public void LoopUpdate()
        {
            if(!startDelayIsBroken || error)
                return;
            
            ManualUpdate();
        }

        private void ManualUpdate()
        {
            if(!canUpdate || error) 
                return;

            if(dynamicMaterial) {
                UpdateMaterial();
            }
            SetSpriteAnimation();
            if(isInterpolateFrames) {
                SetSpriteAnimationInterpolated();
            }
        }

        private void StartDelayFunc()
        {
            startDelayIsBroken = true;
            animationStartTime = Time.time;
        }

        private void InitDefaultVariables()
        {
            InitializeMaterial();
            isInitialized = true;
            if(error)
                return;
            
            totalFrames        = columns * rows;
            previousIndex      = 0;
            canUpdate          = true;
            Vector3 offset     = Vector3.zero;
            size               = new Vector2(1.0f / columns, 1.0f / rows);
            animationStartTime = Time.time;
            if(startDelay > 0.00001f) {
                startDelayIsBroken = false;
                Invoke(nameof(StartDelayFunc), startDelay);
            } else {
                startDelayIsBroken = true;
            }

            foreach(string textureName in textureNames) {
                instanceMaterial.SetTextureScale(textureName, size);
                instanceMaterial.SetTextureOffset(textureName, offset);
            }
        }

        private void InitializeMaterial()
        {
            meshRenderer.enabled = true;
            currentRenderer      = GetComponent<Renderer>();
            instanceMaterial     = currentRenderer.material;
            if(instanceMaterial != null)
                return;
            
            error = true;
            if(Game.ExtendedDebug) {
                Debug.LogWarning($"Effect '{name}' has no material.");
            }
        }

        private void UpdateMaterial()
        {
            instanceMaterial = currentRenderer.material;
        }

        private void SetSpriteAnimation()
        {
            int index = (int)((Time.time - animationStartTime) * fps);
            index %= totalFrames;
            if(index < previousIndex) {
                canUpdate = false;
                meshRenderer.enabled = false;
                if(ReferenceEquals(PoolableGO, null)) {
                    Destroy(main);
                } else {
                    PoolManager.Return(this);
                }
                return;
            }

            canUpdate = true;
            meshRenderer.enabled = true;
            if(isInterpolateFrames && index != previousIndex) {
                currentInterpolatedTime = 0;
            }

            previousIndex  = index;
            int uIndex     = index % columns;
            int vIndex     = index / columns;
            float offsetX  = uIndex * size.x;
            float offsetY  = (1.0f - size.y) - vIndex * size.y;
            Vector2 offset = new(offsetX, offsetY);
            foreach(int textureID in textureIDs) {
                instanceMaterial.SetTextureScale(textureID, size);
                instanceMaterial.SetTextureOffset(textureID, offset);
            }
        }

        private void SetSpriteAnimationInterpolated()
        {
            currentInterpolatedTime += Time.deltaTime;
            int nextIndex = previousIndex + 1;
            if(nextIndex == totalFrames) {
                nextIndex = previousIndex;
            }

            int uIndex     = nextIndex % columns;
            int vIndex     = nextIndex / columns;
            float offsetX  = uIndex * size.x;
            float offsetY  = (1.0f - size.y) - vIndex * size.y;
            Vector2 offset = new(offsetX, offsetY);
            instanceMaterial.SetVector(TexNextFrame, new Vector4(size.x, size.y, offset.x, offset.y));
            instanceMaterial.SetFloat(InterpolationValue, Mathf.Clamp01(currentInterpolatedTime * fps));
        }

        public void OnRetrieve()
        {
            
        }

        public void OnReturn(bool initialization)
        {
            
        }
    }

}
