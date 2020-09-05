﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayerListEntry.cs" company="Exit Games GmbH">
//   Part of: Asteroid Demo,
// </copyright>
// <summary>
//  Player List Entry
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;


using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Photon.Pun;



namespace GoFish
{
    public class PlayerListEntry : MonoBehaviour
    {
        [Header("UI References")]
        public Text PlayerNameText;

        public Image PlayerColorImage;
        public Button PlayerReadyButton;
        public Image PlayerReadyImage;

        private int ownerId;
        private bool isPlayerReady;

        #region UNITY

        public void OnEnable()
        {
            PlayerNumbering.OnPlayerNumberingChanged += OnPlayerNumberingChanged;
        }

        public void Start()
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != ownerId)
            {
                //still have to do 
                //PlayerReadyButton.gameObject.SetActive(false);
            }
            else
            {
                Hashtable initialProps = new Hashtable() { { Constants.PLAYER_READY, isPlayerReady } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
                PhotonNetwork.LocalPlayer.SetScore(0);

                //PlayerReadyButton.onClick.AddListener(() =>
                //{
                //    isPlayerReady = !isPlayerReady;
                //    SetPlayerReady(isPlayerReady);

                //    Hashtable props = new Hashtable() { { AsteroidsGame.PLAYER_READY, isPlayerReady } };
                //    PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                //    if (PhotonNetwork.IsMasterClient)
                //    {
                //        FindObjectOfType<LobbyMainPanel>().LocalPlayerPropertiesUpdated();
                //    }
                //});

            }
        }

        public void OnDisable()
        {
            PlayerNumbering.OnPlayerNumberingChanged -= OnPlayerNumberingChanged;
        }

        #endregion

        public void Initialize(int playerId, string playerName)
        {
            ownerId = playerId;
            PlayerNameText.text = playerName;
        }

        private void OnPlayerNumberingChanged()
        {
            foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == ownerId)
                {
                    
                }
            }
        }

        public void SetPlayerReady(bool playerReady)
        {
            
        }
    }
}