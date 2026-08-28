using System.Collections;
using System;
using UnityEngine;


public abstract class ChatterBehaviour
{
    // Props meant to be stateless (for now )
    protected AudioClip Sound;
    protected float TextSpeed;
    protected float SoundFrequency;
    protected int characters;

    // protected Coroutine routine;

    public void SetSound(AudioClip sound)
    {
        Sound = sound;
    }

    public abstract void Execute(string text, SpeakerData data);

    public static event Action<AudioClip> OnChatterTrigger;

    public void EmitChatterTrigger()
    {
        OnChatterTrigger?.Invoke(Sound);
    }
    
    protected IEnumerator StyleEmitRoutine(string text, AudioManager audioManager) // TODO: Override in intro and fullword 
    {
        for (int i = 0; i < text.Length; i++) // i++ is the n of bl
        {
            EmitChatterTrigger();
            // Debug.Log($"[Monotone] {text[i]}");
            yield return new WaitForSeconds(TextSpeed);
        }

        yield return null;
    }
}

public class MonotoneSpeech : ChatterBehaviour
{
    public MonotoneSpeech()
    {
        TextSpeed = 0.02f;
        SoundFrequency = 0.05f;
        characters = 0;
    }

    public override void Execute(string text, SpeakerData data)
    {
        // Debug.Log($"[Monotone] {data.speakerName} speaking: {text}");
        if (text == null) return;
        DialogueManager.RunCoroutine(StyleEmitRoutine(text, AudioManager.Instance));
    }


}

// public class DuotoneSpeech : ChatterBehaviour
// {
//     public DuotoneSpeech()
//     {
//         // TextSpeed = 0.02f;
//         // SoundFrequency = 0.05f;
//     }
//
//     public override void Execute(string text, SpeakerData data, AudioManager audioManager)
//     {
//         // Logic: Play audio for every character
//         Debug.Log($"[Monotone] {data.speakerName} speaking: {text}");
//     }
// }
//
// public class AnimaleseSpeech : ChatterBehaviour
// {
//     public AnimaleseSpeech()
//     {
//         // TextSpeed = 0.02f;
//         // SoundFrequency = 0.05f;
//     }
//
//     public override void Execute(string text, SpeakerData data, AudioManager audioManager)
//     {
//         // Logic: Play audio for every character
//         Debug.Log($"[Monotone] {data.speakerName} speaking: {text}");
//     }
// }
//
// public class FullWordSpeech : ChatterBehaviour
// {
//     public FullWordSpeech()
//     {
//         // TextSpeed = 0.02f;
//         // SoundFrequency = 0.05f;
//     }
//
//     public override void Execute(string text, SpeakerData data, AudioManager audioManager)
//     {
//         // Logic: Play audio for every character
//         Debug.Log($"[Monotone] {data.speakerName} speaking: {text}");
//     }
// }
//
// public class IntroSpeech : ChatterBehaviour
// {
//     public IntroSpeech()
//     {
//         // TextSpeed = 0.02f;
//         // SoundFrequency = 0.05f;
//     }
//
//     public override void Execute(string text, SpeakerData data, AudioManager audioManager)
//     {
//         // Logic: Play audio for every character
//         Debug.Log($"[Monotone] {data.speakerName} speaking: {text}");
//     }
// }