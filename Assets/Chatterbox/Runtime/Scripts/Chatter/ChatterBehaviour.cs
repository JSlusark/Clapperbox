using System.Collections;
using System;
using UnityEngine;


public abstract class ChatterBehaviour
{
    // Props meant to be stateless (for now )
    private AudioClip _sound;
    protected float TextSpeed;


    public void SetSound(AudioClip sound)
    {
        _sound = sound;
    }

    // Virtual defines standard execution and pitch to avoid repeating logic
    public virtual void Execute(string text, SpeakerData data)
    {
        if (text == null) return;
        DialogueManager.RunCoroutine(StyleEmitRoutine(text.Length));
    }
    
    protected virtual float GetSoundPitch(int index) 
    {
        return 1f;
    }


    public static event Action<AudioClip, float> OnChatterTrigger;

    private void EmitChatterTrigger(float pitch)
    {
        OnChatterTrigger?.Invoke(_sound, pitch);
    }
    
    protected IEnumerator StyleEmitRoutine(int SoundRepeats)  
    {
        for (int i = 0; i  < SoundRepeats ; i++) // i++ is the n of bl
        {
            EmitChatterTrigger(GetSoundPitch(i));
            // Debug.Log($"[Monotone] Executing");
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
        // SoundFrequency = 0.05f;
        // s = 1;
    }

    // public override void Execute(string text, SpeakerData data)
    // {
    //     // Debug.Log($"[Duotone] Executing");
    //     if (text == null) return;
    //     DialogueManager.RunCoroutine(StyleEmitRoutine(text.Length));
    // }
    


}

public class DuotoneSpeech : ChatterBehaviour
{
    public DuotoneSpeech()
    {
        TextSpeed = 0.02f;
        // SoundFrequency = 0.05f;
    }

    // public override void Execute(string text, SpeakerData data)
    // {
    //     // Debug.Log($"[Duotone] Executing");
    //     if (text == null) return;
    //     DialogueManager.RunCoroutine(StyleEmitRoutine(text.Length));
    // }
    
    protected override float GetSoundPitch(int index)
    {
        return (index % 2 == 0) ? 1.1f : 1f;
    }
    
}
//
// public class AnimaleseSpeech : ChatterBehaviour
// {
// }
//

/*
 * TODO: fix text view for full-word and intro speech.
 * Currently the logic is based per audio iteration and the
 * text rendering comes out wrong (may create 2 event loops, 1 for
 * audio and 1 for text view)
 */
public class FullWordSpeech : ChatterBehaviour
{
    public FullWordSpeech()
    {
        TextSpeed = 0.02f;
        // SoundFrequency = 0.05f;
    }

    private int GetWordCount(string s)
    {
        if(string.IsNullOrEmpty(s)) return 0;
        
        string[] words = s.Split(' ');
        return words.Length;

    }

    public override void Execute(string text, SpeakerData data)
    {
        Debug.Log($"[Duotone] Executing");
        if (text == null) return;
        DialogueManager.RunCoroutine(StyleEmitRoutine(GetWordCount(text)));
    }
}
//
public class IntroSpeech : ChatterBehaviour
{
    public IntroSpeech()
    {
        TextSpeed = 0.02f;
        // SoundFrequency = 0.05f;
    }
    


    public override void Execute(string text, SpeakerData data)
    {
        
        Debug.Log($"[Duotone] Executing");
        if (text == null) return;
        DialogueManager.RunCoroutine(StyleEmitRoutine(1));
    }
}