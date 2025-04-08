using OathFramework.Audio;
using OathFramework.EntitySystem.Players;
using OathFramework.EquipmentSystem;
using OathFramework.Networking;
using UnityEngine;

namespace OathFramework.EntitySystem
{

    public interface IEntityControllerBase
    {
        Entity Entity     { get; }
        EntityModel Model { get; }
    }

    public interface IEntityController : IEntityControllerBase
    {
        EntityAudio Audio         { get; }
        EntityAnimation Animation { get; }
    }

    public interface IEquipmentUserController : IEntityController
    {
        EntityEquipment Equipment       { get; }
        ActionBlockHandler ActionBlocks { get; }
        float TimeSinceMoving           { get; }
        Transform AimTarget             { get; }
    }

    public interface IPlayerController : IEquipmentUserController
    {
        NetClient NetClient { get; }
    }

}
