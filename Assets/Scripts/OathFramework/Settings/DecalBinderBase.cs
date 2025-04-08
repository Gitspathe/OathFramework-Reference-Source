using OathFramework.Core;

namespace OathFramework.Settings
{
    public abstract class DecalBinderBase : LoopComponent
    {
        public abstract void Apply(SettingsManager.GraphicsSettings settings);
        
        protected virtual void Awake()
        {
            SettingsManager.RegisterDecal(this);
        }

        protected virtual void OnDestroy()
        {
            SettingsManager.UnregisterDecal(this);
        }
    }
}
