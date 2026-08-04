using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayedText;
    [SerializeField] private float _textSpeed;
    [SerializeField] private float _soundFrequency;
   
    private Coroutine _typeWriteCoroutine;
    
    public void Show(string text, AudioClip sound)
    {
        if (_typeWriteCoroutine != null) StopCoroutine(_typeWriteCoroutine);
        _typeWriteCoroutine = StartCoroutine(TypeWriteRoutine(text, sound));
    }


    private IEnumerator TypeWriteRoutine(string text, AudioClip sound)
    {
        if (displayedText == null) yield break;

        displayedText.text = text;
        displayedText.maxVisibleCharacters = 0;

        for (int i = 0; i < text.Length; i++)
        {
            displayedText.maxVisibleCharacters++;
            if (i % _soundFrequency == 0 && sound != null) 
                AudioManager.Instance.PlaySound(sound);
            
            yield return new WaitForSeconds(_textSpeed);
        }
    }
}