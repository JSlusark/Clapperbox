using DefaultNamespace;
using UnityEngine;

public class DialogueBoxController : MonoBehaviour
{
    [SerializeField] private characterBox characterBox;

    // [SerializeField] private string[] dialogue;
    [SerializeField] private dialogueView dialogueView;

    private int _current = 0;

    private void Awake()
    {
        dialogueView.DisplayDialogue(characterBox.Dialogue[_current]);
    }

    public void DisplayNext()
    {
        string[] dialogue = characterBox.Dialogue;
        
        if (_current < dialogue.Length - 1)
            dialogueView.DisplayDialogue(dialogue[++_current]);
    }
}