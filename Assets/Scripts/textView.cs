using TMPro;
using UnityEngine;

public class textView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayedText;
    
    public void ShowText(string text)
    {
        displayedText.text = text;
    }
}
