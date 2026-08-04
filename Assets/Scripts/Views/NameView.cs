using UnityEngine;
using TMPro;

public class NameView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;

    public void Show(string text, Color color)
    {
        if (text != null)
        {
            nameText.text = text;
            nameText.color = color;
        }
    }

}
