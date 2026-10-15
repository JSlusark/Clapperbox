using TMPro;
using UnityEngine;

public class DialogueView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayedText;
    private string _text;
    private int _pointer;

    private void OnEnable()
    {
        DialogueManager.OnPerformance += DisplayText;
    }

    private void OnDisable()
    {
        DialogueManager.OnPerformance -= DisplayText;
    }
 
    public void SetLine(string fullText)
    {
        if (displayedText != null) displayedText.text = "";  // safety check in case text component has placeholder text or line was not completed before new load
        // Debug.Log("[DialogueView] Line set: " + fullText);
        _pointer = 0;
        _text = fullText;
    }

    // Named method (not a lambda) so OnDisable can actually unsubscribe it
    private void DisplayText(ChatterLine line, AudioClip _)
    {
        if (displayedText == null || _text == null || _pointer >= _text.Length) return;  // safety check to ensure no remaining characters pending

        int count = Mathf.Min(line.Chunk, _text.Length - _pointer);
        displayedText.text += _text.Substring(_pointer, count);
        _pointer += count;
    }

}