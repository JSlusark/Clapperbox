using UnityEngine;

    public class DialogueBoxController : MonoBehaviour
    {
        [SerializeField] private string[] dialogues;
        [SerializeField] private dialogueView dialogueView;
        
        private int _current = 0;

        private void Awake()
        {
            dialogueView.DisplayDialogue(dialogues[_current]);    
        }

        public void DisplayNext()
        {
            dialogueView.DisplayDialogue(_current < dialogues.Length - 1 ? dialogues[++_current] : "end of array");
        }
        
    }