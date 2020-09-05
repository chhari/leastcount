using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.U2D;

namespace GoFish
{
    public class Lobby : MonoBehaviourPunCallbacks
    {
        public enum LobbyState
        {
            Default,
            JoinedRoom,
        }
        public LobbyState State = LobbyState.Default;
        public bool Debugging = false;

        string gameVersion = "1";
        
        public GameObject PopoverBackground;
        public GameObject EnterNicknamePopover;
        public GameObject WaitForOpponentPopover;
        public GameObject StartRoomButton;
        public InputField NicknameInputField;

        public GameObject Player1Portrait;
        public GameObject Player2Portrait;

        private Dictionary<int, GameObject> playerListEntries;

        string nickname;

        private void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void Start()
        {
            // disable all online UI elements
            HideAllPopover();
            Connect();
            
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log("Failed Creating  in");          
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            string roomName = "Room " + Random.Range(1000, 10000);
            Debug.Log("roomname" + roomName);

            RoomOptions options = new RoomOptions { MaxPlayers = 2 };

            PhotonNetwork.CreateRoom(roomName, options, null);
        }

        public override void OnCreatedRoom()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("OnPlayerEnteredRoom");
                ShowJoinedRoomPopover();
                ShowReadyOnePlayer();

            }
        }

        public override void OnJoinedRoom()
        {
            

            if (playerListEntries == null)
            {
                playerListEntries = new Dictionary<int, GameObject>();
            }

            foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
            {
                //GameObject entry = Instantiate(PlayerListEntryPrefab);
                //entry.transform.SetParent(InsideRoomPanel.transform);
                //entry.transform.localScale = Vector3.one;
                //entry.GetComponent<PlayerListEntry>().Initialize(p.ActorNumber, p.NickName);

                object isPlayerReady;
                if (p.CustomProperties.TryGetValue(Constants.PLAYER_READY, out isPlayerReady))
                {
                    //Add any logic you want
                }
                // Use this var if needed
                //playerListEntries.Add(p.ActorNumber, entry);
            }


            bool useVar = CheckPlayersReady();
            if (useVar)
            {
                ShowJoinedRoomPopover();
                GetPlayersInTheRoom();
            }
            //Use Custom Props
            //Hashtable props = new Hashtable
            //{
            //    {Constants.PLAYER_LOADED_LEVEL, false}
            //};
            //PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public override void OnLeftRoom()
        {
            //to clear local variables ,so that when in enters again we dont have to work
            foreach (GameObject entry in playerListEntries.Values)
            {
                Destroy(entry.gameObject);
            }

            playerListEntries.Clear();
            playerListEntries = null;
        }

        //not let players enter after a time

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("OnPlayerEnteredRoom");
                ShowJoinedRoomPopover();
                GetPlayersInTheRoom();
            }
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            Destroy(playerListEntries[otherPlayer.ActorNumber].gameObject);
            playerListEntries.Remove(otherPlayer.ActorNumber);
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
            {
                StartRoomButton.gameObject.SetActive(CheckPlayersReady());
            }
        }


        private bool CheckPlayersReady()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return false;
            }

            foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
            {
                object isPlayerReady;
                if (p.CustomProperties.TryGetValue(Constants.PLAYER_READY, out isPlayerReady))
                {
                    if (!(bool)isPlayerReady)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            if (playerListEntries == null)
            {
                playerListEntries = new Dictionary<int, GameObject>();
            }

            GameObject entry;
            if (playerListEntries.TryGetValue(targetPlayer.ActorNumber, out entry))
            {
                object isPlayerReady;
                if (changedProps.TryGetValue(Constants.PLAYER_READY, out isPlayerReady))
                {
                    entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool)isPlayerReady);
                }
            }

            StartRoomButton.gameObject.SetActive(CheckPlayersReady());
        }

        void ShowEnterNicknamePopover()
        {
            PopoverBackground.SetActive(true);
            EnterNicknamePopover.SetActive(true);
        }

        void ShowJoinedRoomPopover()
        {
            EnterNicknamePopover.SetActive(false);
            WaitForOpponentPopover.SetActive(true);
            StartRoomButton.SetActive(false);
            Player1Portrait.SetActive(false);
            Player2Portrait.SetActive(false);
        }

        void ShowReadyOnePlayer()
        {
            //StartRoomButton.SetActive(false);
            Player1Portrait.SetActive(true);
            //Player2Portrait.SetActive(true);
        }

        void ShowReadyTwoPlayer()
        {
            //StartRoomButton.SetActive(false);
            Player1Portrait.SetActive(true);
            Player2Portrait.SetActive(true);
        }

        void ShowReadyToStartUI()
        {
            StartRoomButton.SetActive(true);
            Player1Portrait.SetActive(true);
            Player2Portrait.SetActive(true);
        }

        void HideAllPopover()
        {
            PopoverBackground.SetActive(false);
            EnterNicknamePopover.SetActive(false);
            WaitForOpponentPopover.SetActive(false);
            StartRoomButton.SetActive(false);
            Player1Portrait.SetActive(false);
            Player2Portrait.SetActive(false);
        }

        //****************** UI event handlers *********************//
        /// <summary>
        /// Practice button was clicked.
        /// </summary>
        public void OnPracticeClicked()
        {
            Debug.Log("OnPracticeClicked");
            SceneManager.LoadScene("GameScene");
        }

        /// <summary>
        /// Online button was clicked.
        /// </summary>
        public void OnOnlineClicked()
        {
            Debug.Log("OnOnlineClicked");
            ShowEnterNicknamePopover();
            
        }

        /// <summary>
        /// Cancel button in the popover was clicked.
        /// </summary>
        public void OnCancelClicked()
        {
            Debug.Log("OnCancelClicked");

            if (State == LobbyState.JoinedRoom)
            {
                // TODO: leave room.
            }

            HideAllPopover();
        }

        

        /// <summary>
        /// Start button in the WaitForOpponentPopover was clicked.
        /// </summary>
        public void OnStartRoomClicked()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                PhotonNetwork.LoadLevel("GameScene");
            }
        }

        public void Connect()
        {
            // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
            
                // #Critical, we must first and foremost connect to Photon Online Server.
                Debug.Log("came else ");
                PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = gameVersion;
           
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
        }

        public void onCreateRoomButton()
        {
            if (PhotonNetwork.IsConnected)
            {
                int r = Random.Range(2000, 5000);
                NicknameInputField.text = r.ToString();
                Debug.Log("roomName" + r.ToString());
                PhotonNetwork.CreateRoom(r.ToString(), new RoomOptions { MaxPlayers = 4 });
            }
            else
            {
                Debug.Log("not connected");

            }
        }

        public void onJoinRoomButton(){
            string input = NicknameInputField.text;
            PhotonNetwork.JoinRoom(input);

        }

        //work on both methods
        public void OnJoinRandomRoomButtonClicked()
        {
            PhotonNetwork.JoinRandomRoom();
        }

        public void OnLeaveGameButtonClicked()
        {
            PhotonNetwork.LeaveRoom();
        }

        //avatar settings maybe
        //public void OnLoginButtonClicked()
        //{
        //    string playerName = PlayerNameInput.text;

        //    if (!playerName.Equals(""))
        //    {
        //        PhotonNetwork.LocalPlayer.NickName = playerName;
        //        PhotonNetwork.ConnectUsingSettings();
        //    }
        //    else
        //    {
        //        Debug.LogError("Player Name is invalid.");
        //    }
        //}


        void GetPlayersInTheRoom()
        {
            
              Player1Portrait.SetActive(true);
              Player2Portrait.SetActive(true);

              if (PhotonNetwork.IsMasterClient)
              {
                   ShowReadyToStartUI();
              }
              else{
                    Debug.Log("Failed to get players " );
             }
            
        }

        
        /// <summary>
        /// Ok button in the EnterNicknamePopover was clicked.
        /// </summary>
        public void OnConfirmNicknameClicked()
        {
            nickname = NicknameInputField.text;
            Debug.Log($"OnConfirmNicknameClicked: {nickname}");

            if (Debugging)
            {
                ShowJoinedRoomPopover();
                ShowReadyOnePlayer();
                Connect();
            }
            else
            {

                //TODO: Use nickname as player custom id to check into SocketWeaver.
            }
        }
    }
}
