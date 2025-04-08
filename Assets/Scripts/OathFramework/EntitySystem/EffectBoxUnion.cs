using System;
using OathFramework.Utility;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting;

namespace OathFramework.EntitySystem
{
    [Serializable]
    public class EffectBoxUnion : MonoBehaviour
    {
        [field: SerializeField] public byte ID { get; private set; }
        
        [field: SerializeField, ValueDropdown("GetEditorEffectBoxes")] 
        public int[] EffectBoxes { get; private set; }

        private QList<IEntity> hits = new();

        public bool CheckHit(IEntity entity)
        {
            for(int i = 0; i < hits.Count; i++) {
                if(hits.Array[i] == entity)
                    return true;
            }
            return false;
        }

        public void RegisterHit(IEntity entity)
        {
            hits.Add(entity);
        }
        
        public void ResetUnion()
        {
            hits.Clear();
        }
        
        [Preserve]
        private IEnumerable GetEditorEffectBoxes()
        {
            ValueDropdownList<int> vals = new();
#if UNITY_EDITOR
            Transform t = FindMain();
            if(t == null) {
                Debug.LogError("Failed to display HurtBox selector, main object not found.");
                return vals;
            }

            EffectBoxController controller = t.GetComponentsInChildren<EffectBoxController>()[0];
            if(controller == null) {
                Debug.LogError("Failed to display HurtBox selector, couldn't find HurtBoxController.");
                return vals;
            }

            foreach(EffectBoxController.EffectBoxPair hurtBox in controller.effectBoxes) {
                vals.Add(new ValueDropdownItem<int>(hurtBox.editorID, hurtBox.id));
            }
#endif
            return vals;

#if UNITY_EDITOR
            Transform FindMain()
            {
                Transform t = transform;
                while(true) {
                    if(t.GetComponent<Entity>() != null)
                        return t;
                    if(t.parent == null)
                        return null;

                    t = t.parent;
                }
            }
#endif
        }
    }
}
