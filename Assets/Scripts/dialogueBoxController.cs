using DefaultNamespace;
using UnityEngine;

public class DialogueBoxController : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;

    // [SerializeField] private string[] dialogue;
    [SerializeField] private dialogueView dialogueView;

    private int _current = 0;

    private void Awake()
    {
        dialogueView.DisplayDialogue(dialogueData.Dialogue[_current], dialogueData.Sound);
    }

    public void DisplayNext()
    {
        string[] dialogue = dialogueData.Dialogue;
        
        if (_current < dialogue.Length - 1)
            dialogueView.DisplayDialogue(dialogue[++_current], dialogueData.Sound);
    }
}