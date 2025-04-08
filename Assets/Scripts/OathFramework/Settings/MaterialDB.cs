using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace OathFramework.Settings
{
    public class MaterialDB : ScriptableObject
    {
        [HideInInspector] public List<MaterialsNode> materialNodes = new();
        private Dictionary<string, List<Material>> materials       = new();

        public void Initialize()
        {
            materials.Clear();
            foreach(MaterialsNode node in materialNodes) {
                materials.Add(node.scenePath, node.materials);
            }
        }
        
        public bool TryGetMaterials(string sceneName, out List<Material> foundMats)
            => materials.TryGetValue(sceneName, out foundMats);

        public List<Material> GetAllMaterials()
        {
            List<Material> mats = new();
            foreach(MaterialsNode node in materialNodes) {
                mats.AddRange(node.materials);
            }
            return mats;
        }
    }

    [System.Serializable]
    public class MaterialsNode
    {
        public string scenePath;
        public int sceneBuildIndex;
        public List<Material> materials;
    }
    
    public interface IMaterialPreloaderDataProvider
    {
        Material[] GetMaterials();
    }
}
