using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting;

namespace OathFramework.EntitySystem.Actions
{
    public abstract partial class MeleeAttack
    {
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
