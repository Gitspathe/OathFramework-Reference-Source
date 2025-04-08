using UnityEngine;

namespace OathFramework.Core
{
    
    public class UpdateManagerInit : MonoBehaviour
    {
        public UpdateManagerInit()
        {
            UpdateManager.OnInitialized = Register;
        }

        private void Register()
        {
            UpdateManager.AddLoopEntries(
                new UpdateLoopEntry("Time",                        GameUpdateOrder.Time),
                new UpdateLoopEntry("Entity Preprocessing",        GameUpdateOrder.EntityPreprocessing),
                new UpdateLoopEntry("Default",                     GameUpdateOrder.Default),
                new UpdateLoopEntry("Finish Entity Preprocessing", GameUpdateOrder.FinishEntityPreprocessing),
                new UpdateLoopEntry("Entity Update",               GameUpdateOrder.EntityUpdate),
                new UpdateLoopEntry("AI Processing",               GameUpdateOrder.AIProcessing),
                new UpdateLoopEntry("Effects",                     GameUpdateOrder.Effects),
                new UpdateLoopEntry("Finalize",                    GameUpdateOrder.Finalize));
        }
    }
    
    public static class GameUpdateOrder
    {
        public static int Time                      => 1;
        public static int EntityPreprocessing       => 10;
        public static int Default                   => 100;
        public static int FinishEntityPreprocessing => 200;
        public static int EntityUpdate              => 300;
        public static int AIProcessing              => 400;
        public static int Effects                   => 500;
        public static int Finalize                  => 10_000;
    }

}
