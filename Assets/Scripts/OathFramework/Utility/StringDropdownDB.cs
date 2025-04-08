using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace OathFramework.Utility
{
    
#if UNITY_EDITOR
    [Preserve]
    public static class StringDropdownDB
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Preserve]
        public static IEnumerable GetValues<T>() where T : IStringDropdownValue
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;
            Type openGeneric         = typeof(StringDropdownDB<>);
            Type closedGeneric       = openGeneric.MakeGenericType(typeof(T));
            MethodInfo method        = closedGeneric.GetMethod(nameof(StringDropdownDB<IStringDropdownValue>.GetValues), flags);
            return (ValueDropdownList<string>)method.Invoke(null, Array.Empty<object>());
        }
    }
    
    [Preserve]
    public static class AssetStringDropdownDB
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Preserve]
        public static IEnumerable GetValues<T>() where T : Object, IStringDropdownValue
        {
            const BindingFlags flags         = BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;
            Type               openGeneric   = typeof(AssetStringDropdownDB<>);
            Type               closedGeneric = openGeneric.MakeGenericType(typeof(T));
            MethodInfo         method        = closedGeneric.GetMethod(nameof(AssetStringDropdownDB<T>.GetValues), flags);
            return (ValueDropdownList<string>)method.Invoke(null, Array.Empty<object>());
        }
    }
    
    [Preserve]
    public static class StringDropdownDB<T> where T : IStringDropdownValue
    {
        private static Dictionary<string, string> values = new();
        private static bool initialized;

        // ReSharper disable once UnusedMember.Global
        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Preserve]
        public static ValueDropdownList<string> GetValues() 
        {
            Initialize();
            ValueDropdownList<string> list = new();
            foreach(KeyValuePair<string,string> val in values) {
                list.Add(new ValueDropdownItem<string>(val.Key, val.Value));
            }
            return list;
        }
        
        [Preserve]
        public static void Initialize()
        {
            if(initialized)
                return;

            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type baseType = typeof(T);
                Type[] types  = assembly.GetTypes().Where(t => t != baseType && baseType.IsAssignableFrom(t)).Distinct().ToArray();
                foreach(Type type in types) {
                    IStringDropdownValue val = (IStringDropdownValue)Activator.CreateInstance(type);
                    values.Add(val.DropdownVal, val.TrueVal);
                    if(val is Object obj) {
                        Object.Destroy(obj);
                    }
                }
            }
            initialized = true;
        }
    }
    
    [Preserve]
    public static class AssetStringDropdownDB<T> where T : Object, IStringDropdownValue
    {
        private static Dictionary<string, string> values = new();

        // ReSharper disable once UnusedMember.Global
        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Preserve]
        public static ValueDropdownList<string> GetValues() 
        {
            Initialize();
            ValueDropdownList<string> list = new();
            foreach(KeyValuePair<string,string> val in values) {
                list.Add(new ValueDropdownItem<string>(val.Key, val.Value));
            }
            return list;
        }
        
        [Preserve]
        public static void Initialize()
        {
            values.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach(string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset     = AssetDatabase.LoadAssetAtPath<T>(path);
                if(asset != null) {
                    values.Add(asset.DropdownVal, asset.TrueVal);
                }
            }
        }
    }
#endif
    
    public interface IStringDropdownValue
    {
        string DropdownVal { get; }
        string TrueVal     { get; }
    }
}
