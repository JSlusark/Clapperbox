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
        if (clip == null || _audioSource == null || line.SoundLength <= 0f) return;

        // Play (not PlayOneShot) so the blip can be cut at the style's sound limit
        double now = AudioSettings.dspTime;
        _audioSource.clip = clip;
        _audioSource.pitch = line.Pitch;
        _audioSource.PlayScheduled(now);
        _audioSource.SetScheduledEndTime(now + line.SoundLength);
    }

    // Cuts the current blip when a line is skipped
    private void StopVoice()
    {
        if (_audioSource != null) _audioSource.Stop();
    }




}