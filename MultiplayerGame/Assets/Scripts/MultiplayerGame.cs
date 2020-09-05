using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoFish;
using Unity;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using ExitGames.Client.Photon;

namespace GoFish { 

    public class MultiplayerGame : MonoBehaviourPunCallbacks
    {
        public Text MessageText;

        CardAnimator cardAnimator;

        public GameDataManager gameDataManager;

        public List<Transform> PlayerPositions = new List<Transform>();
        public List<Transform> BookPositions = new List<Transform>();

        MyPlayer localPlayer;
        MyPlayer remotePlayer;

        MyPlayer currentTurnPlayer;
        MyPlayer currentTurnTargetPlayer;

        PlayerMove move;

        Card selectedCard;
        Ranks selectedRank;

        public override void OnEnable()
        {
            base.OnEnable();

            CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
        }

        public override void OnDisable()
        {
            base.OnDisable();

            CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;
        }

        public enum GameState
        {
            Idel,
            GameStarted,
            TurnStarted,
            TurnSelectingNumber,
            TurnConfirmedSelectedNumber,
            TurnWaitingForOpponentConfirmation,
            TurnOpponentConfirmed,
            TurnGoFish,
            GameFinished
        };

        public GameState gameState = GameState.Idel;

        public virtual void Awake()
        {
            foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
            {
                if (p.IsLocal)
                {
                    localPlayer = new MyPlayer();
                    localPlayer.PlayerId = p.ActorNumber.ToString();
                    localPlayer.PlayerName = p.NickName;
                    localPlayer.Position = PlayerPositions[0].position;
                }
                else {
                    remotePlayer = new MyPlayer();
                    remotePlayer.PlayerId = p.ActorNumber.ToString();
                    remotePlayer.PlayerName = p.NickName;
                    remotePlayer.Position = PlayerPositions[1].position;
                }
                gameDataManager = new GameDataManager(localPlayer, remotePlayer);

            }

            //remotePlayer = new Player();
            //remotePlayer.PlayerId = "offline-bot";
            //remotePlayer.PlayerName = "Bot";
            //remotePlayer.Position = PlayerPositions[1].position;
            //remotePlayer.BookPosition = BookPositions[1].position;
            //remotePlayer.IsAI = true;

            cardAnimator = FindObjectOfType<CardAnimator>();
        }

        void Start()
        {
            gameState = GameState.GameStarted;
            GameFlow();
            //PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        //****************** Game Flow *********************//
        public void GameFlow()
        {
            if (gameState > GameState.GameStarted)
            {
                CheckPlayersBooks();
                ShowAndHidePlayersDisplayingCards();

                if (gameDataManager.GameFinished())
                {
                    gameState = GameState.GameFinished;
                }
            }

            switch (gameState)
            {
                case GameState.Idel:
                    {
                        Debug.Log("IDEL");
                        break;
                    }
                case GameState.GameStarted:
                    {
                        Debug.Log("GameStarted");
                        OnGameStarted();
                        break;
                    }
                case GameState.TurnStarted:
                    {
                        Debug.Log("TurnStarted");
                        OnTurnStarted();
                        break;
                    }
                case GameState.TurnSelectingNumber:
                    {
                        Debug.Log("TurnSelectingNumber");
                        OnTurnSelectingNumber();
                        break;
                    }
                case GameState.TurnConfirmedSelectedNumber:
                    {
                        Debug.Log("TurnComfirmedSelectedNumber");
                        OnTurnConfirmedSelectedNumber();
                        break;
                    }
                case GameState.TurnWaitingForOpponentConfirmation:
                    {
                        Debug.Log("TurnWaitingForOpponentConfirmation");
                        OnTurnWaitingForOpponentConfirmation();
                        break;
                    }
                case GameState.TurnOpponentConfirmed:
                    {
                        Debug.Log("TurnOpponentConfirmed");
                        OnTurnOpponentConfirmed();
                        break;
                    }
                case GameState.TurnGoFish:
                    {
                        Debug.Log("TurnGoFish");
                        OnTurnGoFish();
                        break;
                    }
                case GameState.GameFinished:
                    {
                        Debug.Log("GameFinished");
                        OnGameFinished();
                        break;
                    }
            }
        }

        void OnGameStarted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                gameDataManager.Shuffle();
                //foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
                //{
                //    List<byte> player1Values = gameDataManager.DealCardValuesToPlayer(p, Constants.PLAYER_INITIAL_CARDS);

                //}
                List<byte> player1Values = gameDataManager.DealCardValuesToPlayer(localPlayer.PlayerId, Constants.PLAYER_INITIAL_CARDS);
                List<byte> player2Values = gameDataManager.DealCardValuesToPlayer(remotePlayer.PlayerId, Constants.PLAYER_INITIAL_CARDS);

                gameState = GameState.TurnStarted;

                Dictionary<string, List<byte>> dict = new Dictionary<string, List<byte>>();
                dict.Add(localPlayer.PlayerId,player1Values);
                dict.Add(remotePlayer.PlayerId, player2Values);

                Hashtable props = new Hashtable
                {
                {Constants.INITIALIZING_CARDS, dict},
                };

     
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            }
            
        }

        void OnTurnStarted()
        {
            SwitchTurn();
            gameState = GameState.TurnSelectingNumber;
            GameFlow();
        }

        public void OnTurnSelectingNumber()
        {
            ResetSelectedCard();

            if (currentTurnPlayer == localPlayer)
            {
                SetMessage($"Your turn. Pick a card from your hand.");
            }
            else
            {
                SetMessage($"{currentTurnPlayer.PlayerName}'s turn");
            }

        }

        public void OnTurnConfirmedSelectedNumber()
        {
            if (currentTurnPlayer == localPlayer)
            {
                SetMessage($"Asking {currentTurnTargetPlayer.PlayerName} for {selectedRank}s...");
            }
            else
            {
                SetMessage($"{currentTurnPlayer.PlayerName} is asking for {selectedRank}s...");
            }

            gameState = GameState.TurnWaitingForOpponentConfirmation;
            GameFlow();
        }

        public void OnTurnWaitingForOpponentConfirmation()
        {
            if (currentTurnTargetPlayer.IsAI)
            {
                gameState = GameState.TurnOpponentConfirmed;
                GameFlow();
            }
        }

        public void OnTurnOpponentConfirmed()
        {
            List<byte> cardValuesFromTargetPlayer = gameDataManager.TakeCardValuesWithRankFromPlayer(currentTurnTargetPlayer, selectedRank);

            if (cardValuesFromTargetPlayer.Count > 0)
            {
                gameDataManager.AddCardValuesToPlayer(currentTurnPlayer.PlayerId, cardValuesFromTargetPlayer);

                bool senderIsLocalPlayer = currentTurnTargetPlayer == localPlayer;
                currentTurnTargetPlayer.SendDisplayingCardToPlayer(currentTurnPlayer, cardAnimator, cardValuesFromTargetPlayer, senderIsLocalPlayer);
                gameState = GameState.TurnSelectingNumber;
            }
            else
            {
                gameState = GameState.TurnGoFish;
                GameFlow();
            }
        }

        public void OnTurnGoFish()
        {
            SetMessage($"Go fish!");

            byte cardValue = gameDataManager.DrawCardValue();

            if (cardValue == Constants.POOL_IS_EMPTY)
            {
                Debug.LogError("Pool is empty");
                return;
            }

            if (Card.GetRank(cardValue) == selectedRank)
            {
                cardAnimator.DrawDisplayingCard(currentTurnPlayer, cardValue);
            }
            else
            {
                cardAnimator.DrawDisplayingCard(currentTurnPlayer);
                gameState = GameState.TurnStarted;
            }

            gameDataManager.AddCardValueToPlayer(currentTurnPlayer, cardValue);
        }

        public void OnGameFinished()
        {
            if (gameDataManager.Winner() == localPlayer)
            {
                SetMessage($"You WON!");
            }
            else
            {
                SetMessage($"You LOST!");
            }
        }

        private IEnumerator EndOfGame(string winner, int score)
        {
            float timer = 5.0f;

            while (timer > 0.0f)
            {
                //InfoText.text = string.Format("Player {0} won with {1} points.\n\n\nReturning to login screen in {2} seconds.", winner, score, timer.ToString("n2"));

                yield return new WaitForEndOfFrame();

                timer -= Time.deltaTime;
            }

            PhotonNetwork.LeaveRoom();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            //todo
            //UnityEngine.SceneManagement.SceneManager.LoadScene("DemoAsteroids-LobbyScene");
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.Disconnect();
        }

        //****************** Helper Methods *********************//
        public void ResetSelectedCard()
        {
            if (selectedCard != null)
            {
                selectedCard.OnSelected(false);
                selectedCard = null;
                selectedRank = 0;
            }
        }

        void SetMessage(string message)
        {
            MessageText.text = message;
        }

        public void SwitchTurn()
        {
            if (currentTurnPlayer == null)
            {
                currentTurnPlayer = localPlayer;
                currentTurnTargetPlayer = remotePlayer;
                return;
            }

            if (currentTurnPlayer == localPlayer)
            {
                currentTurnPlayer = remotePlayer;
                currentTurnTargetPlayer = localPlayer;
            }
            else
            {
                currentTurnPlayer = localPlayer;
                currentTurnTargetPlayer = remotePlayer;
            }
        }

       
        public void CheckPlayersBooks()
        {
            List<byte> playerCardValues = gameDataManager.PlayerCards(localPlayer);
            localPlayer.SetCardValues(playerCardValues);
            
            playerCardValues = gameDataManager.PlayerCards(remotePlayer);
            remotePlayer.SetCardValues(playerCardValues);
            
        }

        public void ShowAndHidePlayersDisplayingCards()
        {
            localPlayer.ShowCardValues();
            remotePlayer.HideCardValues();
        }

        //****************** User Interaction *********************//
        public void OnCardSelected(Card card)
        {
            if (gameState == GameState.TurnSelectingNumber)
            {
                if (card.OwnerId == currentTurnPlayer.PlayerId)
                {
                    if (selectedCard != null)
                    {
                        selectedCard.OnSelected(false);
                        selectedRank = 0;
                    }

                    selectedCard = card;
                    selectedRank = selectedCard.Rank;
                    selectedCard.OnSelected(true);
                    SetMessage($"Ask {currentTurnTargetPlayer.PlayerName} for {selectedCard.Rank}s ?");
                }
            }
        }

        public void OnOkSelected()
        {
            if (gameState == GameState.TurnSelectingNumber && localPlayer == currentTurnPlayer)
            {
                if (selectedCard != null)
                {
                    gameState = GameState.TurnConfirmedSelectedNumber;
                    GameFlow();
                }
            }
            else if (gameState == GameState.TurnWaitingForOpponentConfirmation && localPlayer == currentTurnTargetPlayer)
            {
                gameState = GameState.TurnOpponentConfirmed;
                GameFlow();
            }
        }

        //****************** Animator Event *********************//
        public void AllAnimationsFinished()
        {
            if (PhotonNetwork.IsMasterClient && gameState == GameState.GameStarted) {
                Hashtable gameStateChangedProps = new Hashtable
                {
                    {Constants.GAME_STATE_CHANGED, GameState.TurnStarted}
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(gameStateChangedProps);
                gameState = GameState.TurnStarted;
                GameFlow();
            }
        }

        private void OnCountdownTimerIsExpired()
        {
            //StartGame();
        }
        //*********************Call Backs *******************//
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey(Constants.INITIALIZING_CARDS))
            {
                if (!PhotonNetwork.IsMasterClient)
                {
                    Dictionary<string, List<byte>> reply = (Dictionary<string, List<byte>>)propertiesThatChanged[Constants.INITIALIZING_CARDS];

                    if (reply.ContainsKey(localPlayer.PlayerId))
                    {

                        gameDataManager.AddCardValuesToPlayer(localPlayer.PlayerId, reply[localPlayer.PlayerId]);

                    }
                }
                else if (propertiesThatChanged.ContainsKey(Constants.GAME_STATE_CHANGED))
                {
                    int state = (int)propertiesThatChanged[Constants.GAME_STATE_CHANGED];
                    if (!PhotonNetwork.IsMasterClient)
                    {
                        CheckPlayersBooks();
                        ShowAndHidePlayersDisplayingCards();

                    }
                }
                else if (propertiesThatChanged.ContainsKey(Constants.PLAYER_MOVE))
                {
                    PlayerMove move = (PlayerMove)propertiesThatChanged[Constants.PLAYER_MOVE];
                    byte replyDroppedCards = move.droppedCards;
                    byte drawnCard = move.drawnCard;
                    string drawn = move.drawnFromDeckOrDropped;
                    int currentPlayerId = move.NextActorNumber;

                    if (currentPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        
                    }
                    //Animation
                    gameState = GameState.TurnSelectingNumber;
                    GameFlow();
                }
            }
        }


    }

}
