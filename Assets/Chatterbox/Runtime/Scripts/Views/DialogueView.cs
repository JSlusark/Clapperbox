using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayedText;
    private string _line;
    private int _pointer;

    private void OnEnable()
    {
        ChatterBehaviour.OnChatterTrigger += DisplayText; // ChatterBehaviour.Instance not needed as static
    }

    private void OnDisable()
    {
        ChatterBehaviour.OnChatterTrigger -= DisplayText;
    }
 
    public void SetLine(string fullText)
    {
        if (displayedText != null) displayedText.text = "";  // safety check in case text component has placeholder text or line was not completed before new load
        // Debug.Log("[DialogueView] Line set: " + fullText);
        _pointer = 0;
        _line = fullText;
    }

    private void DisplayText(AudioClip soundClip)
    {
        if (displayedText == null || _line == null || _pointer >= _line.Length) return;  // safety check to ensure no remaining characters pending
        
        // Debug.Log($"[DialogueView] Displaying text from ChatterTrigger: {displayedText.text}");
        displayedText.text = displayedText.text + _line[_pointer++];
      
    }

}