using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.EntitySystem.Players
{

    [RequireComponent(typeof(Entity))]
    public class DummyPlayer : MonoBehaviour, IInitialized
    {
        public Entity Entity { get; private set; }

        public bool IsPlayer => true;
        
        uint ILockableOrderedListElement.Order => 0;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            GameCallbacks.Register((IInitialized)this);
        }

        public UniTask OnGameInitialized()
        {
            Entity = GetComponent<Entity>();
            Entity.InitParams();
            return UniTask.CompletedTask;
        }
    }

}
