using OathFramework.Core;
using UnityEngine;

namespace OathFramework.UI
{
    public class MiniChat : LoopComponent, ILoopUpdate
    {
        [SerializeField] private GameObject mainPanel;
        
        void ILoopUpdate.LoopUpdate()
        {
            mainPanel.SetActive(!LeaderboardUIScript.IsOpen && Game.State == GameState.InGame);
        }
    }
}
