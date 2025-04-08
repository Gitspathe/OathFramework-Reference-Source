using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OathFramework.Utility
{
    public sealed class LockableList<T>
    {
        private List<T> toAdd    = new();
        private List<T> toRemove = new();
        private bool isLocked;
        private bool isDirty;

        public List<T> Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] 
            get; private set;
        } = new();
        
        public T this[int i] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current[i];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Current[i] = value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lock()
        {
            isLocked = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unlock()
        {
            if(!isDirty) {
                isLocked = false;
                return;
            }
            foreach(T obj in toAdd) {
                Current.Add(obj);
            }
            foreach(T obj in toRemove) {
                Current.Remove(obj);
            }
            toAdd.Clear();
            toRemove.Clear();
            isLocked = false;
            isDirty  = false;
        }

        public void Add(T obj)
        {
            toRemove.Remove(obj);
            if(!isLocked) {
                isDirty = true;
                Current.Add(obj);
                return;
            }
            toAdd.Add(obj);
        }

        public void Remove(T obj)
        {
            toAdd.Remove(obj);
            if(!isLocked) {
                Current.Remove(obj);
                return;
            }
            isDirty = true;
            toRemove.Add(obj);
        }

        public void RemoveAt(int index)
        {
            if(isLocked) {
                isDirty = true;
                toRemove.Add(Current[index]);
                return;
            }
            Current.RemoveAt(index);
        }

        public void Clear()
        {
            if(isLocked) {
                isDirty = true;
                toRemove.AddRange(Current);
                return;
            }
            Current.Clear();
        }
        
        public bool Contains(T obj) => Current.Contains(obj);
        
        public int Count => Current.Count;
    }
}
