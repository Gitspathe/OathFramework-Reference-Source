using OathFramework.Utility;

namespace OathFramework.Data.StatParams
{
    public static class ParamLookup
    {
        public static class Progression
        {
            public static LookupValue ExpReward { get; } = new("core:exp_reward", 1000);
            public static LookupValue ExpMult   { get; } = new("core:exp_mult",   1001);
        }

        public static class Attack
        {
            public static LookupValue CriticalMult        { get; } = new("core:critical_mult",         100);
            public static LookupValue CriticalChance      { get; } = new("core:critical_chance",       101);
            public static LookupValue BaseDamage          { get; } = new("core:base_damage",           110);
            public static LookupValue DamageMult          { get; } = new("core:damage_mult",           111);
            public static LookupValue AttackSpeedMult     { get; } = new("core:attack_speed_mult",     120);
            public static LookupValue ReloadSpeedMult     { get; } = new("core:reload_speed_mult",     130);
            public static LookupValue SwapSpeedMult       { get; } = new("core:swap_speed_mult",       131);
            public static LookupValue AccuracyMult        { get; } = new("core:accuracy_mult",         140);
            public static LookupValue ProjectileSpeedMult { get; } = new("core:projectile_speed_mult", 150);
            public static LookupValue ExplosiveRangeMult  { get; } = new("core:explosive_range_mult",  160);
            public static LookupValue MaxRangeMult        { get; } = new("core:max_range_mult",        170);
        }

        public static class Stamina
        {
            public static LookupValue Regen      { get; } = new("core:stamina_regen",       200);
            public static LookupValue RegenDelay { get; } = new("core:stamina_regen_delay", 201);
        }

        public static class Evade
        {
            public static LookupValue StaminaUse   { get; } = new("core:dodge_stamina_use",   300);
            public static LookupValue SpeedMult    { get; } = new("core:dodge_speed_mult", 301);
            public static LookupValue DurationMult { get; } = new("core:dodge_duration_mult", 302);
            public static LookupValue IFramesMult  { get; } = new("core:dodge_iframes_mult",  303);
            public static LookupValue IFrames      { get; } = new("core:dodge_iframes",  304);
        }

        public static class Item
        {
            public static LookupValue UseSpeedMult { get; } = new("core:item_use_speed_mult", 400);
        }

        public static class QuickHeal
        {
            public static LookupValue Charges   { get; } = new("core:quick_heal_charges", 500);
            public static LookupValue Amount    { get; } = new("core:quick_heal_amount", 501);
            public static LookupValue SpeedMult { get; } = new("core:quick_heal_speed_mult", 502);
        }

        public static class Ability
        {
            public static LookupValue UseSpeedMult { get; } = new("core:ability_use_speed_mult", 600);
        }
    }
}
