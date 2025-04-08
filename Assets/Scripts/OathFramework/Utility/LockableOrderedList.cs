using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OathFramework.Utility
{
    public sealed class LockableOrderedList<T> where T : ILockableOrderedListElement
    {
        private List<T> toAdd    = new();
        private List<T> toRemove = new();
        private bool isLocked;
        private bool pendingSort;
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
            if(pendingSort) {
                Sort();
                pendingSort = false;
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T obj) => Current.Contains(obj) || toAdd.Contains(obj);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sort() => Current.Sort((x, y) => x.Order.CompareTo(y.Order));

        public bool AddUnique(T obj)
        {
            if(Contains(obj))
                return true;
            
            Add(obj);
            return false;
        }

        public void Add(T obj)
        {
            pendingSort = true;
            toRemove.Remove(obj);
            if(!isLocked) {
                Current.Add(obj);
                return;
            }
            isDirty = true;
            toAdd.Add(obj);
        }

        public void Remove(T obj)
        {
            pendingSort = true;
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
            pendingSort = true;
            if(isLocked) {
                isDirty = true;
                toRemove.Add(Current[index]);
                return;
            }
            Current.RemoveAt(index);
        }

        public void Clear()
        {
            pendingSort = true;
            if(isLocked) {
                isDirty = true;
                toRemove.AddRange(Current);
                return;
            }
            Current.Clear();
        }
        
        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Current.Count; }
    }
    
    public interface ILockableOrderedListElement
    {
        uint Order { get; }
    }
}
