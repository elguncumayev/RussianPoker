using UnityEngine;
using Photon.Pun;

public class ServerGame : MonoBehaviour
{
    public GameObject startButton;
    // Start is called before the first frame update
    void Start()
    {
        if (PhotonNetwork.LocalPlayer.NickName.Equals("CLONSERVER"))
        {
            startButton.SetActive(false);
        }
    }

}
