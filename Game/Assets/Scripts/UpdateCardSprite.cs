using System.Collections.Generic;
using UnityEngine;

/*
    Update cards' sprites correctly due to their index in deck array
 */
public class UpdateCardSprite : MonoBehaviour
{
    public Sprite cardFace;
    public Sprite cardBack;
    private SpriteRenderer spriteRenderer;
    private Selectable selectable;
    private Jock jock;

    // Start is called before the first frame update
    void Start()
    {
        List<string> deck = Jock.GenerateDeckServer();
        jock = FindObjectOfType<Jock>();

        int i = 0;
        foreach(string card in deck)
        {
            if(this.name == card)
            {
                cardFace = jock.cardFaces[i];
                break;
            }
            i++;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        selectable = GetComponent<Selectable>();
    }

    // Update is called once per frame
    void Update()
    {
        if(selectable.faceUp == true)
        {
            spriteRenderer.sprite = cardFace;
        }
        else
        {
            spriteRenderer.sprite = cardBack;
        }
    }
}
