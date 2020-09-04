using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SliderScript : MonoBehaviour
{
    public TMP_Text text;
    public Slider slider;

    // Update is called once per frame
    void Update()
    {
        // Guess sider's text update
        text.text = slider.value.ToString();
    }
}
