using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EntitySystem
{ 

    [RequireComponent(typeof(Collider))]
    public class HurtZone : MonoBehaviour
    {
        public ushort amount;
        public float delay = 1.0f;
        public bool affectsPlayer;
        public bool affectsEnemies;

        private List<(Entity, float)> damageTimers = new();

        private void Update()
        {
            for(int i = 0; i < damageTimers.Count; i++) {
                (Entity stats, float timer) = damageTimers[i];
                damageTimers[i]             = (stats, timer - Time.deltaTime);
                if(damageTimers[i].Item2 <= 0.0f) {
                    stats.Damage(new DamageValue(amount));
                    damageTimers[i] = (stats, delay);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Entity stats = other.GetComponent<Entity>();
            if(stats == null)
                return;

            foreach((Entity, float) pair in damageTimers) {
                if(!affectsPlayer && stats.IsPlayer)
                    return;
                if(!affectsEnemies && !stats.IsPlayer)
                    return;
                if(pair.Item1 == stats)
                    return;
            }

            damageTimers.Add((stats, 0.0f));
        }

        private void OnTriggerExit(Collider other)
        {
            Entity stats = other.GetComponent<Entity>();
            if(stats == null)
                return;

            for(int i = 0; i < damageTimers.Count; i++) {
                (Entity, float) pair = damageTimers[i];
                if(pair.Item1 == stats) {
                    damageTimers.RemoveAt(i);
                    return;
                }
            }
        }
    }

}
