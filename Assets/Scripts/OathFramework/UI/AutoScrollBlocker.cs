using UnityEngine;

namespace OathFramework.UI
{

    public class AutoScrollBlocker : MonoBehaviour
    {
        [field: SerializeField] public AutoScrollRect Target { get; private set; }
    }

}
