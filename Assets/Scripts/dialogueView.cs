using System.Collections;
using TMPro;
using UnityEngine;

public class dialogueView : MonoBehaviour
{
    [SerializeField] private AudioManager AudioManager;
    [SerializeField] private TextMeshProUGUI displayedText;
    [SerializeField] private float _textSpeed;
    [SerializeField] private float _soundFrequency;
    private Coroutine _typeWriteCoroutine;
    
    public void DisplayDialogue(string text, AudioClip sound)
    {
        if (_typeWriteCoroutine != null)
        {
            StopCoroutine(_typeWriteCoroutine);
        }

        _typeWriteCoroutine = StartCoroutine(TypeWriteRoutine(text, sound));
    }


    private IEnumerator TypeWriteRoutine(string text, AudioClip sound)
    {
        displayedText.text = text;
        displayedText.maxVisibleCharacters = 0;
        int totalCharacters = text.Length;

        displayedText.ForceMeshUpdate(); // this function 
        for (int i = 0; i < totalCharacters; i++)
        {
            displayedText.maxVisibleCharacters++;
            if (i % _soundFrequency == 0) 
                AudioManager.Instance.PlaySound(sound);
            
            yield return new WaitForSeconds(_textSpeed);
        }
    }
}