using OathFramework.Core;
using UnityEngine;

namespace OathFramework.UI
{
    public class ActivateScreenDimmer : MonoBehaviour
    {
        [SerializeField] private bool activateInMainMenu;
        
        protected void OnEnable()
        {
            if(!activateInMainMenu && Game.State != GameState.InGame)
                return;

            ScreenDimmer.Dimmers++;
        }

        protected void OnDisable()
        {
            if(!activateInMainMenu && Game.State != GameState.InGame)
                return;
            
            ScreenDimmer.Dimmers--;
        }
    }
}
