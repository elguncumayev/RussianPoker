using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BeforeGame : MonoBehaviour
{
    public TMP_Text staticText;
    public TMP_Text[] playerNameList;

    // Update is called once per frame
    void Update()
    {
        //Show which player joined room before game started
        staticText.text = string.Format("joined players: {0}",PhotonNetwork.PlayerList.Length-1 == -1 ? 0 : PhotonNetwork.PlayerList.Length - 1);
        for(int i = 1; i < PhotonNetwork.PlayerList.Length; i++)
        {
            playerNameList[i-1].text = PhotonNetwork.PlayerList[i].NickName;
        }
    }
}
