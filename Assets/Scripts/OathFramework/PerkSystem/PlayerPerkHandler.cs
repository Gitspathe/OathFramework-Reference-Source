using OathFramework.EntitySystem.Players;
using OathFramework.Networking;
using OathFramework.Progression;
using OathFramework.Utility;

namespace OathFramework.PerkSystem
{
    public class PlayerPerkHandler : PerkHandler, INetworkBindHelperNode
    {
        private PlayerController controller;
        private QList<Perk> perks = new();
        
        public NetworkBindHelper Binder { get; set; }

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<PlayerController>();
        }

        public override void OnNetworkSpawn()
        {
            Binder.OnNetworkSpawnCallback(this);
            base.OnNetworkSpawn();
        }

        void INetworkBindHelperNode.OnBound()
        {
            if(!IsOwner || GlobalNetInfo.UsingSnapshot)
                return;
            
            Assign(controller.NetClient.Data.CurrentBuild);
        }

        public void Assign(in PlayerBuildData data)
        {
            ClearPerks(false);
            perks.Clear();
            for(int i = 0; i < 4; i++) {
                perks.Add(null);
            }
            AddPlayerPerk(data.perk1, 0);
            AddPlayerPerk(data.perk2, 1);
            AddPlayerPerk(data.perk3, 2);
            AddPlayerPerk(data.perk4, 3);
            return;

            void AddPlayerPerk(string perk, byte index)
            {
                if(string.IsNullOrEmpty(perk) || !PerkManager.TryGet(perk, out Perk ret))
                    return;

                AddPerk(ret, !IsOwner, false);
                perks.Array[index] = ret;
            }
        }

        public bool TryGetPerkAtIndex(byte index, out Perk perk)
        {
            perk = null;
            if(index >= perks.Count)
                return false;
            
            perk = perks.Array[index];
            return perk != null;
        }
    }
}
