using System;
using UnityEngine;

namespace OathFramework.EntitySystem.Projectiles
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class GrenadeCollider : MonoBehaviour
    {
        [SerializeField] private GrenadeProjectile projectile;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void AddForce(Vector3 force)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
        
        private void OnCollisionEnter(Collision other)
        {
            projectile.CreateExplosion();
        }
    }
}
