using OathFramework.EntitySystem;
using OathFramework.PerkSystem;
using System.Collections.Generic;

namespace GameCode.MagitechRequiem.Data.Perks
{
    /// <summary>
    /// Overheal
    /// Increase magitech healing by 30%.
    /// </summary>
    public class Perk1 : Perk
    {
        public override string LookupKey => PerkLookup.Perk1.Key;
        public override ushort? DefaultID => PerkLookup.Perk1.DefaultID;

        public override Dictionary<string, string> GetLocalizedParams(Entity entity) 
            => new(){ {"amt", "30%"} };

        public static Perk1 Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
    }

}
