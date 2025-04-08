using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OathFramework.Utility
{
    /// <summary>
    /// QuickList - a collection which exposes direct access to its inner array.
    /// Not thread safe.
    /// </summary>
    /// <typeparam name="T">Type of the inner array.</typeparam>
    public sealed class QList<T>
    {
        /// <summary>Inner array/buffer. Do not modify this directly!</summary>
        public T[] Array;

        private int countInternal;
        
        /// <summary>Elements length.</summary>
        // ReSharper disable once ConvertToAutoProperty
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => countInternal;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set => countInternal = value;
        }

        private int bufferLength; // Faster to cache the array length.

        /// <summary>
        /// NOTE: Directly accessing the Buffer is preferred, as that would be faster.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [Obsolete("It's preferred to access the inner array directly via the " + nameof(Array) + " field.")]
        public T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Array[index] = value;
        }

        /// <summary>
        /// Resets the length back to zero without clearing the items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => countInternal = 0;

        /// <summary>
        /// Clears the list of all it's items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            System.Array.Clear(Array, 0, Array.Length);
            countInternal = 0;
        }

        public QList<T> Copy()
        {
            QList<T> copy = new(countInternal);
            copy.AddRange(this);
            return copy;
        }

        /// <summary>
        /// Adds an item.
        /// </summary>
        /// <param name="item">The item.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Hot path.
        public void Add(T item)
        {
            if(countInternal < bufferLength) {
                Array[countInternal++] = item;
            } else {
                AddWithResize(item);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Cold path.
        private void AddWithResize(T item)
        {
            T[] newItems = new T[(bufferLength + 1) * 2];
            System.Array.Copy(Array, 0, newItems, 0, bufferLength);
            Array        = newItems;
            bufferLength = Array.Length - 1;
            Array[countInternal++] = item;
        }

        public void AddRange(QList<T> items)
        {
            EnsureAdditionalCapacity(items.countInternal);
            System.Array.Copy(items.Array, 0, Array, countInternal, items.countInternal);
            countInternal += items.countInternal;
        }

        public void AddRange(List<T> items)
        {
            EnsureAdditionalCapacity(items.Count);
            items.CopyTo(0, Array, countInternal, items.Count);
            countInternal += items.Count;
        }

        public void AddRange(T[] items)
        {
            EnsureAdditionalCapacity(items.Length);
            System.Array.Copy(items, 0, Array, countInternal, items.Length);
            countInternal += items.Length;
        }

        public void Trim(int maxSize)
        {
            if(Array.Length > maxSize) {
                System.Array.Resize(ref Array, maxSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Hot path.
        public void EnsureAdditionalCapacity(int additionalItems = 1)
        {
            if(countInternal + additionalItems >= bufferLength) {
                Grow(additionalItems);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Hot path.
        public void EnsureCapacity(int count)
        {
            if(bufferLength < count) {
                Grow(count - bufferLength);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Cold path.
        private void Grow(int additionalItems)
        {
            T[] newItems = new T[bufferLength + 1 + additionalItems];
            System.Array.Copy(Array, 0, newItems, 0, bufferLength);
            Array        = newItems;
            bufferLength = Array.Length;
        }

        /// <summary>
        /// Adds an enumerable range of items.
        /// </summary>
        /// <param name="enumerable">IEnumerable collection.</param>
        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach(T item in enumerable) {
                Add(item);
            }
        }

        /// <summary>
        /// Sorts the list with a given comparer.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        public void Sort(IComparer<T> comparer)
            => System.Array.Sort(Array, 0, countInternal, comparer);

        /// <summary>
        /// Checks if an item is present in the list. Uses the default equality comparer to test.
        /// </summary>
        /// <param name="item">Item to check.</param>
        /// <returns>True if the item is present, false otherwise.</returns>
        public bool Contains(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for(int i = 0; i < countInternal; ++i) {
                if(comparer.Equals(item, Array[i])) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Inserts an item at a given index.
        /// </summary>
        /// <param name="index">Index to insert.</param>
        /// <param name="item">Item to insert.</param>
        public void Insert(int index, T item)
        {
            if(countInternal == Array.Length) {
                EnsureAdditionalCapacity();
            }
            if(index < countInternal) {
                System.Array.Copy(Array, index, Array, index + 1, countInternal - index);
            }
            Array[index] = item;
            countInternal++;
        }

        /// <summary>
        /// Removes an element at a specific index.
        /// </summary>
        /// <param name="index">Index of item to remove.</param>
        public void RemoveAt(int index)
        {
            countInternal--;
            if(index < countInternal)
                System.Array.Copy(Array, index + 1, Array, index, countInternal - index);

            Array[countInternal] = default;
        }

        /// <summary>
        /// Removes an item from the list.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True if the item was present and was removed, false otherwise.</returns>
        public bool Remove(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for(int i = 0; i < countInternal; ++i) {
                if(comparer.Equals(item, Array[i])) {
                    RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Finds the index of an item. Returns -1 if the item isn't found.
        /// </summary>
        /// <param name="item">Item to find the index of.</param>
        /// <returns>Index of the element, if found. If not found, returns -1.</returns>
        public int IndexOf(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for(int i = 0; i < countInternal; ++i) {
                if(comparer.Equals(item, Array[i])) {
                    RemoveAt(i);
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Creates a new QuickList instance.
        /// </summary>
        /// <param name="size">Initial capacity.</param>
        public QList(int size)
        {
            size         = Math.Max(1, size);
            Array        = new T[size];
            bufferLength = Array.Length;
        }

        public QList() : this(8) { }

        public QList(T[] array) : this(8)
        {
            Array = array;
            int i = 0;
            while(i < array.Length && array[i] != null) {
                i++;
            }
            countInternal = i;
            bufferLength = array.Length;
        }
    }
}
