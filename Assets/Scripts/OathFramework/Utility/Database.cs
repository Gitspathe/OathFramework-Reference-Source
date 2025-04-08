using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace OathFramework.Utility
{

    public abstract class Database<TKey, TValue>
    {
        private Dictionary<TKey, TValue> Data { get; set; } = new();

        public bool Register(TKey key, TValue value)
        {
            if(Data.TryAdd(key, value))
                return false;
            
            Debug.LogError($"Key {key} already present in Database.");
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(TKey key, out TValue value)
        {
            value = default;
            return key != null && Data.TryGetValue(key, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key) => Data.ContainsKey(key);
    }

    public abstract class Database<TKey, TID, TValue> where TID : struct
    {
        private bool first = true;

        private Dictionary<TKey, (TValue val, TID id)> DataByKey { get; } = new();
        private Dictionary<TID, (TValue val, TKey key)> DataByID { get; } = new();
        public TID CurrentID                                     { get; protected set; }
        protected abstract TID StartingID                        { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void IncrementID();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool IsIDLarger(TID current, TID comparison);

        public bool Register(TKey key, TValue value, out TID id)
        {
            if(first) {
                CurrentID = StartingID;
                first     = false;
            }
            
            TID old  = CurrentID;
            bool val = RegisterWithID(key, value, CurrentID);
            id       = CurrentID;
            if(val && !IsIDLarger(old, CurrentID)) {
                IncrementID();
            }
            return val;
        }
        
        public bool RegisterWithID(TKey key, TValue value, TID id)
        {
            if(key == null || DataByID.ContainsKey(id))
                return false;
            
            if(first) {
                CurrentID = StartingID;
                first     = false;
            }
            if(IsIDLarger(CurrentID, id)) {
                CurrentID = id;
                IncrementID();
            }
            if(!DataByKey.TryAdd(key, (value, id)))
                return false;

            DataByID.Add(id, (value, key));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key) => DataByKey.ContainsKey(key);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsID(TID netID) => DataByID.ContainsKey(netID);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(TKey key, out TValue value, out TID id)
        {
            if(key == null || !DataByKey.TryGetValue(key, out (TValue retVal, TID retID) ret)) {
                id    = default;
                value = default;
                return false;
            }

            value = ret.retVal;
            id    = ret.retID;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(TID id, out TValue value, out TKey key)
        {
            if(!DataByID.TryGetValue(id, out (TValue retVal, TKey retKey) ret)) {
                value = default;
                key   = default;
                return false;
            }

            value = ret.retVal;
            key   = ret.retKey;
            return true;
        }
    }

}
