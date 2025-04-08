using OathFramework.UI.Animation;
using UnityEngine.UI;

namespace OathFramework.UI
{
    public class ExButton : Button
    {
        private UIAnimation[] anims;

        protected override void Awake()
        {
            base.Awake();
            anims = GetComponents<UIAnimation>();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if(anims == null)
                return;

            foreach(UIAnimation anim in anims) {
                anim.DoStateTransition((UIAnimation.SelectionState)state, instant);
            }
        }
    }
}
