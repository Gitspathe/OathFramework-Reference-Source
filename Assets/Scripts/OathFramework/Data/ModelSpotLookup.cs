namespace OathFramework.Data
{
    public static class ModelSpotLookup
    {
        public static class EquippableRanged
        {
            public const byte Root   = 0;
            public const byte Muzzle = 1;
            public const byte Barrel = 2;
            public const byte Handle = 3;
        }
        
        public static class Core
        {
            public const byte None  = 0;
            public const byte Root  = 1;
            public const byte Head  = 2;
            public const byte Torso = 3;
            public const byte Feet  = 4;
        }
        
        public static class Human
        {
            public const byte None       = 0;
            public const byte Root       = 1;
            public const byte Head       = 2;
            public const byte Torso      = 3;
            public const byte Feet       = 4;
            
            public const byte LowerTorso = 11;
            public const byte Neck       = 12;
            
            public const byte RShoulder  = 13;
            public const byte RUpperArm  = 14;
            public const byte RLowerArm  = 15;
            public const byte RHand      = 16;

            public const byte LShoulder  = 17;
            public const byte LUpperArm  = 18;
            public const byte LLowerArm  = 19;
            public const byte LHand      = 20;
            
            public const byte RHip       = 21;
            public const byte RUpperLeg  = 22;
            public const byte RLowerLeg  = 23;
            public const byte RAnkle     = 24;
            public const byte RFoot      = 25;
            
            public const byte LHip       = 26;
            public const byte LUpperLeg  = 27;
            public const byte LLowerLeg  = 28;
            public const byte LAnkle     = 29;
            public const byte LFoot      = 30;
        }
    }
}
