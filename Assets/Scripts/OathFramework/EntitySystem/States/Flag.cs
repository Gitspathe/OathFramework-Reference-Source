using System;
using UnityEngine;

namespace OathFramework.EntitySystem.States
{
    public abstract class Flag : IEquatable<Flag>
    {
        public abstract string LookupKey    { get; }
        public abstract ushort? DefaultID   { get; }
        public ushort ID                    { get; }
        public virtual bool NetSync         => false; // Owner -> Server/Client sync.
        public virtual bool PersistenceSync => false; // Save file sync.
        
        public Flag()
        {
            if(FlagManager.Register(this, out ushort netID)) {
                ID = netID;
                return;
            }
            Debug.LogError($"Failed to register Entity Flag of Type'{GetType()}'.");
        }
        
        public bool Equals(Flag other)
        {
            if(ReferenceEquals(null, other)) {
                return false;
            }
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) {
                return false;
            }
            return obj.GetType() == GetType() && Equals((Flag)obj);
        }
        
        public override int GetHashCode() => ID;
    }
}
