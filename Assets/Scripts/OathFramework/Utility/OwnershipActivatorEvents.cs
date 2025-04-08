using UnityEngine;
using Unity.Netcode;

namespace OathFramework.Utility
{ 

    public class OwnershipActivatorEvents : NetworkBehaviour
    {
        [SerializeField] private Component[] componentsOwnerOnly;
        [SerializeField] private Component[] componentsNonOwnerOnly;

        [Space(10)]

        [SerializeField] private Transform[] transformsOwnerOnly;
        [SerializeField] private Transform[] transformsNonOwnerOnly;

        public override void OnNetworkSpawn()
        {
            foreach(Component behaviour in componentsOwnerOnly) {
                TrySetActive(behaviour, IsOwner);
            }
            foreach(Component behaviour in componentsNonOwnerOnly) {
                TrySetActive(behaviour, !IsOwner);
            }

            foreach(Transform t in transformsOwnerOnly) {
                t.gameObject.SetActive(IsOwner);
            }
            foreach(Transform t in transformsNonOwnerOnly) {
                t.gameObject.SetActive(!IsOwner);
            }
        }

        private void TrySetActive(Component component, bool val)
        {
            if(component is MonoBehaviour behaviour) {
                behaviour.enabled = val;
                return;
            }
            if(component is Collider collider) {
                collider.enabled = val;
                return;
            }

            // Otherwise we can't change the active state.
            Debug.LogError($"Cannot toggle Component state for {component.GetType()}. It is not a collider or MonoBehaviour.");
        }
    }

}
