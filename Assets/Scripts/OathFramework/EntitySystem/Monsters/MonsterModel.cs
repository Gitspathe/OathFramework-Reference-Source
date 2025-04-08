using UnityEngine;

namespace OathFramework.EntitySystem.Monsters
{ 

    [RequireComponent(typeof(Animator))]
    public class MonsterModel : EntityModel
    {
        protected override void Awake()
        {
            base.Awake();
        }
    }

}
