using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OathFramework.Utility
{
    public sealed class LockableHashSet<T>
    {
        private HashSet<T> toAdd    = new();
        private HashSet<T> toRemove = new();
        private bool isLocked;
        private bool isDirty;

        public HashSet<T> Current { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new();
        
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

        public bool Add(T obj)
        {
            toRemove.Remove(obj);
            if(!isLocked) {
                return Current.Add(obj);
            }
            isDirty  = true;
            bool ret = !Contains(obj);
            return toAdd.Add(obj) && ret;
        }

        public bool Remove(T obj)
        {
            toAdd.Remove(obj);
            if(!isLocked) {
                return Current.Remove(obj);
            }
            isDirty = true;
            toRemove.Add(obj);
            return Current.Contains(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T obj) => Current.Contains(obj) || toAdd.Contains(obj);

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Current.Count; }
    }
}
