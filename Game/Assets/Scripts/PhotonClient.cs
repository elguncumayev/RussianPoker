using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

/*
 Online networking multiplayer settings for client
 */
public class PhotonClient : MonoBehaviourPunCallbacks
{
    public Camera mainCamera;
    public Canvas menu,controls;
    public GameObject player;
    public TMP_InputField roomName, playerName;
    public GameObject gameScripts;
    public Camera playerCamera;

    public object[] myCardMyPlayerInfo = new object[6];

    private readonly byte SendMyInfoToMasterEventCode = 3;
    // Start is called before the first frame update
    void Awake()
    {
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.NetworkingClient.AppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;  
    }

    public override void OnConnectedToMaster()
    {
        //base.OnConnectedToMaster();
        Debug.Log("OnMaster");
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }
    public override void OnJoinedLobby()
    {
        Debug.Log("Client: Joined Lobby");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("failed to join room");
        base.OnJoinRoomFailed(returnCode, message);
    }

    public void JoinGame()
    {
        //MAIN CODE
        PhotonNetwork.JoinRoom(roomName.text);
        
        //DEBUGGING PURPOSE
        //PhotonNetwork.JoinOrCreateRoom("room", new RoomOptions() { MaxPlayers = 4 }, TypedLobby.Default,null);/*roomName.text);*/
        
        //name on PhotonNetwork
        PhotonNetwork.LocalPlayer.NickName = playerName.text;
    }

    public override void OnJoinedRoom()
    {
        controls.gameObject.SetActive(true);

        //Create new online CardPLayer
        int countOfPlayers = PhotonNetwork.PlayerList.Length;
        GameObject player = PhotonNetwork.Instantiate("CardPlayer", new Vector3(-25, countOfPlayers * 15, -2), Quaternion.identity, 0);
        player.name = PhotonNetwork.LocalPlayer.NickName;
        playerCamera.gameObject.SetActive(true);

        object[] playerInfo = new object[] { PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.ActorNumber ,0,0,0,0,string.Empty};
        
        SendMyInfoToMaster(playerInfo);
        
        myCardMyPlayerInfo = playerInfo;

        mainCamera.gameObject.SetActive(false);
        menu.gameObject.SetActive(false);
    }

    private void SendMyInfoToMaster(object[] playerInfo)
    {
        //Custom data is CardPlayerInfo 
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(SendMyInfoToMasterEventCode, playerInfo, raiseEventOptions, SendOptions.SendReliable);
    }


}