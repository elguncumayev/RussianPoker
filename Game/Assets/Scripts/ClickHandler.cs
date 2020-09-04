using UnityEngine;
using UnityEngine.EventSystems;

public class ClickHandler : MonoBehaviour,IPointerDownHandler
{
    public GameObject sendCardButton;
    public GameObject InGameTools;
    public GameObject[] jokerWinLoseButtons;

    //Click handler on cards: to select, deselect and change positions as well
    public void OnPointerDown(PointerEventData eventData)
    {
        string selectedCard = InGameTools.GetComponent<InGameToolsScript>().selectedCard;

        //if selected card is empty select it
        if (selectedCard.Equals(string.Empty))
        {
            InGameTools.GetComponent<InGameToolsScript>().selectedCard = name;
            if (name[0] == 'R' || name[0] == 'B')
            {
                jokerWinLoseButtons[0].SetActive(true);
                jokerWinLoseButtons[1].SetActive(true);
                sendCardButton.SetActive(false);
            }
            else
            {
                jokerWinLoseButtons[0].SetActive(false);
                jokerWinLoseButtons[1].SetActive(false);
                sendCardButton.SetActive(true);
            }
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        }
        //if selected card different from clicked card deselect it and select new one
        else if (!selectedCard.Equals(name))
        {
            if (name[0] == 'R' || name[0] == 'B')
            {
                jokerWinLoseButtons[0].SetActive(true);
                jokerWinLoseButtons[1].SetActive(true);
                sendCardButton.SetActive(false);
            }
            else
            {
                jokerWinLoseButtons[0].SetActive(false);
                jokerWinLoseButtons[1].SetActive(false);
                sendCardButton.SetActive(true);
            }
            GameObject lastSelectedCard = GameObject.Find(selectedCard);
            lastSelectedCard.transform.position = new Vector3(lastSelectedCard.transform.position.x, lastSelectedCard.transform.position.y - 0.5f, lastSelectedCard.transform.position.z);
            InGameTools.GetComponent<InGameToolsScript>().selectedCard = name;
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        }
        //if clicked on selected card second time deselect it
        else if (selectedCard.Equals(name))
        {
            jokerWinLoseButtons[0].SetActive(false);
            jokerWinLoseButtons[1].SetActive(false);
            sendCardButton.SetActive(false);
            transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
            InGameTools.GetComponent<InGameToolsScript>().selectedCard = string.Empty;
        }
        Debug.Log("this is " + this.gameObject.name);
    }

    //When all cards in hand gone deselect last card
    public void GuessButtonDeselect()
    {
        InGameTools.GetComponent<InGameToolsScript>().selectedCard = string.Empty;
    }
}