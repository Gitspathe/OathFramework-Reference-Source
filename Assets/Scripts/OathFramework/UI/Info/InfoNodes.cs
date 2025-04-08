using OathFramework.EquipmentSystem;
using OathFramework.Progression;
using OathFramework.UI.Builds;
using UnityEngine;

namespace OathFramework.UI.Info
{
    public static class InfoNodeFactory
    {
        public static EquippableParamNode CreateParamNode(UIEquippableParams param, Equippable template)
        {
            return new EquippableParamNode(param, template);
        }

        public static LevelDetailNode CreateLevelDetailNode(in PlayerBuildData buildData)
        {
            return new LevelDetailNode(buildData);
        }
    }
    
    public abstract class InfoNode
    {
        public abstract UIInfoNodes NodeType { get; }
        public abstract GameObject Display(Transform parent);
    }
    

    public class LevelDetailNode : InfoNode
    {
        public PlayerBuildData BuildData { get; private set; }
        public override UIInfoNodes NodeType => UIInfoNodes.LevelDetails;
        
        public LevelDetailNode(PlayerBuildData buildData)
        {
            BuildData = buildData;
        }

        public override GameObject Display(Transform parent)
        {
            GameObject go           = Object.Instantiate(UIInfoManager.Instance.UILevelDetailsGroupPrefab, parent);
            LevelDetailsGroup group = go.GetComponent<LevelDetailsGroup>();
            group.SetData(BuildData);
            return go;
        }
    }

    public class EquippableParamNode : InfoNode, INodeComparable<Equippable>
    {
        public Equippable Template      { get; private set; }
        public UIEquippableParams Param { get; private set; }
        public override UIInfoNodes NodeType => UIInfoNodes.EquippableParam;

        public EquippableParamNode(UIEquippableParams param, Equippable template)
        {
            Param    = param;
            Template = template;
        }
        
        public override GameObject Display(Transform parent)
        {
            return Display(null, parent);
        }

        public GameObject Display(Equippable obj, Transform parent)
        {
            GameObject go   = Object.Instantiate(UIInfoManager.Instance.UIInfoStatBarPrefab, parent);
            InfoStatBar bar = go.GetComponent<InfoStatBar>();
            bar.SetupEquippable(Param, Template, obj);
            return go;
        }
    }

    public class ToolParamNode : InfoNode
    {
        public UIToolParams Param { get; private set; }
        public override UIInfoNodes NodeType => UIInfoNodes.ToolParam;

        public ToolParamNode(UIToolParams param)
        {
            Param = param;
        }
        
        public override GameObject Display(Transform parent)
        {
            return null;
        }
    }

    public interface INodeComparable<in T>
    {
        GameObject Display(T comparison, Transform parent);
    }

    public enum UIInfoNodes
    {
        None            = 0,
        EquippableParam = 1,
        ToolParam       = 2,
        LevelDetails    = 3,
    }

    public enum UIEquippableParams
    {
        None            = 0,
        Damage          = 1,
        DamagePerSecond = 2,
        RateOfFire      = 3,
        ReloadSpeed     = 4,
        Accuracy        = 5,
        Handling        = 6,
        Penetration     = 7,
        Destruction     = 8
    }

    public enum UIToolParams
    {
        None = 0
    }
}
