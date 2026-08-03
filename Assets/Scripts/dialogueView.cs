using System.Collections;
using TMPro;
using UnityEngine;

public class dialogueView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayedText;
    [SerializeField] private float _textSpeed;
    private Coroutine _typeWriteCoroutine;

    public void DisplayDialogue(string text)
    {
        if (_typeWriteCoroutine != null)
        {
            StopCoroutine(_typeWriteCoroutine);
        }

        _typeWriteCoroutine = StartCoroutine(TypeWriteRoutine(text));
    }


    private IEnumerator TypeWriteRoutine(string text)
    {
        displayedText.text = text;
        int totalCharacters = text.Length;
        displayedText.maxVisibleCharacters = 0;

        displayedText.ForceMeshUpdate(); // this function 
        for (int i = 0; i < totalCharacters; i++)
        {
            displayedText.maxVisibleCharacters += 1;
            // send signal to chatter manager here? chatter needs to be 2 diff tones that are not that far from eachother
            yield return new WaitForSeconds(_textSpeed);
        }
    }
}