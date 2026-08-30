using System;
using Chatterbox.Runtime.Scripts.Generics;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource _audioSource;
    
    private void OnEnable()
    {
        ChatterBehaviour.OnChatterTrigger += PlaySound; 
    }

    private void OnDisable()
    {
 
        ChatterBehaviour.OnChatterTrigger -= PlaySound;
    }

    public void PlaySound(AudioClip clip, float pitch)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip);
        }
    }



}