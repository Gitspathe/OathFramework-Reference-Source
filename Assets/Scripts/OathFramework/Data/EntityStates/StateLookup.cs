using OathFramework.Utility;

namespace OathFramework.Data.EntityStates
{
    public static class StateLookup
    {
        public static class Debug
        {
            public static LookupValue GodMode { get; } = new("core:debug_godmode", 10);
        }
        
        public static class Attributes
        {
            public static LookupValue Constitution { get; } = new("core:attribute_constitution", 100);
            public static LookupValue Endurance    { get; } = new("core:attribute_endurance",    101);
            public static LookupValue Agility      { get; } = new("core:attribute_agility",      102);
            public static LookupValue Strength     { get; } = new("core:attribute_strength",     103);
            public static LookupValue Expertise    { get; } = new("core:attribute_expertise",    104);
            public static LookupValue Intelligence { get; } = new("core:attribute_intelligence", 105);
        }

        public static class Offensive
        {
            public static LookupValue Stunned { get; } = new("core:status_stunned", 200);
        }

        public static class Defensive
        {
            public static LookupValue Invulnerable { get; } = new("core:invulnerable", 300);
        }
    }
}
