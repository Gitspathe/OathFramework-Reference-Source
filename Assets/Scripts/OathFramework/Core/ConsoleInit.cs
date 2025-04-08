using OathFramework.UI;
using UnityEngine;

namespace OathFramework.Core
{
    public class ConsoleInit : MonoBehaviour
    {
        private void Awake()
        {
            Game.LoadLaunchArgs();
            if(!Game.ConsoleEnabled)
                return;

            FindObjectOfType<GameUI>().CreateConsole();
            Debug.Log("*** OathFramework v1 ***\n");
        }
    }
}
