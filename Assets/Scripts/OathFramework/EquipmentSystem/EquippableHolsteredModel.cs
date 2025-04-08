using UnityEngine;

namespace OathFramework.EquipmentSystem
{ 

    public class EquippableHolsteredModel : EquippableModel
    {
        [SerializeField] private HolsteredModelLocation location;

        protected override ModelPerspective Perspective => ModelPerspective.Holstered;
        public HolsteredModelLocation Location => location;
    }

}
