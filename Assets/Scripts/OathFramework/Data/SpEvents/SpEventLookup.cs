using OathFramework.Utility;

namespace OathFramework.Data.SpEvents
{
    public static class SpEventLookup
    {
        public static class QuickHeal
        {
            public static LookupValue Trigger { get; } = new("core:quick_heal_invoke", 100);
        }

        public static class Effect
        {
            public static LookupValue ModelEffect { get; } = new("core:model_effect", 200);
        }

        public static class State
        {
            public static LookupValue ApplyState { get; } = new("core:apply_state", 300);
        }
    }
}
