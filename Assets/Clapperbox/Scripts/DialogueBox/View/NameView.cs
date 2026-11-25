using UnityEngine;
using TMPro;

public class NameView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    // Off for styled dialogue boxes, whose name colour is part of the design
    [SerializeField] private bool useSpeakerColor = true;

    public void Show(string text, Color color)
    {
        if (text != null)
        {
            nameText.text = text;
            if (useSpeakerColor) nameText.color = color;
        }
    }

}
