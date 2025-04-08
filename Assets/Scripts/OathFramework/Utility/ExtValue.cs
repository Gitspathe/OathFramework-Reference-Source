using System;
using System.Collections.Generic;

namespace OathFramework.Utility
{
    public sealed class ExtValue<T>
    {
        public uint Order { get; }
        public T Value    { get; set; }
        
        public ExtValue(uint order = 0, T value = default)
        {
            Order = order;
            Value = value;
        }
        
        public sealed class Handler
        {
            private List<ExtValue<T>> mods = new();
            
            public T DefaultVal { get; set; }

            public Handler(T defaultVal = default)
            {
                DefaultVal = defaultVal;
            }
            
            private void Sort() => mods.Sort((x, y) => x.Order.CompareTo(y.Order));

            public void Add(ExtValue<T> mod)
            {
                if(mods.Contains(mod))
                    return;
                
                mods.Add(mod);
                Sort();
            }

            public void Remove(ExtValue<T> mod)
            {
                if(!mods.Contains(mod))
                    return;
                
                mods.Remove(mod);
                Sort();
            }

            public T Value => mods.Count == 0 ? DefaultVal : mods[0].Value;
        }
    }
    
    public sealed class ExtBool
    {
        public uint Order { get; }
        public bool Value { get; set; }
        
        public ExtBool(bool value = false, uint order = 0)
        {
            Value = value;
            Order = order;
        }
        
        public sealed class Handler
        {
            // Object does not generate garbage here because it is never a struct.
            private object customCheckTarget;
            private Func<object, bool> customCheck;
            private List<ExtBool> mods = new();

            public bool DefaultVal { get; set; }

            private Handler() { }

            public static Handler CreateBasic(bool defaultVal = false) 
                => new() { DefaultVal = defaultVal };

            public static Handler CreateFunc<T>(T customCheckTarget, Func<T, bool> customCheck) where T : class 
                => new() { customCheckTarget = customCheckTarget, customCheck = obj => customCheck((T)obj) };

            private void Sort() => mods.Sort((x, y) => x.Order.CompareTo(y.Order));

            public void Add(ExtBool mod)
            {
                if(mods.Contains(mod))
                    return;
                    
                mods.Add(mod);
                Sort();
            }

            public void Remove(ExtBool mod)
            {
                if(!mods.Contains(mod))
                    return;
                    
                mods.Remove(mod);
                Sort();
            }
            
            public static implicit operator bool(Handler d) => d.Value;

            public bool Value {
                get {
                    if(mods.Count != 0)
                        return mods[0].Value;
                    
                    return customCheck?.Invoke(customCheckTarget) ?? DefaultVal;
                }
            }
        }
    }
}
