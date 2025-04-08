using UnityEngine;

namespace OathFramework.Persistence
{
    public class PersistentSceneBehaviour : MonoBehaviour
    {
        [field: SerializeField] public PersistentScene Scene { get; private set; }

        private void Awake()
        {
            if(!Scene.IsGlobal) {
                PersistenceManager.RegisterScene(Scene);
            }
        }
    }
}
