using System;
using UnityEngine;

namespace OathFramework.Achievements
{
    [Serializable]
    public class Stat
    {
        [field: SerializeField] public string ID      { get; private set; }
        [field: SerializeField] public string Name    { get; private set; }
        [field: SerializeField] public int DefaultVal { get; private set; }
        [field: SerializeField] public int MinVal     { get; private set; }
        [field: SerializeField] public int MaxVal     { get; private set; } = int.MaxValue;
    }
}
