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

    public static string[] suits = new string[] { "C", "D", "H", "S" };
    public static string[] values = new string[] { "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

    public List<string> deck;
    public string[] myCardsInGame;
    private int guessesCounter = 0;

    private const byte DrawCardsEventCode = 1;
    private const byte PlaceGuessEventCode = 2;
    private const byte SendCardEventCode = 4;
    private const byte SendAuthToSendCardEventCode = 5;
    private const byte SendGuessAuthCode = 6;

    private bool blindRound = false;
    private bool guessAuthority = false;
    private bool sendCardAuthority = false;
    private bool lastGuesser = false;
    private string currentFirstCard = string.Empty;
    private string currentTrumpCard = string.Empty;
    private string mySelectedCard = string.Empty;

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
        float yOffset = 0;
        float zOffset = 0.03f;
        Debug.Log("JockDrawClient");
        foreach (string card in deck)
        {
            GameObject newCard = Instantiate(cardPrefab, new Vector3(positionForDeck.transform.position.x, positionForDeck.transform.position.y + yOffset, positionForDeck.transform.position.z + zOffset), Quaternion.identity);
            newCard.name = card;
            newCard.GetComponent<ClickHandler>().InGameTools = inGameTools;
            newCard.GetComponent<Selectable>().faceUp = true;
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
                sendCardAuthority = true;
            }
        }
        else if (eventCode == SendGuessAuthCode)
        {
            object[] customData = (object[])photonEvent.CustomData;

            object[] player = new object[] { ((object[])customData[0])[0], ((object[])customData[0])[1] };
            int playerCounterForGuess = int.Parse(customData[1].ToString());
            if(PlayersEquals(player, new object[] { PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber}))
            {
                if (playerCounterForGuess == 3)
                {
                    lastGuesser = true;
                    guessesCounter = int.Parse(customData[2].ToString());
                }
                guessAuthority = true;
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
            GameObject myCard = GameObject.Find(card);
            if(blindRound) myCard.GetComponent<Selectable>().faceUp = false;
            myCard.transform.position = new Vector3(positionForHandCards.transform.position.x + xOffset, positionForHandCards.transform.position.y, zOffset);
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
                GameObject.Find(mySelectedCard).transform.position = positionForDeck.transform.position;
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
                object[] data = new object[] { new object[] { PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber, 0, 0, 0, 0,string.Empty }, mySelectedCard+'L' };
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                PhotonNetwork.RaiseEvent(SendCardEventCode, data, raiseEventOptions, SendOptions.SendReliable);
                mySelectedCard = string.Empty;

                sendCardButton.SetActive(false);
                jokerWinLoseButtons[0].SetActive(false);
                jokerWinLoseButtons[1].SetActive(false);

                sendCardAuthority = false;
            }
        }
    }

    // Can player sent selected card to table
    private bool IsOkCard(string mySelectedCard)
    {
        
        if (currentFirstCard.Equals(string.Empty)) return true; //first card is mine
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
                if (myCardsInGame.Length - guess == guessesCounter)
                {
                    Debug.Log("Wrong number to guess try again");
                    return;
                }
            }

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(PlaceGuessEventCode, player, raiseEventOptions, SendOptions.SendReliable);
            guessAuthority = false;
            lastGuesser = false;
            guessesCounter = 0;

            sliderToGuess.gameObject.SetActive(false);
            guessButton.SetActive(false);

            foreach(string card in myCardsInGame)
            {
                GameObject myCard = GameObject.Find(card);
                myCard.GetComponent<Selectable>().faceUp = true;
            }

        }
    }
    private bool PlayersEquals(object[] first, object[] second)
    {
        if ( ( (string) first[0]).Equals( (string) second[0]) ) return true;
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


