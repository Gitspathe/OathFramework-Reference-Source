using OathFramework.Extensions;
using OathFramework.Core.Service;
using UnityEngine;

namespace OathFramework.Core
{

    [RequireComponent(typeof(BoxCollider))]
    public class PlayerSpawn : MonoBehaviour
    {
        private BoxCollider boxArea;

        public PlayerSpawnService Service => GameServices.PlayerSpawn;

        private void Awake()
        {
            boxArea = GetComponent<BoxCollider>();
        }

        public Vector3 GetRandomPointInsideCollider()
        {
            Vector3 extents = boxArea.size / 2f;
            Vector3 point = new(
                Random.Range(-extents.x, extents.x),
                0.0f,
                Random.Range(-extents.z, extents.z)
            );

            return boxArea.transform.TransformPoint(point);
        }

        public bool GetRandomSpawnPoint(out Vector3 point)
        {
            point = Vector3.zero;
            bool foundSpawn = false;
            int currentAttempts = 0;
            while(!foundSpawn && currentAttempts < Service.MaxAttempts) {
                Vector3 randPoint = GetRandomPointInsideCollider();
                Ray ray = new(randPoint, Vector3.down);
                if(Physics.Raycast(ray, out RaycastHit hit, Service.SpawnRaycastDistance, Service.SpawnCheckMask, QueryTriggerInteraction.Ignore)) {
                    if(Service.SpawnBlockMask.ContainsLayer(hit.collider.gameObject.layer)) {
                        currentAttempts++;
                        continue;
                    }
                    point = new Vector3(randPoint.x, hit.point.y + Service.PlayerHeight, randPoint.z);
                    return true;
                }
                currentAttempts++;
            }
            return false;
        }
    }

}
