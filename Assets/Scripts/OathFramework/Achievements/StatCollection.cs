using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.Achievements
{
    [CreateAssetMenu(fileName = "Stat Collection", menuName = "ScriptableObjects/Achievements/Stat Collection", order = 1)]
    public class StatCollection : ScriptableObject
    {
        [field: SerializeField] public List<Stat> Stats { get; private set; }
    }
}
