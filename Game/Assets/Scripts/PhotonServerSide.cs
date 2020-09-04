using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/*
 Online multiplayer settings for Server
 */
public class PhotonServerSide : MonoBehaviourPunCallbacks
{
    public TMP_InputField RoomName, RoomPass;
    private void Awake()
    {
        Debug.Log("PhotonServerAwake");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.NetworkingClient.AppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
        PhotonNetwork.JoinLobby(TypedLobby.Default);
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
        PhotonNetwork.CreateRoom(RoomName.text, new RoomOptions() { MaxPlayers = 5}, TypedLobby.Default);
        PhotonNetwork.LocalPlayer.NickName = "SERVER";
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("OnMaster");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Server: You are on Lobby");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("You are on Room; Room Name: " + PhotonNetwork.CurrentRoom.Name);
    }
}
