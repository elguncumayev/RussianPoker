using Photon.Pun;
using TMPro;
using UnityEngine;

public class BeforeGame : MonoBehaviour
{
    public TMP_Text staticText;
    public TMP_Text[] playerNameList;

    // Update is called once per frame
    void Update()
    {
        int playercount = 0;
        //Show which player joined room before game started
        staticText.text = string.Format("joined players: {0}",PhotonNetwork.PlayerList.Length-1 == -1 ? 0 : PhotonNetwork.PlayerList.Length - 1);
        for(int i = 1; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (!PhotonNetwork.PlayerList[i].NickName.Equals("CLONSERVER"))
            {
                staticText.text = string.Format("joined players: {0}", playercount+1/*PhotonNetwork.PlayerList.Length - 1 == -1 ? 0 : PhotonNetwork.PlayerList.Length - 1*/);
                playerNameList[playercount].text = PhotonNetwork.PlayerList[i].NickName;
                playercount++;
            }
        }
    }
}
