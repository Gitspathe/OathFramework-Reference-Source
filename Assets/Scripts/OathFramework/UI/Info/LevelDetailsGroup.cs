using OathFramework.Progression;
using OathFramework.UI.Builds;
using UnityEngine;

namespace OathFramework.UI.Info
{
    public class LevelDetailsGroup : MonoBehaviour
    {
        [SerializeField] private InfoStatBar constitutionBar;
        [SerializeField] private InfoStatBar enduranceBar;
        [SerializeField] private InfoStatBar agilityBar;
        [SerializeField] private InfoStatBar strengthBar;
        [SerializeField] private InfoStatBar expertiseBar;
        [SerializeField] private InfoStatBar intelligenceBar;

        public void SetData(in PlayerBuildData data)
        {
            constitutionBar.SetupGeneric(data.constitution, 1, 25);
            enduranceBar.SetupGeneric(data.endurance, 1, 25);
            agilityBar.SetupGeneric(data.agility, 1, 25);
            strengthBar.SetupGeneric(data.strength, 1, 25);
            expertiseBar.SetupGeneric(data.expertise, 1, 25);
            intelligenceBar.SetupGeneric(data.intelligence, 1, 25);
        }
    }
}
