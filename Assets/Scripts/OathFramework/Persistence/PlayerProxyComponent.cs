using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.Progression;

namespace OathFramework.Persistence
{
    public class PlayerProxyComponent : PersistentProxy
    {
        public string UniqueID           { get; private set; }
        public byte Index                { get; private set; }
        public PlayerBuildData BuildData { get; private set; }

        protected override void OnAssignData()
        {
            PlayerPersistenceBinder.Data data = (PlayerPersistenceBinder.Data)Components[PersistenceLookup.Name.PlayerBinder];
            UniqueID  = data.UniqueID;
            Index     = data.Index;
            BuildData = data.BuildData;
            ProxyDatabase<PlayerProxyComponent>.Register(UniqueID, this);
            PlayerManager.AssignPlayerProxyInfo(Index, UniqueID);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ProxyDatabase<PlayerProxyComponent>.Unregister(UniqueID);
        }
    }
}
