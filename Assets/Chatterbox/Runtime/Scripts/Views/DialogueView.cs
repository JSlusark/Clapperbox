using TMPro;
using UnityEngine;

public class DialogueView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayedText;
    private string _line;
    private int _pointer;

    private void OnEnable()
    {
        // Anonymous delegate - made via lambdas and uses (_, _) to ignore the 2 params
        ChatterBehaviour.OnChatterTrigger += (_, _) => DisplayText();
    }

    private void OnDisable()
    {
        ChatterBehaviour.OnChatterTrigger -= (_, _) => DisplayText();
    }
 
    public void SetLine(string fullText)
    {
        if (displayedText != null) displayedText.text = "";  // safety check in case text component has placeholder text or line was not completed before new load
        // Debug.Log("[DialogueView] Line set: " + fullText);
        _pointer = 0;
        _line = fullText;
    }

    private void DisplayText()
    {
        if (displayedText == null || _line == null || _pointer >= _line.Length) return;  // safety check to ensure no remaining characters pending
        
        // Debug.Log($"[DialogueView] Displaying text from ChatterTrigger: {displayedText.text}");
        displayedText.text = displayedText.text + _line[_pointer++];
      
    }

}