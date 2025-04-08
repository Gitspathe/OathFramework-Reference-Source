using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Achievements
{
    [CreateAssetMenu(fileName = "Achievement Collection", menuName = "ScriptableObjects/Achievements/Achievement Collection", order = 1)]
    public class AchievementCollection : ScriptableObject
    {
        [field: SerializeField] public List<Achievement> Achievements { get; private set; }
    }
}
