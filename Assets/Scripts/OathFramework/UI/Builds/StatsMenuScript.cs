using OathFramework.Networking;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Attributes;
using OathFramework.EntitySystem.Players;
using OathFramework.PerkSystem;
using OathFramework.Progression;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.UI.Builds
{ 

    public class StatsMenuScript : MonoBehaviour
    {
        [SerializeField] private List<StatNode> statElements = new();

        private static Entity PlayerDummyEntity    => PlayerManager.Instance.DummyPlayer.Entity;
        private static Entity AltPlayerDummyEntity => PlayerManager.Instance.AltDummyPlayer.Entity;

        public StatsMenuScript Initialize()
        {
            Tick(ref BuildMenuScript.CurBuildData);
            return this;
        }

        public void Tick(ref PlayerBuildData newBuild)
        {
            PlayerBuildData original = BuildMenuScript.Profile.CurrentLoadout;
            ApplyToDummy(AltPlayerDummyEntity, in original);
            ApplyToDummy(PlayerDummyEntity, in newBuild);
            foreach(StatNode statElement in statElements) {
                statElement.Tick(AltPlayerDummyEntity.CurStats, PlayerDummyEntity.CurStats);
            }
        }

        public void OnApply()
        {
            PlayerDummyEntity.CurStats.CopyTo(AltPlayerDummyEntity.CurStats);
        }

        private void ApplyToDummy(Entity dummy, in PlayerBuildData build)
        {
            // Step 1: Apply attributes.
            AttributeManager.Apply(in build, dummy.States, true);

            // Step 2: Apply perks.
            dummy.GetComponent<PlayerPerkHandler>().Assign(in build);

            // Step 3: Apply states.
            dummy.States.ApplyStats();
        }
    }

}
