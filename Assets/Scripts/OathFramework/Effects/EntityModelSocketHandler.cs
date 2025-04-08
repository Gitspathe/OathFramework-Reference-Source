using OathFramework.EntitySystem;

namespace OathFramework.Effects
{
    public class EntityModelSocketHandler : ModelSocketHandler
    {
        public EntityModel EntityModel { get; protected set; }
        
        public Entity Entity => EntityModel.Entity;

        protected override void Awake()
        {
            base.Awake();
            EntityModel = Model as EntityModel;
        }
    }
}
