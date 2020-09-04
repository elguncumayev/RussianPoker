using UnityEngine;

public class ServerClientStarter : MonoBehaviour
{

    private string role = "SERVER";        //"CLIENT" 
    public GameObject server, client;


    /*
     Game will start due to role:
     SERVER or not;
     */
    private void Awake()
    {
        if (role.Equals("SERVER"))
        {
            server.SetActive(true);
        }
        else
        {
            client.SetActive(true);
        }
    }
}