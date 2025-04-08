using OathFramework.Attributes;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace OathFramework.Editor
{

    [CustomPropertyDrawer(typeof(ArrayElementTitleAttribute))]
    public class ArrayElementTitleDrawer : PropertyDrawer
    {
        private SerializedProperty titleNameProp;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        protected virtual ArrayElementTitleAttribute Atribute
        {
            get {
                return (ArrayElementTitleAttribute)attribute;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.boxedValue is IArrayElementTitle titled) {
                label = new GUIContent(label) { text = titled.Name };
            } else {
                string FullPathName = property.propertyPath + "." + Atribute.VarName;
                titleNameProp = property.serializedObject.FindProperty(FullPathName);
                string newlabel = GetTitle();
                if (string.IsNullOrEmpty(newlabel)) {
                    newlabel = label.text;
                }
                label = new GUIContent(label) { text = newlabel };
            }
            EditorGUI.PropertyField(position, property, label, true);
        }

        private string GetTitle()
        {
            if (titleNameProp == null)
                return "NULL";

            switch (titleNameProp.propertyType) {
                case SerializedPropertyType.Generic:
                    break;
                case SerializedPropertyType.Integer:
                    return titleNameProp.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return titleNameProp.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return titleNameProp.floatValue.ToString();
                case SerializedPropertyType.String:
                    return titleNameProp.stringValue;
                case SerializedPropertyType.Color:
                    return titleNameProp.colorValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return titleNameProp.objectReferenceValue == null ? "NULL" : titleNameProp.objectReferenceValue.name;
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    return titleNameProp.enumNames == null || titleNameProp.enumNames.Length == 0
                        ? "NULL"
                        : Regex.Replace(titleNameProp.enumNames[titleNameProp.enumValueIndex], "(\\B[A-Z])", " $1");
                case SerializedPropertyType.Vector2:
                    return titleNameProp.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return titleNameProp.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return titleNameProp.vector4Value.ToString();
                case SerializedPropertyType.Rect:
                    break;
                case SerializedPropertyType.ArraySize:
                    break;
                case SerializedPropertyType.Character:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    break;
                case SerializedPropertyType.Bounds:
                    break;
                case SerializedPropertyType.Gradient:
                    break;
                case SerializedPropertyType.Quaternion:
                    break;
                default:
                    break;
            }
            return "";
        }
    }

}
