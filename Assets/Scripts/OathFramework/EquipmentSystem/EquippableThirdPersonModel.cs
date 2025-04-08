using UnityEngine;
using OathFramework.Pooling;
using OathFramework.Audio;
using OathFramework.EntitySystem.Players;
using OathFramework.Effects;

namespace OathFramework.EquipmentSystem
{

    public class EquippableThirdPersonModel : EquippableModel
    {
        [field: SerializeField] public ThirdPersonModelLocation Location { get; private set; }
        [field: SerializeField] public EquippableAnimSets AnimSet        { get; private set; }
        [field: SerializeField] public Transform ProjectileSpawnPoint    { get; private set; }
        
        [field: Header("IK Params")]
        
        [field: SerializeField] public Transform AimStartPoint           { get; private set; }
        [field: SerializeField] public Transform RightHandGoal           { get; private set; }
        [field: SerializeField] public Transform LeftHandGoal            { get; private set; }

        protected override ModelPerspective Perspective => ModelPerspective.ThirdPerson;
    }

}
