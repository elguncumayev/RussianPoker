using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonController : MonoBehaviour
{
    public Canvas BeforeGameCanvas;

    //Method to disable before game canvas on start button click
    public void AllPlayersJoinStart()
    {
        Debug.Log("AllPlayersJoinStart");
        BeforeGameCanvas.enabled = false;
    }
}
