using OathFramework.Utility;

namespace GameCode.MagitechRequiem.Data.States
{
    public static class StateLookup
    {
        public static class Status
        {
            public static LookupValue Shield   { get; } = new("core:shield", 1000);
            public static LookupValue GunBuff1 { get; } = new("core:gun_buff1", 1001);
        }

        public static class PerkStates
        {
            public static LookupValue Perk4State         { get; } = new("core:perk4state", 2000);
            public static LookupValue Perk5State         { get; } = new("core:perk5state", 2001);
            public static LookupValue Perk6State         { get; } = new("core:perk6state", 2002);
            public static LookupValue Perk8State         { get; } = new("core:perk8state", 2004);
            public static LookupValue Perk13State        { get; } = new("core:perk13state", 2005);
            public static LookupValue Perk14State        { get; } = new("core:perk14state", 2006);
            public static LookupValue Perk16State        { get; } = new("core:perk16state", 2007);
            public static LookupValue Perk16PassiveState { get; } = new("core:perk16passive_state", 2008);
            public static LookupValue Perk18State        { get; } = new("core:perk18state", 2009);
            public static LookupValue Perk20State        { get; } = new("core:perk20state", 2010);
            public static LookupValue Perk21State        { get; } = new("core:perk21state", 2011);
            public static LookupValue Perk22State        { get; } = new("core:perk22state", 2012);
            public static LookupValue Perk23State        { get; } = new("core:perk23state", 2012);
            public static LookupValue Perk26State        { get; } = new("core:perk26state", 2013);
        }
    }
}
