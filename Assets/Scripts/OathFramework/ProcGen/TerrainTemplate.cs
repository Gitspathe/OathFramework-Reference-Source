using UnityEngine;

namespace OathFramework.ProcGen
{
    [CreateAssetMenu(fileName = "Terrain Template", menuName = "ScriptableObjects/ProcGen/Terrain Template", order = 1)]
    public class TerrainTemplate : ScriptableObject
    {
        [field: SerializeField] public TerrainData Data { get; private set; }
    }
}
