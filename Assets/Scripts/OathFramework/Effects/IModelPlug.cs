namespace OathFramework.Effects
{
    public interface IModelPlug
    {
        ModelPlugType PlugType { get; }
        ushort ID              { get; }
        
        void OnAdd(byte spot, ModelSocketHandler sockets);
        void OnRemove(ModelSocketHandler sockets, ModelPlugRemoveBehavior returnBehavior);
        byte CurrentSpot { get; }
    }

    public enum ModelPlugRemoveBehavior
    {
        None,
        Dissipate,
        Instant
    }
    
    public enum ModelPlugType
    {
        Effect,
        Prop
    }
}
