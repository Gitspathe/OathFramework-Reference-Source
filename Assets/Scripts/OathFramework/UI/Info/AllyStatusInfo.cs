using OathFramework.EntitySystem.Players;
using OathFramework.Core;
using OathFramework.EntitySystem;
using OathFramework.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI.Info
{

    public class AllyStatusInfo : LoopComponent, ILoopLateUpdate
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Slider healthBar;

        private NetClient attached;
        
        public AllyStatusInfo Setup(NetClient client)
        {
            healthBar.value = 1.0f;
            attached        = client;
            nameText.SetText(client.Name);
            return this;
        }

        public void LoopLateUpdate()
        {
            if(attached.PlayerController == null || attached.PlayerController.IsDead) {
                healthBar.value = 0.0f;
                return;
            }

            Stats curStats = attached.PlayerController.Entity.CurStats;
            healthBar.value = (float)curStats.health / curStats.maxHealth;
        }
    }

}
