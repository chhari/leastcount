using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using System;

namespace GoFish
{
    public class LeastCountGame : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public Text MessageText;

        CardAnimator cardAnimator;

        public LeastCountManager leastCountManager;

        public List<Transform> PlayerPositions = new List<Transform>();
        public List<Transform> BookPositions = new List<Transform>();

        MyPlayer localPlayer;
        MyPlayer remotePlayer;

        MyPlayer winner;
        List<Card> selectedCards = new List<Card>();

        MyPlayer currentTurnPlayer;
        MyPlayer currentTurnTargetPlayer;

        Card selectedCard;
        Ranks selectedRank;
        Ranks deckOrDroppedCard;
        PlayerMove move;
        bool intializing=false;

        public enum GameState
        {
            Idle,
            GameStarted,
            TurnStarted,
            TurnSelectingDroppingCard,
            TurnConfirmDroppingCard,
            TurnDrawingCard,
            TurnDrawingCardConfirmed,
            Show,
            GameFinished
        };

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

        public GameState gameState = GameState.Idle;

        private void Awake()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.IsLocal)
                {
                    localPlayer = new MyPlayer();
                    localPlayer.PlayerId = p.ActorNumber.ToString();
                    localPlayer.PlayerName = p.NickName;
                    localPlayer.Position = PlayerPositions[0].position;
                }
                else
                {
                    remotePlayer = new MyPlayer();
                    remotePlayer.PlayerId = p.ActorNumber.ToString();
                    remotePlayer.PlayerName = p.NickName;
                    remotePlayer.Position = PlayerPositions[1].position;
                }
            }
            leastCountManager = new LeastCountManager(localPlayer, remotePlayer);
            cardAnimator = FindObjectOfType<CardAnimator>();
        }

        void Start()
        {
            gameState = GameState.GameStarted;
            GameFlow();
        }

        //****************** Game Flow *********************//
        public void GameFlow()
        {
            if (gameState > GameState.GameStarted)
            {
                CheckPlayersBooks();
                ShowAndHidePlayersDisplayingCards();

                if (leastCountManager.GameFinished())
                {
                    gameState = GameState.GameFinished;
                }
            }

            switch (gameState)
            {
                case GameState.Idle:
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
                case GameState.TurnSelectingDroppingCard:
                    {
                        Debug.Log("TurnSelectingNumber");
                        OnTurnSelectingDroppingCard();
                        break;
                    }
                case GameState.TurnConfirmDroppingCard:
                    {
                        Debug.Log("TurnComfirmedSelectedNumber");
                        OnTurnConfirmDroppingCard();
                        break;
                    }
                case GameState.TurnDrawingCard:
                    {
                        Debug.Log("TurnWaitingForOpponentConfirmation");
                        OnTurnDrawingCard();
                        break;
                    }
                case GameState.TurnDrawingCardConfirmed:
                    {
                        Debug.Log("TurnOpponentConfirmed");
                        OnTurnDrawingCardConfirmed();
                        break;
                    }
                case GameState.Show:
                    {
                        Debug.Log("TurnGoFish");
                        OnShow();
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

                leastCountManager.Shuffle();
                List<byte> player1Values = leastCountManager.DealCardValuesToPlayer(localPlayer, Constants.PLAYER_INITIAL_CARDS);
                List<byte> player2Values = leastCountManager.DealCardValuesToPlayer(remotePlayer, Constants.PLAYER_INITIAL_CARDS);
                List<byte> poolOfCards = leastCountManager.GetPoolOfCards();
                byte droppedCardValue = leastCountManager.FirstDroppedCard();
                List<byte> droppedListValue = new List<byte>();
                droppedListValue.Add(droppedCardValue);
               
                Dictionary<string, byte[]> dict = new Dictionary<string,byte[]>();
                dict.Add(localPlayer.PlayerId, player1Values.ToArray());
                dict.Add(remotePlayer.PlayerId, player2Values.ToArray());
                dict.Add("poolOfCards", poolOfCards.ToArray());
                dict.Add(Constants.INITIALIZING_DROPPEDCARD, droppedListValue.ToArray());

                currentTurnPlayer = localPlayer;

                //Hashtable props = new Hashtable
                //{
                //{Constants.INITIALIZING_CARDS, dict}
                //};
                //PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                byte evCode = 1; // Custom Event 1: Used as "MoveUnitsToTargetPosition" event
                //object[] content = new object[] { dict }; // Array contains the target position and the IDs of the selected units
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                PhotonNetwork.RaiseEvent(evCode, dict, raiseEventOptions, SendOptions.SendReliable);
                Debug.Log("master onstarted");
                cardAnimator.DealDisplayingCards(localPlayer, Constants.PLAYER_INITIAL_CARDS);
                cardAnimator.DealDisplayingCards(remotePlayer, Constants.PLAYER_INITIAL_CARDS);
                Card firstDroppedCard = cardAnimator.DropFirstCard(droppedCardValue);
                leastCountManager.AddToDropCardsReference(firstDroppedCard);
                
            }
            else {
                if (intializing && !PhotonNetwork.IsMasterClient)
                {
                    cardAnimator.DealDisplayingCards(localPlayer, Constants.PLAYER_INITIAL_CARDS);
                    cardAnimator.DealDisplayingCards(remotePlayer, Constants.PLAYER_INITIAL_CARDS);
                    Card firstDroppedCard = cardAnimator.DropFirstCard(leastCountManager.GetDroppedCardValues()[0]);
                    leastCountManager.AddToDropCardsReference(firstDroppedCard);
                    intializing = false;

                }
            }

        }

        void OnTurnStarted()
        {
            SwitchTurn();
            gameState = GameState.TurnSelectingDroppingCard;
            GameFlow();
        }

        public void OnTurnSelectingDroppingCard()
        {

            ResetSelectedCard();
            move = new PlayerMove();

            if (currentTurnPlayer == localPlayer)
            {
                SetMessage($"Your turn. Pick a card from your hand.select a card");
            }
            else
            {
                SetMessage($"{currentTurnPlayer.PlayerName}'s turn");
            }
            
        }

        public void OnTurnConfirmDroppingCard()
        {
            Dictionary<string, byte> dict3 = new Dictionary<string, byte>();
            dict3.Add("CurrentActorNumber", Convert.ToByte(PhotonNetwork.LocalPlayer.ActorNumber));
            string dropString = "dropString";
            for(int i=0;i<selectedCards.Count;i++) {
                dict3.Add(dropString + i.ToString(),selectedCards[i].Value);
            }
            byte evCode = 3;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, dict3, raiseEventOptions, SendOptions.SendReliable);

            gameState = GameState.TurnDrawingCard;
            GameFlow();

            SetMessage($" {currentTurnPlayer.PlayerName}dropped {selectedRank},click draw from deck or draw from dropped buttons");
        }

        public void OnTurnDrawingCard()
        {
           
        }

        public void OnTurnDrawingCardConfirmed()
        {
            move.CurrentActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            move.NextActorNumber = PhotonNetwork.LocalPlayer.GetNext().ActorNumber;
            Dictionary<string, byte> dict2 = new Dictionary<string,byte>();            
            dict2.Add("drawnCard", move.drawnCard);
            dict2.Add("CurrentActorNumber", Convert.ToByte(move.CurrentActorNumber));
            dict2.Add("NextActorNumber", Convert.ToByte(move.NextActorNumber));
            if (move.drawnFromDeckOrDropped == "dropped")
            {
                dict2.Add("drawnFromDeckOrDropped", 0);
            }
            else {
                dict2.Add("drawnFromDeckOrDropped", 1);

            }

            byte evCode = 2;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, dict2, raiseEventOptions, SendOptions.SendReliable);

        }

        public void OnShow() {
            gameState = GameState.GameFinished;
            GameFlow();

        }


        public void OnGameFinished()
        {
            if (leastCountManager.Winner() == localPlayer)
            {
                SetMessage($"You WON!");
            }
            else
            {
                SetMessage($"You LOST!");
            }
        }

        //****************** Helper Methods *********************//
        public void ResetSelectedCard()
        {
            if (selectedCard != null)
            {
                //selectedCard.OnSelected(false);
                selectedCard = null;
                selectedRank = 0;
            }

            if (selectedCards != null)
            {               
                selectedCards.Clear();
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
                return;
            }

            //if (currentTurnPlayer == localPlayer)
            //{
            //    currentTurnPlayer = remotePlayer;
            //}///
            //else
            //{
            //    currentTurnPlayer = localPlayer;
            //}
        }

        public void CheckPlayersBooks()
        {
            List<byte> playerCardValues = leastCountManager.PlayerCards(localPlayer);
            localPlayer.SetCardValues(playerCardValues);
            
            playerCardValues = leastCountManager.PlayerCards(remotePlayer);
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
            if (gameState == GameState.TurnSelectingDroppingCard && currentTurnPlayer == localPlayer)
            {
                if (card.OwnerId == currentTurnPlayer.PlayerId)
                {
                    
                    if (ConditionsForCardSelection(card))
                    {
                        selectedCard = card;
                        card.OnSelected(true);
                        selectedCards.Add(card);
                        selectedRank = selectedCard.Rank;                                            
                    }
                    SetMessage($"{currentTurnPlayer.PlayerName} ,do you want to drop  {selectedCard.Rank} ?");
                }
            }
        }

        public bool ConditionsForCardSelection(Card card) {
            if (selectedCards.Count == 0)
            {
                return true;
            }
            else if (selectedCards.Contains(card))
            {
                return false;
            }
            else if (card.Rank == selectedCards[0].Rank)
            {
                return true;
            }
            
            else {
                foreach (Card c in selectedCards) {
                    c.OnSelected(false);
                }
                selectedCards.Clear();
                return true;
            }
            
        }


        public void OnOkSelected()
        {

            if (gameState == GameState.TurnConfirmDroppingCard && currentTurnPlayer.IsAI)
            {
                if (currentTurnPlayer.IsAI)
                {
                    int res = leastCountManager.SelectInRandomFromDeckOrDroppedCard();
                    if (res.Equals(0))
                    {
                        OnDrawFromDeckButton();
                    }
                    else
                    {
                        OnDrawFromLastDroppedButton();
                    }
                }
            }
        }

        public void OnShowButton() {
            winner = leastCountManager.getWinner(currentTurnPlayer, currentTurnTargetPlayer);
            SetMessage($" {winner.PlayerName} Won the game ");
            gameState = GameState.Show;
            GameFlow();

        }

        public void OnDrawFromDeckButton() {
            if (gameState == GameState.TurnDrawingCard)
            {
                byte cardValue = leastCountManager.DrawCardValue();

                if (cardValue == Constants.POOL_IS_EMPTY)
                {
                    Debug.LogError("Pool is empty");
                    return;
                }

                cardAnimator.DrawDisplayingCard(currentTurnPlayer, cardValue);
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, cardValue);
                move.drawnFromDeckOrDropped = "deck";
                move.drawnCard = cardValue;
                gameState = GameState.TurnDrawingCardConfirmed;
                GameFlow();
            }
            else {
                SetMessage("Drop the card and click on confirm card button first");
            }
        }

        public void OnDrawFromLastDroppedButton() {
            if (gameState == GameState.TurnDrawingCard)
            {
                Card card = leastCountManager.DrawDroppedCard();
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, card.GetValue());
                cardAnimator.DrawDroppedCard(currentTurnPlayer, card);
                leastCountManager.RepositionDroppedCards(cardAnimator);
                move.drawnFromDeckOrDropped = "dropped";
                move.drawnCard = card.GetValue();
                gameState = GameState.TurnDrawingCardConfirmed;
                GameFlow();
            }
            else {
                SetMessage("Drop the card and click on confirm card button first");
            }

        }

        //
        public void ConfirmDropButton() {

            if (selectedCards != null)
            {
                foreach (Card selCard in selectedCards)
                {
                    leastCountManager.DropCardsFromPlayer(currentTurnPlayer, selCard);
                    cardAnimator.DropCardAnimation(selCard, leastCountManager.GetDroppedCardsCount());
                    currentTurnPlayer.DropCardFromPlayer(cardAnimator, selCard.GetValue(), true);
                    leastCountManager.RepositionDroppedCards(cardAnimator);
                }
                gameState = GameState.TurnConfirmDroppingCard;
                GameFlow();
            }
            else {
                SetMessage("Select a card from your deck and click confirm");
            }
        }

        //****************** Animator Event *********************//
        public void MoveAnimations(byte value,byte deckOrDrawn,byte cardValue) {                       
            Card returnedCard = currentTurnPlayer.DropCardFromPlayer(cardAnimator, cardValue, false);
            leastCountManager.DropCardsFromPlayer(currentTurnPlayer, returnedCard);
            cardAnimator.DropCardAnimation(returnedCard, leastCountManager.GetDroppedCardsCount());
            leastCountManager.RepositionDroppedCards(cardAnimator);

            if (deckOrDrawn == 0){
                Card card = leastCountManager.DrawDroppedCard();
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, card.GetValue());
                cardAnimator.DrawDroppedCard(currentTurnPlayer, card);
                leastCountManager.RepositionDroppedCards(cardAnimator);
            }
            else {
                cardAnimator.DrawDisplayingCard(currentTurnPlayer, cardValue);
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, cardValue);
            }

        }

        public void DroppedAnimations( byte value) {
            
                Card returnedCard = currentTurnPlayer.DropCardFromPlayer(cardAnimator, value, false);
                leastCountManager.DropCardsFromPlayer(currentTurnPlayer, returnedCard);
                cardAnimator.DropCardAnimation(returnedCard, leastCountManager.GetDroppedCardsCount());
                leastCountManager.RepositionDroppedCards(cardAnimator);
            
        }

        public void DrawCardAnimations(byte deckOrDrawn, byte cardValue) {
            if (deckOrDrawn == 0)
            {
                Card card = leastCountManager.DrawDroppedCard();
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, card.GetValue());
                cardAnimator.DrawDroppedCard(currentTurnPlayer, card);
                leastCountManager.RepositionDroppedCards(cardAnimator);
            }
            else
            {
                cardAnimator.DrawDisplayingCard(currentTurnPlayer, cardValue);
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, cardValue);
            }
        }
        //****************** Animator Event *********************//
        public void AllAnimationsFinished()
        {
            if (gameState == GameState.GameStarted)
            {
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
                
                Debug.Log("Intializing cards");
                if (!PhotonNetwork.IsMasterClient)
                {
                    Dictionary<string, List<byte>> reply = (Dictionary<string, List<byte>>)propertiesThatChanged[Constants.INITIALIZING_CARDS];

                    if (reply.ContainsKey(localPlayer.PlayerId))
                    {

                        leastCountManager.AddCardValuesToPlayer(localPlayer.PlayerId, reply[localPlayer.PlayerId]);
                        leastCountManager.AddCardValuesToPlayer(remotePlayer.PlayerId, reply[remotePlayer.PlayerId]);
                        leastCountManager.SetPoolOfCards(reply["poolOfCards"]);
                        leastCountManager.AddCardToDroppedCards(reply[Constants.INITIALIZING_DROPPEDCARD][0]);
                        intializing = true;
                        gameState = GameState.GameStarted;
                        GameFlow();
                    }
                }
            }
            if (propertiesThatChanged.ContainsKey(Constants.GAME_STATE_CHANGED))
            {
                int state = (int)propertiesThatChanged[Constants.GAME_STATE_CHANGED];
                if (!PhotonNetwork.IsMasterClient)
                {
                    CheckPlayersBooks();
                    ShowAndHidePlayersDisplayingCards();

                }
            }
            if (propertiesThatChanged.ContainsKey(Constants.PLAYER_MOVE))
            {
                PlayerMove move = (PlayerMove)propertiesThatChanged[Constants.PLAYER_MOVE];
                int justPlayed = move.CurrentActorNumber;
                byte replyDroppedCards = move.droppedCards;
                byte drawnCard = move.drawnCard;
                string drawn = move.drawnFromDeckOrDropped;
                int currentPlayerId = move.NextActorNumber;
                if (justPlayed != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    MoveAnimations(replyDroppedCards, 0,drawnCard);
                }

                if (currentPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    currentTurnPlayer = localPlayer;
                    gameState = GameState.TurnSelectingDroppingCard;
                    GameFlow();

                }
                //Animation

            }
        }

        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;

            if (eventCode == 1)
            {
                if (!PhotonNetwork.IsMasterClient)
                {

                    Debug.Log(photonEvent.CustomData.GetType());
                    Dictionary<string, byte[]> reply = (Dictionary<string, byte[]>)photonEvent.CustomData;

                    //Dictionary<string, List<byte>> reply = data;

                    if (reply.ContainsKey(localPlayer.PlayerId))
                    {

                        leastCountManager.AddCardValuesToPlayer(localPlayer.PlayerId, reply[localPlayer.PlayerId].ToList());
                        leastCountManager.AddCardValuesToPlayer(remotePlayer.PlayerId, (List<byte>)reply[remotePlayer.PlayerId].ToList());
                        leastCountManager.SetPoolOfCards((List<byte>)reply["poolOfCards"].ToList());
                        leastCountManager.AddCardToDroppedCards(reply[Constants.INITIALIZING_DROPPEDCARD][0]);
                         intializing = true;
                        currentTurnPlayer = remotePlayer;
                        gameState = GameState.GameStarted;
                        GameFlow();
                    }
                }
            }
            else if (eventCode == 2)
            {

                Dictionary<string, byte> move = (Dictionary<string, byte>)photonEvent.CustomData;
                int justPlayed = Convert.ToInt32(move["CurrentActorNumber"]);
                byte drawnCard = move["drawnCard"];
                byte drawnFromDeckOrDropped = move["drawnFromDeckOrDropped"];
                int currentPlayerId = Convert.ToInt32(move["NextActorNumber"]);
                if (justPlayed != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    DrawCardAnimations(drawnFromDeckOrDropped, drawnCard);
                }

                if (currentPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    currentTurnPlayer = localPlayer;
                    gameState = GameState.TurnSelectingDroppingCard;
                    GameFlow();

                }
                else {
                    currentTurnPlayer = remotePlayer;
                    gameState = GameState.TurnSelectingDroppingCard;
                    GameFlow();
                }
              
            }
            else if (eventCode == 3) {
                Dictionary<string, byte> move = (Dictionary<string, byte>)photonEvent.CustomData;
                int justPlayed = Convert.ToInt32(move["CurrentActorNumber"]);
                if (justPlayed != PhotonNetwork.LocalPlayer.ActorNumber) {
                    var keyList = new List<string>(move.Keys);
                    for (int i = 0; i < keyList.Count; i++)
                    {
                        var key = keyList[i];
                        if (key.Contains("dropString")) {
                            DroppedAnimations(move[key]);
                        }
                    }
                }
            }
        }
    }
}
