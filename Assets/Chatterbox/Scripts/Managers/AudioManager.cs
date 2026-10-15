using Chatterbox.Runtime.Scripts.Generics;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource _audioSource;
    
    private void OnEnable()
    {
        DialogueManager.OnPerformance += PlayVoice; 
        DialogueManager.OnLineSkipped += StopVoice;
    }

    private void OnDisable()
    {
 
        DialogueManager.OnPerformance -= PlayVoice;
        DialogueManager.OnLineSkipped -= StopVoice;
    }

    private void PlayVoice(ChatterLine line, AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.pitch = line.Pitch;
            _audioSource.PlayOneShot(clip);
        }
    }

    // Stop() also cuts clips started with PlayOneShot on this source
    private void StopVoice()
    {
        if (_audioSource != null) _audioSource.Stop();
    }




}