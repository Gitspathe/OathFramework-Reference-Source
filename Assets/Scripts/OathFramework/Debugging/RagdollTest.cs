using UnityEngine;
using OathFramework.Effects;
using System.Collections.Generic;

namespace OathFramework.Debugging
{
    public class RagdollTest : MonoBehaviour
    {
        [SerializeField] private float force;
        [SerializeField] private float radius;
        [SerializeField] private float upwardsForce;
        [SerializeField] private float distance = 1.0f;
        [SerializeField] private Transform location;
        [SerializeField] private GameObject prefab;

        public void DoExplode()
        {
            List<GameObject> gos = new();
            for(int i = 0; i < 10; i++) {
                gos.Add(Instantiate(prefab, new Vector3(i * distance, 3.0f, 0.0f), Random.rotation));
            }
            foreach(GameObject go in gos) {
                RagdollTarget t = go.GetComponentInChildren<RagdollTarget>(true);
                t.ApplyForce(force, radius, upwardsForce, location.position);
            }
            gos.Clear();
        }
    }
}
