using OathFramework.Core;
using OathFramework.Networking;
using OathFramework.Progression;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace OathFramework.UI.Info
{
    public class PlayerInfo : LoopComponent, ILoopUpdate
    {
        [field: SerializeField] public Selectable Border { get; private set; }
        
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject connectingPanel;
        [SerializeField] private GameObject kickBtn;
        [SerializeField] private GameObject supporterEmblem;
        
        [Space(10)]
        
        [SerializeField] private TextMeshProUGUI nameText;

        [Space(10)]
        
        [SerializeField] private PlayerLevelText levelText;
        [SerializeField] private PlayerInfoItem primaryEquippableItem;
        [SerializeField] private PlayerInfoItem secondaryEquippableItem;
        [SerializeField] private PlayerInfoItem meleeEquippableItem;
        [SerializeField] private PlayerInfoItem magitech1Item;
        [SerializeField] private PlayerInfoItem magitech2Item;
        [SerializeField] private PlayerInfoItem perk1Item;
        [SerializeField] private PlayerInfoItem perk2Item;
        [SerializeField] private PlayerInfoItem perk3Item;
        [SerializeField] private PlayerInfoItem perk4Item;

        [Header("Localization")]
        [SerializeField] private LocalizedString kickConfirmMsgStr;

        private NetClient client;
        private PlayerInfoHolder holder;
        
        public void Setup(PlayerInfoHolder holder, NetClient client)
        {
            this.holder               = holder;
            this.client               = client;
            PlayerBuildData buildData = client.Data.CurrentBuild;
            StringBuilder sb          = StringBuilderCache.Retrieve;
            sb.Append(client.Name);
            nameText.SetText(sb);
            kickBtn.SetActive(NetworkManager.Singleton.IsServer && !client.IsOwner);
            supporterEmblem.SetActive(client.ShowSupporterBadge);
            
            levelText.SetData(client.Data.CurrentBuild);
            primaryEquippableItem.SetInfo(
                string.IsNullOrEmpty(buildData.equippable1) ? null : UIInfoManager.GetEquippableInfo(buildData.equippable1)
            );
            secondaryEquippableItem.SetInfo(
                string.IsNullOrEmpty(buildData.equippable2) ? null : UIInfoManager.GetEquippableInfo(buildData.equippable2)
            );
            meleeEquippableItem.SetInfo(
                null
            );
            magitech1Item.SetInfo(
                string.IsNullOrEmpty(buildData.ability1) ? null : UIInfoManager.GetAbilityInfo(buildData.ability1)
            );
            magitech2Item.SetInfo(
                string.IsNullOrEmpty(buildData.ability2) ? null : UIInfoManager.GetAbilityInfo(buildData.ability2)
            );
            perk1Item.SetInfo(
                string.IsNullOrEmpty(buildData.perk1) ? null : UIInfoManager.GetPerkInfo(buildData.perk1)
            );
            perk2Item.SetInfo(
                string.IsNullOrEmpty(buildData.perk2) ? null : UIInfoManager.GetPerkInfo(buildData.perk2)
            );
            perk3Item.SetInfo(
                string.IsNullOrEmpty(buildData.perk3) ? null : UIInfoManager.GetPerkInfo(buildData.perk3)
            );
            perk4Item.SetInfo(
                string.IsNullOrEmpty(buildData.perk4) ? null : UIInfoManager.GetPerkInfo(buildData.perk4)
            );
        }
        
        void ILoopUpdate.LoopUpdate()
        {
            bool connecting = NetGame.IsClientConnecting(client.OwnerClientId) || client.Data.PendingInitialSync;
            mainPanel.SetActive(!connecting);
            connectingPanel.SetActive(connecting);
        }

        private void OnDestroy()
        {
            holder.ClearInfo(client);
        }

        public void ClickedKick()
        {
            if(!NetworkManager.Singleton.IsServer || client.IsOwner)
                return;
            
            ModalConfig.Retrieve()
                .WithText(kickConfirmMsgStr)
                .WithButtons(new List<(LocalizedString, Action)> {
                    (UICommonMessages.Yes, () => { NetworkManager.Singleton.DisconnectClient(client.OwnerClientId); }),
                    (UICommonMessages.No,  () => { })
                })
                .WithInitButton(0)
                .Show();
        }
    }
}
