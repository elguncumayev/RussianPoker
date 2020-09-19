using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections;
using System;

/*
    Main place where all game logic happens on Server side
 */
public class Jock : MonoBehaviourPunCallbacks, IOnEventCallback
{
    //  playerInfo object array
    //   0 string playerName{ get; set; }
    //   1 int id { get; set; }
    //   2 int currentGuess { get; set; }
    //   3 int currentWins { get; set; }
    //   4 int score { get; set; }
    //   5 int currentCardScore { get; set; }
    //   6 string currentSelectedCard { get; set; }

    public Sprite[] cardFaces;
    public GameObject cardPrefab;
    public GameObject DeckForPosition;
    public GameObject[] playerPositionsOnTable;
    public GameObject[] turnCircles;
    public GameObject InGameTools;
    public PhotonView clientPhotonView;
    public TMP_Text[] scoreTable;
    public TMP_Text[] playerNames;
    public TMP_Text roundText;
    public TMP_Text winner;
    public Canvas inGameUI;
    public TMP_Text turnPlayer;
    public TMP_Text jocker;
    public TMP_Text tookHand;
    public Canvas BeforeGameCanvas;
    public TMP_Text loseJock;

    public static string[] suits = new string[] { "C", "D", "H", "S" };
    public static string[] values = new string[] { "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

    public List<string> deck;
    public List<GameObject> playerCardsPlaces;

    //players info save in array because, Photon Networking don't let send info as custom classes via network 
    public object[,] playerInfoList; /*= new object[4, 7] { { string.Empty, 0, -1, 0, 0, 0, string.Empty }, 
                                                         { string.Empty, 0, -1, 0, 0, 0, string.Empty }, 
                                                         { string.Empty, 0, -1, 0, 0, 0, string.Empty }, 
                                                         { string.Empty, 0, -1, 0, 0, 0, string.Empty }};*/

    private int currentQueue = 0;
    private int startPlayer = 0;
    private int playerCounterForGuess = 0;
    private int playerCounterForfirstCard = 0;
    private int currentRound = 0;
    private int currentHand = 0;
    private bool gameStart = false;
    private bool noTrumpRound = false;
    private bool blindRound = false;
    private string greaterJocker = string.Empty;

    public string currentTrumpCard = string.Empty;
    public string currentTrumpCardClonServer = string.Empty;
    public string currentFirstCard = string.Empty;
    private const byte DrawCardsEventCode = 1;
    private const byte PlaceGuessEventCode = 2;
    private const byte SendMyInfoToMasterEventCode = 3;
    private const byte SendCardEventCode = 4;
    private const byte SendAuthToSendCardEventCode = 5;
    private const byte SendGuessAuthCode = 6;
    private const byte WhoTakesHandEventCode = 7;
    private const byte UpdateScoresEventCode = 8;
    private const byte ClonServerStartGameEventCode = 9;
    private const byte SendRoundAndHandInfoEventCode = 10;
    private const byte SendTurnPlayerEventCode = 11;
    private const byte SendTrumpCardEventCode = 12;


    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    private void Start()
    {
        playerInfoList = new object[4, 7] { { string.Empty, 0, -1, 0, 0, 0, string.Empty },
                                            { string.Empty, 0, -1, 0, 0, 0, string.Empty },
                                            { string.Empty, 0, -1, 0, 0, 0, string.Empty },
                                            { string.Empty, 0, -1, 0, 0, 0, string.Empty }};
    }

    // Update is called once per frame
    void Update()
    {
        // Score table score update
        if (gameStart)
        {
            for (int i = 0; i < 4; i++)
            {
                scoreTable[i].text = string.Format("{0}. G: {1} ; W: {2} ; S: {3}", ((string)playerInfoList[i, 0]) == string.Empty ? "NN" : ((string)playerInfoList[i, 0]).Substring(0, 2), playerInfoList[i, 2].ToString()[0], playerInfoList[i, 3], playerInfoList[i, 4]);
            }
        }
        roundText.text = string.Format("Round: {0}\nHand: {1}", currentRound, currentHand);
    }

    public void StartGameAsync()
    {
        ClonServerStartGameEvent();
        // Start game on Start button click
        for (int i = 0; i < 4; i++)
        {
            playerNames[i].text = (string)playerInfoList[i, 0];
        }
        gameStart = true;
        inGameUI.gameObject.SetActive(true);
        StartCoroutine(PlayCardsAsync());
    }

    private void ClonServerStartGameEvent()
    {
        object data = 1;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(ClonServerStartGameEventCode, data, raiseEventOptions, SendOptions.SendReliable);
    }

    //All rounds in one place with different arguments for each round
    IEnumerator PlayCardsAsync()
    {
        Debug.Log("PlayCardsAsync");
        deck = GenerateDeckServer();
        Shuffle(deck);

        // show cards on Log
        //foreach(string card in deck)
        //{
        //    print(card);
        //}

        Debug.Log("Round 1 Start");
        JockDrawServer();
        startPlayer = RandomPlayerInt();
        for(int i = 0; i < 4; i++)
        {
            if(i == startPlayer)
            {
                turnCircles[i].SetActive(true);
            }
            else
            {
                turnCircles[i].SetActive(false);
            }
        }
        SendTurnPlayerEvent(startPlayer);

            //Clear this statement after tests
        turnPlayer.text = string.Format("Turn\n{0}", (string)playerInfoList[startPlayer, 0]);
        
        currentRound++;
        yield return StartCoroutine(Round1to4Async());
        StopCoroutine(Round1to4Async());
        Debug.Log("Round 1 End");

        Debug.Log("Round 2 Start");
        startPlayer = NextPlayerInt(startPlayer);
        Debug.Log("Start player : " + playerInfoList[startPlayer, 0].ToString());
        currentRound++;
        yield return StartCoroutine(Round1to4Async());
        StopCoroutine(Round1to4Async());
        Debug.Log("Round 2 End");

        Debug.Log("Round 3 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round1to4Async());
        StopCoroutine(Round1to4Async());
        Debug.Log("Round 3 End");

        Debug.Log("Round 4 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round1to4Async());
        StopCoroutine(Round1to4Async());
        Debug.Log("Round 4 End");

        Debug.Log("Round 5 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round5and22Async());
        StopCoroutine(Round5and22Async());
        Debug.Log("Round 5 End");

        Debug.Log("Round 6 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round6and21Async());
        StopCoroutine(Round6and21Async());
        Debug.Log("Round 6 End");

        Debug.Log("Round 7 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round7and20Async());
        StopCoroutine(Round7and20Async());
        Debug.Log("Round 7 End");

        Debug.Log("Round 8 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round8and19Async());
        StopCoroutine(Round8and19Async());
        Debug.Log("Round 8 End");

        Debug.Log("Round 9 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round9and18Async());
        StopCoroutine(Round9and18Async());
        Debug.Log("Round 9 End");

        Debug.Log("Round 10 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round10and17Async());
        StopCoroutine(Round10and17Async());
        Debug.Log("Round 10 End");

        Debug.Log("Round 11 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round11and16Async());
        StopCoroutine(Round11and16Async());
        Debug.Log("Round 11 End");

        Debug.Log("Round 12 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round12to15Async());
        StopCoroutine(Round12to15Async());
        Debug.Log("Round 12 End");

        Debug.Log("Round 13 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round12to15Async());
        StopCoroutine(Round12to15Async());
        Debug.Log("Round 13 End");

        Debug.Log("Round 14 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round12to15Async());
        StopCoroutine(Round12to15Async());
        Debug.Log("Round 14 End");

        Debug.Log("Round 15 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round12to15Async());
        StopCoroutine(Round12to15Async());
        Debug.Log("Round 15 End");

        Debug.Log("Round 16 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round11and16Async());
        StopCoroutine(Round11and16Async());
        Debug.Log("Round 16 End");

        Debug.Log("Round 17 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round10and17Async());
        StopCoroutine(Round10and17Async());
        Debug.Log("Round 17 End");

        Debug.Log("Round 18 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round9and18Async());
        StopCoroutine(Round9and18Async());
        Debug.Log("Round 18 End");

        Debug.Log("Round 19 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round8and19Async());
        StopCoroutine(Round8and19Async());
        Debug.Log("Round 19 End");

        Debug.Log("Round 20 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round7and20Async());
        StopCoroutine(Round7and20Async());
        Debug.Log("Round 20 End");

        Debug.Log("Round 21 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round6and21Async());
        StopCoroutine(Round6and21Async());
        Debug.Log("Round 21 End");

        Debug.Log("Round 22 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(Round5and22Async());
        StopCoroutine(Round5and22Async());
        Debug.Log("Round 22 End");

        noTrumpRound = true;
        Debug.Log("Round 23 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(RoundNoTrAsync());
        StopCoroutine(RoundNoTrAsync());
        Debug.Log("Round 23 End");

        Debug.Log("Round 24 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(RoundNoTrAsync());
        StopCoroutine(RoundNoTrAsync());
        Debug.Log("Round 24 End");

        Debug.Log("Round 25 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(RoundNoTrAsync());
        StopCoroutine(RoundNoTrAsync());
        Debug.Log("Round 25 End");

        Debug.Log("Round 26 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(RoundNoTrAsync());
        StopCoroutine(RoundNoTrAsync());
        Debug.Log("Round 26 End");
        noTrumpRound = false;

        blindRound = true;
        Debug.Log("Round 27 Start");
        startPlayer = NextPlayerInt(startPlayer);
        currentRound++;
        yield return StartCoroutine(RoundBlAsync());
        StopCoroutine(RoundBlAsync());
        Debug.Log("Round 27 End");
        blindRound = false;


        //Calculate who wins and give message on screen
        int max = 0;

        for (int i = 1; i < 4; i++)
        {
            if (int.Parse(playerInfoList[i, 4].ToString()) > int.Parse(playerInfoList[max, 4].ToString()))
            {
                max = i;
            }
        }

        winner.text = string.Format("{0} WINS the game", (string)playerInfoList[max, 0]);
        winner.gameObject.SetActive(true);

    }

    IEnumerator RoundBlAsync()
    {
        currentHand = 9;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(9));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(9));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator RoundNoTrAsync()
    {
        currentHand = 9;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(9));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(9));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
        jocker.gameObject.SetActive(false);
    }

    IEnumerator Round12to15Async()
    {
        currentHand = 9;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(9));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(9));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator Round11and16Async()
    {
        currentHand = 8;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(8));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(8));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator Round10and17Async()
    {
        currentHand = 7;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(7));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(7));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator Round9and18Async()
    {
        currentHand = 6;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(6));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(6));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator Round8and19Async()
    {
        currentHand = 5;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(5));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(5));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator Round7and20Async()
    {
        currentHand = 4;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(4));
        StopCoroutine(DrawCardsEvent(4));
        yield return StartCoroutine(PlayHandsAsync(4));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator Round6and21Async()
    {
        currentHand = 3;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(3));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(3));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator Round5and22Async()
    {
        currentHand = 2;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(2));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(2));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    IEnumerator Round1to4Async()
    {
        currentHand = 1;
        SendRoundAndHandInfoEvent();
        Shuffle(deck);
        yield return StartCoroutine(DrawCardsEvent(1));
        StopCoroutine(DrawCardsEvent(0));
        yield return StartCoroutine(PlayHandsAsync(1));
        StopCoroutine(PlayHandsAsync(0));
        UpdateScore();
    }

    private void SendRoundAndHandInfoEvent()
    {
        object data = new object[] { currentRound, currentHand};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(SendRoundAndHandInfoEventCode, data, raiseEventOptions, SendOptions.SendReliable);
    }

    private void SendTurnPlayerEvent(int turnPlayerInt)
    {
        object data = turnPlayerInt;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(SendTurnPlayerEventCode, data, raiseEventOptions, SendOptions.SendReliable);
    }

    //Calculate and give scores to players after each round
    private void UpdateScore()
    {
        for (int i = 0; i < 4; i++)
        {
            int guess = int.Parse(playerInfoList[i, 2].ToString());
            int wins = int.Parse(playerInfoList[i, 3].ToString());

            //player guess more than 0 but does not win any
            if (guess > 0 && wins == 0)
            {
                playerInfoList[i, 4] = (int)playerInfoList[i, 4] - 10 * (blindRound ? 2 : 1) * guess;
            }

            else if (guess == wins)
            {
                playerInfoList[i, 4] = (int)playerInfoList[i, 4] + (guess * 5 + 5) * (blindRound ? 2 : 1);
            }
            else
            {
                playerInfoList[i, 4] = (int)playerInfoList[i, 4] + wins * (blindRound ? 2 : 1);
            }

            playerInfoList[i, 2] = -1;
            playerInfoList[i, 3] = 0;
        }
        UpdateScoresEvent();
    }

    private void UpdateScoresEvent()
    {
        object data = new object[] { playerInfoList[0,0],playerInfoList[1,0],playerInfoList[2,0],playerInfoList[3,0],playerInfoList[0,4],playerInfoList[1,4],playerInfoList[2,4], playerInfoList[3, 4] } ;                   
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(UpdateScoresEventCode, data, raiseEventOptions, SendOptions.SendReliable);
    }

    IEnumerator PlayHandsAsync(int cards)
    {
        currentQueue = startPlayer;

        //Send authority to send cards back and send current first card and trump card on table
        for (int i = 0; i < cards; i++)
        {
            bool firstPlayer = true;
            currentFirstCard = string.Empty;
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    if (k == currentQueue)
                    {
                        turnCircles[k].SetActive(true);
                    }
                    else
                    {
                        turnCircles[k].SetActive(false);
                    }
                }
                SendTurnPlayerEvent(currentQueue);
                //Clear this statement after tests
                turnPlayer.text = (string)playerInfoList[currentQueue, 0];

                SendAuthToSendCardEvent(firstPlayer);
                while (((string)playerInfoList[currentQueue, 6]).Equals(string.Empty))
                {
                    yield return new WaitForSeconds(1);
                }
                Debug.Log((string)playerInfoList[currentQueue, 0] + (string)playerInfoList[currentQueue, 6]);
                currentQueue = NextPlayerInt(currentQueue);
                firstPlayer = false;
            }
            ChooseWinnerAndReset();//Will reset currentSelectedCard
            tookHand.gameObject.SetActive(true);
            tookHand.text = string.Format("{0} took",playerInfoList[currentQueue,0].ToString());
            yield return new WaitForSeconds(3);
            /*Differ starts here*/
            for(int g= 0; g < 4; g++)
            {
                string cardName = (string)playerInfoList[g, 6];
                GameObject selected = GameObject.Find(cardName.Length == 4 ? cardName.Substring(0, 3) : cardName);
                selected.transform.position = DeckForPosition.transform.position;
                selected.GetComponent<Selectable>().faceUp = true;
                playerInfoList[g, 6] = string.Empty;
            }
            /*End here*/
            tookHand.gameObject.SetActive(false);
        }
    }

    // Calculate who takes hand and give message on screen after each hand
    private void ChooseWinnerAndReset()
    {
        char currentTrumpColor;
        char trumpSuit = currentTrumpCard[0];

        if (trumpSuit == 'C' || trumpSuit == 'S') currentTrumpColor = 'B';
        else currentTrumpColor = 'R';

        for(int i = 0; i<4; i++)//if first player played jock to lose
        {
            if (((string)playerInfoList[i, 6]).Length == 4)
            {
                if(((string)playerInfoList[i, 6])[3] != 'L')
                {
                    currentFirstCard = ((string)playerInfoList[i, 6])[3] + "5";
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            char suit = ((string)playerInfoList[i, 6])[0];

            //any card is joker
            if (suit == 'R' || suit == 'B')
            {
                //Player wants lose or win 
                if (((string)playerInfoList[i, 6]).Length == 4) // if length is 4 it means player wants to lose
                {
                    if(((string)playerInfoList[i, 6])[3] == 'L')
                    {
                        playerInfoList[i, 5] = 0;
                    }
                    else
                    {
                        //currentFirstCard = ((string)playerInfoList[i, 6])[3] + "5";
                        playerInfoList[i, 5] = 10;
                    }
                }
                else
                {
                    if(noTrumpRound){
                        if(suit == greaterJocker[0]) playerInfoList[i, 5] = 100;
                        else playerInfoList[i, 5] = 80;
                    }
                    else
                    {
                        if (suit == currentTrumpColor) playerInfoList[i, 5] = 100;
                        else playerInfoList[i, 5] = 80;
                    }
                }
            }

            //any card is trump
            else if (suit == trumpSuit)
            {
                for (int j = 1; j < 10; j++)
                {
                    if (values[j - 1].Equals(((string)playerInfoList[i, 6]).Substring(1)))
                    {
                        playerInfoList[i, 5] = 20 + j;
                        break;
                    }
                }
            }

            //any card has same suit with first card
            else if (suit == currentFirstCard[0])
            {
                for (int j = 1; j < 10; j++)
                {
                    if (values[j - 1].Equals(((string)playerInfoList[i, 6]).Substring(1)))
                    {
                        playerInfoList[i, 5] = 10 + j;
                        break;
                    }
                }
            }
            //any other card is nothing
            else
            {
                playerInfoList[i, 5] = 0;
            }
        }
        int max = -1;
        for (int i = 0; i < 4; i++)
        {
            if ((int)playerInfoList[i, 5] > max)
            {
                max = (int)playerInfoList[i, 5];
            }
        }
        for (int i = 0; i < 4; i++)
        {
            if ((int)playerInfoList[i, 5] == max)
            {
                playerInfoList[i, 3] = (int)playerInfoList[i, 3] + 1;
                currentQueue = i;
                WhoTakesHandEvent(currentQueue);
            }
            playerInfoList[i, 5] = 0;
            //string cardName = (string)playerInfoList[i, 6];
            //GameObject selected = GameObject.Find(cardName.Length == 4 ? cardName.Substring(0,3) : cardName);
            //selected.transform.position = DeckForPosition.transform.position;
            //selected.GetComponent<Selectable>().faceUp = true;
            //playerInfoList[i, 6] = string.Empty;
        }
        loseJock.gameObject.SetActive(false);
    }

    private void WhoTakesHandEvent(int winner)
    {
        object data = (string)playerInfoList[winner, 0];
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(WhoTakesHandEventCode, data, raiseEventOptions, SendOptions.SendReliable);
    }

    //EVENT -> CODE = 1
    //To send random cards. Numbers of card depend on hand 1 to 9
    IEnumerator DrawCardsEvent(int hand)
    {
        Debug.Log("Event Method : DrawCardsEvent");
        Debug.Log("PlayerList.Length : " + PhotonNetwork.PlayerList.Length);

        int index = 0;
        int temp = 0;
        int playerCount = startPlayer;
        while (temp < 4)
        {
            object[] player = new object[] { playerInfoList[playerCount, 0], playerInfoList[playerCount, 2], 0, 0, 0, 0, string.Empty };
            playerCount = NextPlayerInt(playerCount);
            Debug.Log("Hand :    " + hand);
            object[] dataToSend = deck.GetRange(index, hand).ToArray();
            foreach (object str in dataToSend)
            {
                Debug.Log((string)str);
            }

            //data is which player to recieve ,cards and blindRound boolean
            object data = new object[] { player, dataToSend, blindRound };

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(DrawCardsEventCode, data, raiseEventOptions, SendOptions.SendReliable);
            index += hand;

            temp++;
        }

        if (currentTrumpCard.Equals("NOT"))
        {
            GameObject.Find("BJk").transform.position = new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, 6f);
            GameObject.Find("RJk").transform.position = new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, 6f);
        }
        else if (!currentTrumpCard.Equals("NO") && !currentTrumpCard.Equals(string.Empty))
        {
            GameObject.Find(currentTrumpCard).transform.position = new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, 6f);
        }

        if (!noTrumpRound)
        {
            currentTrumpCard = deck[4 * hand];
            if (currentTrumpCard[0] == 'B' || currentTrumpCard[0] == 'R') // if trump card is joker then "NOT" trump card
            {
                GameObject.Find(currentTrumpCard).transform.position = new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, -0.5f);
                currentTrumpCard = "NOT";
            }
            else
            {
                GameObject.Find(currentTrumpCard).transform.position = new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, -0.5f);
            }
            Debug.Log("trump card  " + currentTrumpCard);
        }
        else
        {
            currentTrumpCard = "NO";
            int rand = new System.Random().Next(1);
            if (rand == 0)
            {
                greaterJocker = "BJk";
                jocker.text = "black";
                jocker.gameObject.SetActive(true);
            }
            else
            {
                greaterJocker = "RJk";
                jocker.text = "red";
                jocker.gameObject.SetActive(true);
            }
        }

        SendTrumpCardEvent(currentTrumpCard);

        playerCount = startPlayer;
        for (int i = 0; i < 4; i++)
        {
            SendAuthToGuessEvent(playerCount);
            Debug.Log("playerCounterForGuess : " + playerCounterForGuess);

            for (int k = 0; k < 4; k++)
            {
                if (k == playerCount)
                {
                    turnCircles[k].SetActive(true);
                }
                else
                {
                    turnCircles[k].SetActive(false);
                }
            }
            SendTurnPlayerEvent(playerCount);
            while (playerCounterForGuess == i)
            {
                Debug.Log("Wait for guesses!!!");
                
                //Clear this statement
                turnPlayer.text = (string)playerInfoList[playerCount, 0];
                
                yield return new WaitForSeconds(1);
            }
            playerCount = NextPlayerInt(playerCount);
        }
        playerCounterForGuess = 0;
        Debug.Log("All players has guessed");
    }

    private void SendTrumpCardEvent(string trumpCard)
    {
        object data = trumpCard;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(SendTrumpCardEventCode, data, raiseEventOptions, SendOptions.SendReliable);
    }


    // EVENT -> CODE = 5
    // Send auth each player on his/her turn to send card
    private void SendAuthToSendCardEvent(bool firstPlayer)
    {
        object[] datasome = new object[] { playerInfoList[currentQueue, 0], playerInfoList[currentQueue, 1], playerInfoList[currentQueue, 2], playerInfoList[currentQueue, 3], playerInfoList[currentQueue, 4], playerInfoList[currentQueue, 5], playerInfoList[currentQueue, 6] };
        Debug.Log("SendAuthToSendCardEvent");

        //data is current turn Player ( as object[] ), first card, trumpcard and boolean fisrtPlayer to play correctly
        object[] data = new object[] { datasome, currentFirstCard, currentTrumpCard, firstPlayer};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(SendAuthToSendCardEventCode, data, raiseEventOptions, SendOptions.SendReliable);
    }

    //EVENT -> CODE = 6
    // Send auth each player on his/her turn to guess
    private void SendAuthToGuessEvent(int playerToSend)
    {
        Debug.Log("SendAuthToGuessEvent");
        object[] player = new object[] { playerInfoList[playerToSend, 0], 0, 0, 0, 0, 0, string.Empty };
        Debug.Log((string)player[0]);
        Debug.Log( "playerCounterForGuess : " + playerCounterForGuess);
        //data is player to send, count of players guessed till now and guesses sum
        object[] data = new object[] { player, playerCounterForGuess, CountGuesses() };

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(SendGuessAuthCode, data, raiseEventOptions, SendOptions.SendReliable);
    }

    //Count all players guesses to inform players play correctly
    private int CountGuesses()
    {
        int counter = 0;
        for (int i = 0; i < 4; i++)
        {
            int value = int.Parse(playerInfoList[i, 2].ToString());
            counter += (value == -1 ? 0 : value);
        }
        Debug.Log("guesses counter : " + counter);
        return counter;
    }

    //Generate deck of all cards on the beginning of game
    public static List<string> GenerateDeckServer()
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

    //Shuffle deck array randomly
    void Shuffle<T>(List<T> list)
    {
        System.Random random = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            int k = random.Next(n);
            n--;
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    //Create cards on scene but not shown on screen
    void JockDrawServer()
    {
        float zOffset = 0.03f;
        foreach (string card in deck)
        {
            //GameObject newCard = PhotonNetwork.Instantiate("Card", new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, DeckForPosition.transform.position.z + zOffset), Quaternion.identity);
            GameObject newCard = Instantiate(cardPrefab, new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, DeckForPosition.transform.position.z + zOffset), Quaternion.identity);
            newCard.name = card;
            newCard.GetComponent<Selectable>().faceUp = true;
            zOffset += 0.03f;
        }
    }

    //Method which receive players methods coming via network 
    public void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;

        //PlaceGuessEvent - Code = 2
        if (eventCode == PlaceGuessEventCode)
        {
            object[] playerInfo = (object[])photonEvent.CustomData;
            //CardPlayerInfo playerInfo = (CardPlayerInfo)photonEvent.CustomData;
            for (int i = 0; i < 4; i++)
            {
                object[] first = new object[] { playerInfoList[i, 0], playerInfoList[i, 0] };
                object[] second = new object[] { playerInfo[0], playerInfo[1] };
                if (PlayersEquals(first, second))
                {
                    playerInfoList[i, 2] = playerInfo[2];
                    playerCounterForGuess++;
                    break;
                }
            }
        }

        //SendMyInfoToMasterEvent - Code = 3
        else if (eventCode == SendMyInfoToMasterEventCode)
        {
            object[] customDataPlayer = (object[])photonEvent.CustomData;
            byte count = CountPlayers();
            if (count < 4)
            {
                Debug.Log("Add Player Method");
                AddPlayer(count, customDataPlayer);
            }

            //DEBUG -- clear else if
            //else if (count == 4)
            //{
            //    for (int i = 0; i < 4; i++)
            //    {
            //        for (int j = 0; j < 7; j++)
            //        {
            //            if (j == 0 || j == 6)
            //            {
            //                Debug.Log((string)playerInfoList[i, j]);
            //            }
            //            else
            //            {
            //                Debug.Log(playerInfoList[i, j]);
            //            }
            //        }
            //    }
            //}
        }

        //SendCardEvent - Code = 4
        else if (eventCode == SendCardEventCode)//Check jock lose   L   
        {
            //custom data: Sender and selected card
            object[] sender = (object[])((object[])photonEvent.CustomData)[0];

            string selectedCard = (string)((object[])photonEvent.CustomData)[1];
            Debug.Log(selectedCard);
            if (playerCounterForfirstCard == 0) currentFirstCard = selectedCard;
            playerCounterForfirstCard++;
            if (playerCounterForfirstCard == 4) playerCounterForfirstCard = 0;

            if (playerCounterForfirstCard == 1 && selectedCard.Length == 4)
            {
                loseJock.gameObject.SetActive(true);
                if(selectedCard[3] == 'C')
                {
                    loseJock.text = "♣";
                }
                else if(selectedCard[3] == 'D')
                {
                    loseJock.text = "♦";
                }
                else if (selectedCard[3] == 'S')
                {
                    loseJock.text = "♠";
                }
                else
                {
                    loseJock.text = "♥";
                }
            }

            for(int i = 0; i < 4; i++)
            {
                Debug.Log(playerInfoList[i, 0].ToString());
            }

            object[] first = new object[] { sender[0], sender[1] };
            for (int i = 0; i < 4; i++)
            {
                object[] second = new object[] { playerInfoList[i, 0], playerInfoList[i, 0] };
                Debug.Log("Players equal? : " + PlayersEquals(first, second));
                if (PlayersEquals(first, second))
                {
                    playerInfoList[i, 6] = selectedCard;
                    GameObject selected;
                    if (selectedCard.Length == 4)
                    {
                        selected = GameObject.Find(selectedCard.Substring(0, 3));
                        selected.GetComponent<Selectable>().faceUp = false;
                    }
                    else
                    {
                        selected = GameObject.Find(selectedCard);
                    }
                    selected.transform.position = new Vector3(playerCardsPlaces[i].transform.position.x, playerCardsPlaces[i].transform.position.y, 0);
                }
            }
        }

        //Code = 7
        else if (eventCode == WhoTakesHandEventCode)
        {
            Debug.Log("Reset Method");
            //int winner = int.Parse(photonEvent.CustomData.ToString());
            for (int i = 0; i < 4; i++)
            {
                playerInfoList[i, 5] = 0;
                string cardName = (string)playerInfoList[i, 6];
                Debug.Log(cardName);
                GameObject selected = GameObject.Find(cardName.Length == 4 ? cardName.Substring(0, 3) : cardName);
                selected.transform.position = DeckForPosition.transform.position;
                selected.GetComponent<Selectable>().faceUp = true;
                playerInfoList[i, 6] = string.Empty;
            }
        }

        //Code = 9
        else if (eventCode == ClonServerStartGameEventCode)
        {

            deck = GenerateDeckServer();
            JockDrawServer();
            BeforeGameCanvas.enabled = false;

            for (int i = 0; i < 4; i++)
            {
                playerNames[i].text = (string)playerInfoList[i, 0];
            }
        }

        //Code = 10
        else if (eventCode == SendRoundAndHandInfoEventCode)
        {
            currentRound = int.Parse(((object[])photonEvent.CustomData)[0].ToString());
            currentHand = int.Parse(((object[])photonEvent.CustomData)[1].ToString());
        }

        //Code = 11
        else if (eventCode == SendTurnPlayerEventCode)
        {
            int turnPlayer = int.Parse(photonEvent.CustomData.ToString());
            for (int i = 0; i < 4; i++)
            {
                if (i == turnPlayer)
                {
                    turnCircles[i].SetActive(true);
                }
                else
                {
                    turnCircles[i].SetActive(false);
                }
            }
        }
        else if (eventCode == SendTrumpCardEventCode) 
        {
            string trumpCard = photonEvent.CustomData.ToString(); // Can be any Card , "NO" and "NOT"

            Debug.Log("gelen trump : " + trumpCard);
            Debug.Log("kohne trump : " + currentTrumpCardClonServer);

            if (!currentTrumpCardClonServer.Equals(string.Empty) && !currentTrumpCardClonServer.Equals("NO") && !currentTrumpCardClonServer.Equals("NOT"))
            {
                GameObject.Find(currentTrumpCardClonServer).transform.position = new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, 6f);
            }

            if(!trumpCard.Equals("NO") && !trumpCard.Equals("NOT"))
            {
                GameObject.Find(trumpCard).transform.position = new Vector3(DeckForPosition.transform.position.x, DeckForPosition.transform.position.y, -0.5f);
            }
            currentTrumpCardClonServer = trumpCard;
        }
    }

    // Add new player when player joins room
    private void AddPlayer(byte count, object[] customDataPlayer)
    {
        for (int i = 0; i < 6; i++)
        {
            playerInfoList[count, i] = customDataPlayer[i];
        }
        playerInfoList[count, 1] = count;
    }

    private byte CountPlayers()
    {
        byte count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (((string)playerInfoList[i, 0]).Equals(string.Empty))
            {
                return count;
            }
            else count++;
        }
        return count;
    }

    private int RandomPlayerInt()
    {
        return new System.Random().Next(4);
    }



    private int NextPlayerInt(int curr)
    {
        return curr == 3 ? 0 : curr + 1;
    }
    private int PrevPlayerInt(int curr)
    {
        return curr == 0 ? 3 : curr - 1;
    }

    private bool PlayersEquals(object[] first, object[] second)
    {
        if (((string)first[0]).Equals((string)second[0])) return true;
        //else if (!(first[1] == second[1])) return false;
        else return false;
    }
}


//****************OLD CODE**************************
//private void DrawCardsEvent(int hand)
//{
//    Debug.Log("Before RPC");
//    Debug.Log(PhotonNetwork.playerList.Length);
//    int index = 0;
//    for (int i = 1; i < PhotonNetwork.playerList.Length/*MUST BE FIVE - 5 CHECK THAT*/; i++)
//    {
//        clientPhotonView.RPC("getMyCards", PhotonNetwork.playerList[1/*i*/], deck.GetRange(index, hand).ToArray() as object);
//        index += hand;
//    }
//}


//for (int i = 1/*startPlayer*/; i < 5/*PhotonNetwork.PlayerList.Length/*MUST BE |FIVE - 5| CHECK THAT*/; i = NextPlayerInt(i))
//{
//    object[] player = new object[] { playerInfoList[i - 1, 0], playerInfoList[i - 1, 2], null, null, null, null };
//    Debug.Log("Hand :    " + hand);
//    object[] dataToSend = deck.GetRange(index, hand).ToArray();
//    foreach(object str in dataToSend)
//    {
//        Debug.Log((string)str);
//    }

//    //data is which player to recieve and cards
//    object[] data = new object[] { player, dataToSend };

//    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
//    PhotonNetwork.RaiseEvent(DrawCardsEventCode, data, raiseEventOptions, SendOptions.SendReliable);
//    index += hand;
//}