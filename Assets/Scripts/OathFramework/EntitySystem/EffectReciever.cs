using UnityEngine;

namespace OathFramework.EntitySystem
{
    [RequireComponent(typeof(Collider))]
    public class EffectReceiver : MonoBehaviour
    {
        [field: SerializeField] public Entity Entity { get; private set; }
    }
}
