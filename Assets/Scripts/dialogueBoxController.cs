using UnityEngine;

    public class DialogueBoxController : MonoBehaviour
    {
        [SerializeField] private string[] dialogues;
        [SerializeField] private textView textView;
        
        private int _current = 0;

        private void Awake()
        {
            textView.ShowText(dialogues[_current]);    
        }

        public void DisplayNext()
        {
            textView.ShowText(_current < dialogues.Length - 1 ? dialogues[++_current] : "end of array");
        }
        
    }