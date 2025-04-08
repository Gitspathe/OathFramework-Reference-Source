using OathFramework.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EntitySystem 
{ 

    public class EffectBoxController : MonoBehaviour, IEntityDieCallback
    {
        public EffectBoxPair[] effectBoxes;

        private Dictionary<int, EffectBox> eBoxDict = new();
        private Dictionary<int, EffectBox> active   = new();
        
        public Entity Entity { get; private set; }
        
        uint ILockableOrderedListElement.Order => 10_000;

        public void Initialize(Entity entity)
        {
            Entity = entity;
            eBoxDict.Clear();
            active.Clear();
            foreach(EffectBoxPair pair in effectBoxes) {
                eBoxDict.Add(pair.id, pair.hurtBox);
            }
        }
        
        public void OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityDieCallback)this);
        }

        public void SetupEffectBoxes(
            Entity entity,
            int[] ids, 
            in DamageValue damageVal, 
            EntityTeams[] targets,
            bool ignoreHitBoxMultipliers, 
            EffectBoxUnion union = null)
        {
            foreach(int id in ids) {
                if(!GetEffectBox(id, out EffectBox box))
                    continue;

                box.Setup(damageVal, targets, null, union, ignoreHitBoxMultipliers, entity);
            }
        }

        public bool GetEffectBox(int id, out EffectBox hurtBox)
        {
            hurtBox = null;
            return eBoxDict.TryGetValue(id, out hurtBox);
        }

        public void ActivateEffectBox(int id)
        {
            if(!eBoxDict.TryGetValue(id, out EffectBox hurtBox))
                return;

            hurtBox.Activate();
            active.TryAdd(id, hurtBox);
        }

        public void DeactivateEffectBox(int id)
        {
            if(!active.TryGetValue(id, out EffectBox hurtBox))
                return;

            hurtBox.Deactivate();
            active.Remove(id);
        }

        public void DeactivateEffectBoxes()
        {
            foreach(EffectBox hurtBox in active.Values) {
                hurtBox.Deactivate();
            }
            active.Clear();
        }

        void IEntityDieCallback.OnDie(Entity entity, in DamageValue lastDamageVal)
        {
            DeactivateEffectBoxes();
        }

        [Serializable]
        public class EffectBoxPair
        {
#if UNITY_EDITOR
            public string editorID;
#endif
            public int id;
            public EffectBox hurtBox;
        }
    }

}
