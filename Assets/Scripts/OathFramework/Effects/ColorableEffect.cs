using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.Effects
{
    public class ColorableEffect : MonoBehaviour, IColorable
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private bool execOnEnable;
        [SerializeField] private string colorPropertyName;
        [SerializeField] protected ParticleSystem.MinMaxGradient defaultColor = Color.white;

        public ParticleSystem.MinMaxGradient? CurrentColor { get; private set; }
        
        private QList<EffectNode> nodes = new();
        private int colorParam;
        private MaterialPropertyBlock propertyBlock;

        protected virtual void Awake()
        {
            CurrentColor = defaultColor;
            if(!string.IsNullOrEmpty(colorPropertyName) && renderers.Length > 0) {
                propertyBlock = new MaterialPropertyBlock();
                colorParam    = Shader.PropertyToID(colorPropertyName);
            }
            EffectNode[] arr = GetComponentsInChildren<EffectNode>(true);
            nodes            = new QList<EffectNode>(arr.Length);
            nodes.AddRange(arr);
        }

        private void OnEnable()
        {
            if(!execOnEnable)
                return;
            
            SetColor();
        }

        public virtual void SetColor(ParticleSystem.MinMaxGradient? color = null)
        {
            CurrentColor = color;
            ParticleSystem.MinMaxGradient col = color ?? defaultColor.color;
            if(!ReferenceEquals(propertyBlock, null)) {
                propertyBlock.SetColor(colorParam, col.color);
                foreach(Renderer r in renderers) {
                    r.SetPropertyBlock(propertyBlock);
                }
            }
            for(int i = 0; i < nodes.Count; i++) {
                nodes.Array[i].SetColor(color);
            }
        }
    }
}
