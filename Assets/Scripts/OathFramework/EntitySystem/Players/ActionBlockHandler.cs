using System.Collections.Generic;

namespace OathFramework.EntitySystem.Players
{
    public class ActionBlockHandler
    {
        private readonly HashSet<uint> curBlockerIDs = new();

        public void Add(uint blocker)      => curBlockerIDs.Add(blocker);
        public void Remove(uint blocker)   => curBlockerIDs.Remove(blocker);
        public bool Contains(uint blocker) => curBlockerIDs.Contains(blocker);
        public void Clear()                => curBlockerIDs.Clear();
    }
    
    public static class ActionBlockers
    {
        public static uint Stagger    => 10;
        public static uint Dodge      => 11;
        public static uint AbilityUse => 12;
    }
}
