using UnityEngine;

namespace OathFramework.Attributes
{ 

    public class ArrayElementTitleAttribute : PropertyAttribute
    {
        public string VarName { get; }

        public ArrayElementTitleAttribute(string elementTitleVar = "")
        {
            VarName = elementTitleVar;
        }
    }

    public interface IArrayElementTitle
    {
        public string Name { get; }
    }

}
