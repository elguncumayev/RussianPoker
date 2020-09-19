using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
    Main place where all game logic happens on Client side 
*/
public class GameLogicForClient : MonoBehaviour, IOnEventCallback
{
    public GameObject inGameTools;
    public Sprite[] cardFaces;
    public GameObject cardPrefab;
    public GameObject positionForDeck;
    public GameObject positionForHandCards;
    public PhotonView photonView;
    public TMP_Text sliderText;
    public Slider sliderToGuess;
    public GameObject guessButton;
    public GameObject sendCardButton;
    public GameObject[] jokerWinLoseButtons;
    public TMP_Text[] playersNames;
    public TMP_Text[] playersScores;
    public GameObject[] panels;
    public Camera ServerCamera;
    public GameObject panelJockLoseSuits;
    public GameObject cardForServer;

    public static string[] suits = new string[] { "C", "D", "H", "S" };
    public static string[] values = new string[] { "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

    public Dictionary<string, GameObject> cardsWithGameObject;
    public List<string> deck;
    public string[] myCardsInGame;
    private int guessesSumCounter = 0;
    private int guessCounter = 0;

    private const byte DrawCardsEventCode = 1;
    private const byte PlaceGuessEventCode = 2;
    private const byte SendCardEventCode = 4;
    private const byte SendAuthToSendCardEventCode = 5;
    private const byte SendGuessAuthCode = 6;
    private const byte WhoTakesHandEventCode = 7;
    private const byte UpdateScoresEventCode = 8;

    private bool gameStart = true;
    private bool blindRound = false;
    private bool guessAuthority = false;
    private bool sendCardAuthority = false;
    private bool lastGuesser = false;
    private bool firstPlayer = false;
    private string currentFirstCard = string.Empty;
    private string currentTrumpCard = string.Empty;
    private string mySelectedCard = string.Empty;
    private string jockLoseSuit = string.Empty;

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        cardForServer.SetActive(false);
        ServerCamera.gameObject.SetActive(false);
        deck = GenerateDeckClient();
        JockDrawClient();
    }

    //Generate deck of all cards on the beginning of game
    public static List<string> GenerateDeckClient()
    {
        List<string> newDeck = new List<string>();

        foreach (string suit in suits)
        {
            foreach (string value in values)
            {
                newDeck.Add(suit + value);
            }
        }
        newDeck.Add("RJk");
        newDeck.Add("BJk");
        return newDeck;
    }

    //Create cards on scene but not shown on screen
    void JockDrawClient()
    {
        cardsWithGameObject = new Dictionary<string, GameObject>();
        float yOffset = 0;
        float zOffset = 0.03f;
        Debug.Log("JockDrawClient");
        foreach (string card in deck)
        {
            GameObject newCard = Instantiate(cardPrefab, new Vector3(positionForDeck.transform.position.x, positionForDeck.transform.position.y + yOffset, positionForDeck.transform.position.z + zOffset), Quaternion.identity);
            newCard.name = card;
            newCard.GetComponent<ClickHandler>().InGameTools = inGameTools;
            newCard.GetComponent<Selectable>().faceUp = true;
            cardsWithGameObject.Add(card, newCard);
            yOffset += 0.1f;
            zOffset += 0.03f;
        }
    }

    //Method which receive Server methods coming via network 
    public void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;

        // DrawCardsEvent - Code = 1
        if (eventCode == DrawCardsEventCode)
        {
            guessCounter++;
            if (guessCounter == 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    string scoreText = playersScores[i].text;
                    playersScores[i].text = string.Format("G  - | W  - | S  {0}", scoreText.Substring(17));
                }
            }
            else if (guessCounter == 4) guessCounter = 0;
            Debug.Log("DrawCardsEvent Start");
            //Debug.Log("CustomData length : " + (string[])photonEvent.CustomData);
            //data is which player to recieve, cards and blindRound boolean
            object[] player = (object[])((object[])photonEvent.CustomData)[0];
            blindRound = (bool)((object[])photonEvent.CustomData)[2];
            Debug.Log((string)player[0]);

            object[] first = new object[] { player[0], player[1] };
            object[] second = new object[] { PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber };
            if (PlayersEquals(first, second))
            {
                myCardsInGame = Array.ConvertAll((object[])((object[])photonEvent.CustomData)[1], Convert.ToString);//(string[]) photonEvent.CustomData;

                int hand = myCardsInGame.Length;
                //DEBUG
                for (int i = 0; i < hand; i++)
                {
                    Debug.Log(myCardsInGame[i] + " new");
                }
                PlaceCards();
            }
        }
        else if (eventCode == SendAuthToSendCardEventCode)
        {
            object[] customData = (object[])photonEvent.CustomData;
            object[] cardPlayer = (object[])customData[0];
            object[] first = new object[] { cardPlayer[0], cardPlayer[1] };

            if (PlayersEquals(first, new object[] { PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber }))
            {
                currentFirstCard = (string)customData[1];
                currentTrumpCard = (string)customData[2];
                firstPlayer = (bool)customData[3];
                sendCardAuthority = true;
            }
        }
        else if (eventCode == SendGuessAuthCode)
        {
            if (gameStart)
            {
                for (int i = 0; i < 4; i++)
                {
                    int oneOrTwo = PhotonNetwork.PlayerList.Length == 5 ? 1 : 2;
                        playersNames[i].text = PhotonNetwork.PlayerList[i+oneOrTwo].NickName;
                    if (PhotonNetwork.LocalPlayer.NickName.Equals(PhotonNetwork.PlayerList[i+oneOrTwo].NickName))
                    {
                        panels[i].GetComponent<Image>().color = Color.green;
                    }
                }
                gameStart = false;
            }
            object[] customData = (object[])photonEvent.CustomData;

            object[] player = new object[] { ((object[])customData[0])[0], ((object[])customData[0])[1] };
            int playerCounterForGuess = int.Parse(customData[1].ToString());
            if(PlayersEquals(player, new object[] { PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber}))
            {
                if (playerCounterForGuess == 3)
                {
                    lastGuesser = true;
                    guessesSumCounter = int.Parse(customData[2].ToString());
                }
                guessAuthority = true;
            }
        }
        else if (eventCode == PlaceGuessEventCode)
        {
            object[] playerInfo = (object[])photonEvent.CustomData;
            for (int i = 0; i < 4; i++)
            {
                if (playersNames[i].text.Equals(playerInfo[0].ToString()))
                {
                    string text = playersScores[i].text;
                    playersScores[i].text = text.Substring(0, 3) + playerInfo[2].ToString() + text.Substring(4);
                }
            }
        }
        else if(eventCode == WhoTakesHandEventCode)
        {
            string name = (string)photonEvent.CustomData;
            for(int i = 0; i < 4; i++)
            {
                if (name.Equals(playersNames[i].text))
                {
                    string scoreText = playersScores[i].text;
                    int currentWins = scoreText[10].Equals('-') ? 0 : int.Parse(scoreText[10].ToString());
                    playersScores[i].text = string.Format("G  {0} | W  {1} | S  {2}", scoreText[3], currentWins + 1, scoreText.Substring(17));
                }
            }
        }
        else if(eventCode == UpdateScoresEventCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            for(int i = 0; i < 4; i++)
            {
                for(int j = 0; j < 4; j++)
                {
                    if (playersNames[i].text.Equals(data[j].ToString()))
                    {
                        playersScores[i].text = playersScores[i].text.Substring(0,17) + data[j+4].ToString();
                    }
                }
            }
        }
    }

    // Place cards on screen when server player cards 
    private void PlaceCards()
    {
        float xOffset = 0;
        float zOffset = 2f;
        foreach(string card in myCardsInGame)
        {
            Debug.Log(card);

            //DICTIONARY CHANGE #1
            //STARTS HERE
            
            //GameObject myCard = GameObject.Find(card);
            //GameObject myCard = cardsWithGameObject[card];

            if(blindRound) cardsWithGameObject[card].GetComponent<Selectable>().faceUp = false;
            cardsWithGameObject[card].transform.position = new Vector3(positionForHandCards.transform.position.x + xOffset, positionForHandCards.transform.position.y, zOffset);
            //END HERE

            xOffset += 1.3f;
            zOffset -= 0.1f;
        }

        Debug.Log("myCardsInGame length : " + myCardsInGame.Length);

        sliderToGuess.gameObject.SetActive(true);
        guessButton.SetActive(true);
        sliderToGuess.maxValue = myCardsInGame.Length;
    }
    
    //EVENT -> CODE = 4
    public void SendCardEvent()
    {
        Debug.Log("sendCardAuthority : " + sendCardAuthority);
        if (sendCardAuthority)
        {
            mySelectedCard = inGameTools.GetComponent<InGameToolsScript>().selectedCard;
            Debug.Log("mySelectedCard : " + mySelectedCard);
            if (!mySelectedCard.Equals(string.Empty) && IsOkCard(mySelectedCard))
            {
                //custom data: Sender and selected card
                //object[] data = new object[] { new CardPlayerInfo(PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber), mySelectedCard };
                object[] data = new object[] { new object[] { PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber, 0, 0, 0, 0,string.Empty }, mySelectedCard };//DEVELOPMENT - send card in playerInfo
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                PhotonNetwork.RaiseEvent(SendCardEventCode, data, raiseEventOptions, SendOptions.SendReliable);

                Debug.Log("Cardc to find: " + mySelectedCard);

                //DICTIONARY CHANGE #2
                //GameObject.Find(mySelectedCard).transform.position = positionForDeck.transform.position;
                cardsWithGameObject[mySelectedCard].transform.position = positionForDeck.transform.position;

                myCardsInGame  =  DeleteCardFromHand(mySelectedCard);
                mySelectedCard = string.Empty;
                
                sendCardButton.SetActive(false);
                jokerWinLoseButtons[0].SetActive(false);
                jokerWinLoseButtons[1].SetActive(false);

                sendCardAuthority = false;
            }
        }
    }

    //Delete card from hand which was sent
    private string[] DeleteCardFromHand(string mySelectedCard)
    {
        string[] newArray = new string[myCardsInGame.Length-1];
        int counter = 0;
        foreach(string card in myCardsInGame)
        {
            if (!card.Equals(mySelectedCard))
            {
                newArray[counter] = card;
                counter++;
            }
        }
        return newArray;
    }

    //EVENT -> CODE = 4
    public void SendCardEventJockLose()
    {
        if (sendCardAuthority)
        {
            mySelectedCard = inGameTools.GetComponent<InGameToolsScript>().selectedCard;
            if (!(mySelectedCard.Equals(string.Empty)) && IsOkCard(mySelectedCard))
            {
                //custom data: Sender and selected card
                //object[] data = new object[] { new CardPlayerInfo(PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber), mySelectedCard };
                object[] data = new object[] { new object[] { PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber, 0, 0, 0, 0,string.Empty }, mySelectedCard + (firstPlayer ? jockLoseSuit : "L") };
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                PhotonNetwork.RaiseEvent(SendCardEventCode, data, raiseEventOptions, SendOptions.SendReliable);

                Debug.Log("Cardc to find: " + mySelectedCard);

                //DICTIONARY CHANGE #3
                //GameObject.Find(mySelectedCard.Substring(0,3)).transform.position = positionForDeck.transform.position;
                cardsWithGameObject[mySelectedCard.Substring(0, 3)].transform.position = positionForDeck.transform.position;

                myCardsInGame = DeleteCardFromHand(mySelectedCard);
                mySelectedCard = string.Empty;

                sendCardButton.SetActive(false);
                jokerWinLoseButtons[0].SetActive(false);
                jokerWinLoseButtons[1].SetActive(false);

                sendCardAuthority = false;
                jockLoseSuit = string.Empty;
                inGameTools.GetComponent<InGameToolsScript>().selectedCard = string.Empty;
                firstPlayer = false;
            }
        }
    }

    // Can player sent selected card to table
    private bool IsOkCard(string mySelectedCard)
    {
        if (currentFirstCard.Equals(string.Empty)) return true; //first card is mine
        if (currentFirstCard[0] == 'B' || currentFirstCard[0] == 'R') return true; //first card is joker
        if (currentFirstCard[0].Equals(mySelectedCard[0]))//my Card is same kind as first card
        {
            return true;
        }
        else
        {
            if (currentTrumpCard[0].Equals(mySelectedCard[0]) && IsOkHandForTrump()){//my Card is Trump
                return true;
            }
            else
            {
                if(mySelectedCard[0].Equals('R') || mySelectedCard[0].Equals('B'))//my Card is Joker
                {
                    return true;
                }
                else
                {
                    return IsOkHand();
                }
            }
        }
    }

    // When player send card check hand for correct play for trump cards
    private bool IsOkHandForTrump()
    {
        foreach (string card in myCardsInGame)
        {
            //dont check selected card
            if (!card.Equals(mySelectedCard))
            {
                if (card[0].Equals(currentFirstCard[0]))
                {
                    return false;
                }
            }
        }
        return true;
    }

    // When player send card check hand for correct play for other cards
    private bool IsOkHand()
    {
        foreach(string card in myCardsInGame)
        {
            //dont check selected card
            if (!card.Equals(mySelectedCard))
            {
                //if player have same kind as first card or trump card he/she should play these ones
                if(card[0].Equals(currentFirstCard[0]) || card[0].Equals(currentTrumpCard[0]))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /*
     PUN2 Event: Send guess (integer value next to slider) to master client
                    in CardPlayerInfo object
     */
    //EVENT -> CODE = 2
    public void PlaceGuessEvent()
    {
        Debug.Log("guessAuthority : " + guessAuthority);
        if (guessAuthority)
        {
            Debug.Log("Guess Event");
            Debug.Log("Event Method : PlaceGuessEvent");
            int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

            object[] player = new object[] { PhotonNetwork.LocalPlayer.NickName, myActorNumber, 0, 0, 0, 0,string.Empty };
            
            //player is object array so there is no problem below
            player[2] = byte.Parse(sliderText.text);
            int guess = int.Parse(sliderText.text);

            Debug.Log("myCardsInGame length : " + myCardsInGame.Length);

            if (lastGuesser)
            {
                if (myCardsInGame.Length - guess == guessesSumCounter)
                {
                    Debug.Log("Wrong number to guess try again");
                    return;
                }
            }

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(PlaceGuessEventCode, player, raiseEventOptions, SendOptions.SendReliable);
            guessAuthority = false;
            lastGuesser = false;
            guessesSumCounter = 0;
            for(int i = 0; i < 4; i++)
            {
                if (playersNames[i].text.Equals(PhotonNetwork.LocalPlayer.NickName))
                {
                    string myScore = playersScores[i].text;
                    playersScores[i].text = string.Format("G  {0} | W  - | S  {1}", guess, myScore.Substring(17));
                }
            }

            sliderToGuess.gameObject.SetActive(false);
            guessButton.SetActive(false);

            foreach(string card in myCardsInGame)
            {
                
                //DICTIONARY CHANGE #4
                //STARTS HERE
                //GameObject myCard = GameObject.Find(card);
                //myCard.GetComponent<Selectable>().faceUp = true;

                cardsWithGameObject[card].GetComponent<Selectable>().faceUp = true;
                //END HERE

            }

        }
    }

    public void JockLoseChoice()
    {
        if (firstPlayer)
        {
            panelJockLoseSuits.SetActive(true);
        }
        else
        {
            SendCardEventJockLose();
        }
    }

    public void JockLoseClubs()
    {
        jockLoseSuit = "C";
        SendCardEventJockLose();
        panelJockLoseSuits.SetActive(false);
    }
    public void JockLoseDiamonds()
    {
        jockLoseSuit = "D";
        SendCardEventJockLose();
        panelJockLoseSuits.SetActive(false);
    }
    public void JockLoseHearts()
    {
        jockLoseSuit = "H";
        SendCardEventJockLose();
        panelJockLoseSuits.SetActive(false);
    }
    public void JockLoseSpades()
    {
        jockLoseSuit = "S";
        SendCardEventJockLose();
        panelJockLoseSuits.SetActive(false);
    }

    private bool PlayersEquals(object[] first, object[] second)
    {
        if (((string)first[0]).Equals((string)second[0])) return true;
        //else if (!(first[1] == second[1])) return false;
        else return false;
    }
}


//OLD CODE
//[PunRPC]
//void getMyCards(object[] randomCards)
//{
//    myCardsInGame = (string[]) randomCards;

//    //DEBUG purpose
//    //print in Game randomly selected cards
//    foreach(string card in myCardsInGame)
//    {
//        Debug.Log(card);
//    }

//    int xOffset = 0;//1
//    foreach(string card in myCardsInGame)
//    {
//        GameObject cardObject = GameObject.Find(card);
//        cardObject.transform.position = new Vector3(positionForHandCards.transform.position.x + xOffset, positionForHandCards.transform.position.y, positionForHandCards.transform.position.z);
//        xOffset += 1;
//    }
//}


/*
 selected card send help

    string myGuessCard = inGameTools.GetComponent<InGameToolsScript>().selectedCard;
    object guessedCard = myGuessCard;
 */


