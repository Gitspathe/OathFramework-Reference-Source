using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace OathFramework.Attributes
{
    [Preserve]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class StringDropdownAttribute : PropertyAttribute
    {
        public Type Type { get; private set; }

        public StringDropdownAttribute(Type type)
        {
            Type = type;
        }
    }
}
