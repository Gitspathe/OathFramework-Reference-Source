namespace OathFramework.EntitySystem
{
    public static class PersistenceLookup
    {
        public static class Name
        {
            public static string Equipment         => "core:entity_equipment";
            public static string Stagger           => "core:entity_stagger";
            public static string Flags             => "core:entity_flags";
            public static string EntityEffects     => "core:entity_effects";
            public static string Abilities         => "core:entity_abilities";
            public static string Perks             => "core:entity_perks";
            public static string Entity            => "core:entity";
            public static string Targeting         => "core:entity_targeting";
            public static string PlayerBinder      => "core:player_binder";
            public static string RaycastProjectile => "core:raycast_projectile";
            public static string Effect            => "core:effect";
        }
        
        public static class LoadOrder
        {
            public static uint Default           => 0;
            public static uint Equipment         => 60;
            public static uint Stagger           => 70;
            public static uint Flags             => 80;
            public static uint EntityEffects     => 90;
            public static uint Abilities         => 100;
            public static uint Perks             => 110;
            public static uint Entity            => 120;
            public static uint Targeting         => 130;
            public static uint PlayerBinder      => 140;
            public static uint RaycastProjectile => 200;
            public static uint Effect            => 210;
        }
    }
}
