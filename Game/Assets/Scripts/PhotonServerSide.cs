using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/*
 Online multiplayer settings for Server
 */
public class PhotonServerSide : MonoBehaviourPunCallbacks
{
    public GameObject game;
    public Canvas menu;

    public TMP_InputField RoomName, RoomPass;
    private void Awake()
    {
        Debug.Log("PhotonServerAwake");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.NetworkingClient.AppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
    }

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }
    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(RoomName.text, new RoomOptions() { MaxPlayers = 6}, TypedLobby.Default);
        PhotonNetwork.LocalPlayer.NickName = "SERVER";
    }

    public void JoinAsClonServer()
    {
        PhotonNetwork.JoinRoom(RoomName.text);
        PhotonNetwork.LocalPlayer.NickName = "CLONSERVER";
        Debug.Log(PhotonNetwork.PlayerList.Length);
    } 

    public override void OnConnectedToMaster()
    {
        //base.OnConnectedToMaster();
        Debug.Log("OnMaster");
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Server: You are on Lobby");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("You are on Room; Room Name: " + PhotonNetwork.CurrentRoom.Name);
        if (PhotonNetwork.LocalPlayer.NickName.Equals("CLONSERVER"))
        {
            if(PhotonNetwork.PlayerList.Length == 2)
            {
                game.SetActive(true);
                menu.enabled = false;
            }
            else
            {
                PhotonNetwork.LeaveRoom();
            }
        }
        else
        {
            game.SetActive(true);
            menu.enabled = false;
        }
    }
    public override void OnLeftRoom()
    {
        Debug.Log("Left Room");
    }
}
