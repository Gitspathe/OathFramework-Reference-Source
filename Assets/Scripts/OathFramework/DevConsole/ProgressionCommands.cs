using OathFramework.Progression;
using OathFramework.Utility;
using OathFramework.UI;
using QFSW.QC;
using UnityEngine;

namespace OathFramework.DevConsole
{
    public class ProgressionCommands : MonoBehaviour
    {
        [Command("show-exp-gain", "Shows the exp gain counter.", MonoTargetType.Singleton)]
        private void ShowExpGain()
        {
            HUDScript.ExpPopup.Show();
        }

        [Command("gain-exp", "Gain the specified number of exp.", MonoTargetType.Singleton)]
        private void GainExp(uint amt)
        {
            ProgressionManager.Profile.AddExp(amt);
        }
    }
}
